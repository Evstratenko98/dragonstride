using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class MatchStateApplier : IMatchStateApplier
{
    private readonly IMatchRuntimeRoleService _runtimeRoleService;
    private readonly IMatchNetworkService _matchNetworkService;
    private readonly IMatchClientTurnStateService _clientTurnStateService;
    private readonly FieldState _fieldState;
    private readonly CharacterRoster _characterRoster;
    private readonly EnemySpawner _enemySpawner;
    private readonly IActorIdentityService _actorIdentityService;
    private readonly FieldPresenter _fieldPresenter;
    private readonly ConfigScriptableObject _config;
    private readonly IEventBus _eventBus;
    private readonly IInventorySnapshotService _inventorySnapshotService;

    public bool HasReceivedInitialSnapshot { get; private set; }
    public long LastAppliedSequence { get; private set; }

    public MatchStateApplier(
        IMatchRuntimeRoleService runtimeRoleService,
        IMatchNetworkService matchNetworkService,
        IMatchClientTurnStateService clientTurnStateService,
        FieldState fieldState,
        CharacterRoster characterRoster,
        EnemySpawner enemySpawner,
        IActorIdentityService actorIdentityService,
        FieldPresenter fieldPresenter,
        ConfigScriptableObject config,
        IEventBus eventBus,
        IInventorySnapshotService inventorySnapshotService)
    {
        _runtimeRoleService = runtimeRoleService;
        _matchNetworkService = matchNetworkService;
        _clientTurnStateService = clientTurnStateService;
        _fieldState = fieldState;
        _characterRoster = characterRoster;
        _enemySpawner = enemySpawner;
        _actorIdentityService = actorIdentityService;
        _fieldPresenter = fieldPresenter;
        _config = config;
        _eventBus = eventBus;
        _inventorySnapshotService = inventorySnapshotService;
    }

    public bool TryApply(MatchStateSnapshot snapshot)
    {
        if (_runtimeRoleService == null || !_runtimeRoleService.IsClientReplica)
        {
            return false;
        }

        if (snapshot.Sequence <= LastAppliedSequence)
        {
            return false;
        }

        FieldGrid field = _fieldState?.CurrentField;
        if (field == null)
        {
            return false;
        }

        ApplyOpenedCells(snapshot.OpenedCells, field);
        ApplyActors(snapshot.Actors, field);
        ApplyRemovedReplicaEnemies(snapshot.Actors);
        ApplyInventories(snapshot.Inventories);
        UpdateOnlineTurnState(snapshot);

        LastAppliedSequence = snapshot.Sequence;
        bool isInitial = !HasReceivedInitialSnapshot;
        HasReceivedInitialSnapshot = true;
        _eventBus.Publish(new MatchSnapshotApplied(snapshot.Sequence, isInitial));
        return true;
    }

    private void ApplyInventories(IReadOnlyList<CharacterInventorySnapshot> inventories)
    {
        if (_inventorySnapshotService == null || inventories == null)
        {
            return;
        }

        for (int i = 0; i < inventories.Count; i++)
        {
            _inventorySnapshotService.Apply(inventories[i]);
        }
    }

    private void UpdateOnlineTurnState(MatchStateSnapshot snapshot)
    {
        if (_clientTurnStateService == null)
        {
            return;
        }

        string localPlayerId = _matchNetworkService != null ? _matchNetworkService.LocalPlayerId : string.Empty;
        _clientTurnStateService.UpdateFromSnapshot(snapshot, localPlayerId);

        _eventBus.Publish(new OnlineTurnStateUpdated(
            snapshot.CurrentActorId,
            _clientTurnStateService.CurrentOwnerPlayerId,
            ResolveCurrentActorDisplayName(snapshot),
            snapshot.TurnState,
            snapshot.StepsTotal,
            snapshot.StepsRemaining,
            _clientTurnStateService.IsLocalTurn));
    }

    private static string ResolveCurrentActorDisplayName(MatchStateSnapshot snapshot)
    {
        if (snapshot.Actors == null || snapshot.CurrentActorId <= 0)
        {
            return string.Empty;
        }

        for (int i = 0; i < snapshot.Actors.Count; i++)
        {
            ActorStateSnapshot actor = snapshot.Actors[i];
            if (actor.ActorId != snapshot.CurrentActorId)
            {
                continue;
            }

            return actor.DisplayName ?? string.Empty;
        }

        return string.Empty;
    }

    private void ApplyOpenedCells(IReadOnlyList<OpenedCellSnapshot> openedCells, FieldGrid field)
    {
        if (openedCells == null || openedCells.Count == 0 || field == null)
        {
            return;
        }

        for (int i = 0; i < openedCells.Count; i++)
        {
            OpenedCellSnapshot openedCell = openedCells[i];
            Cell cell = field.GetCell(openedCell.X, openedCell.Y);
            if (cell == null)
            {
                continue;
            }

            bool changed = false;
            if (Enum.TryParse(openedCell.CellType, true, out CellType parsedType) && cell.Type != parsedType)
            {
                cell.SetType(parsedType);
                changed = true;
            }

            if (!cell.IsTypeRevealed)
            {
                cell.RevealType();
                changed = true;
            }

            if (!cell.IsOpened)
            {
                cell.MarkOpened();
                changed = true;
            }

            if (changed)
            {
                _fieldPresenter.RefreshCell(cell);
            }
        }
    }

    private void ApplyActors(IReadOnlyList<ActorStateSnapshot> actors, FieldGrid field)
    {
        if (actors == null || _actorIdentityService == null || field == null)
        {
            return;
        }

        for (int i = 0; i < actors.Count; i++)
        {
            ActorStateSnapshot snapshot = actors[i];
            ICellLayoutOccupant actor = ResolveOrSpawnActor(snapshot, field);
            if (actor == null)
            {
                continue;
            }

            Entity entity = actor.Entity;
            if (entity == null)
            {
                continue;
            }

            if (entity.Level != snapshot.Level)
            {
                entity.SetLevel(snapshot.Level);
            }

            if (entity.Health != snapshot.Health)
            {
                entity.SetHealth(snapshot.Health);
            }

            if (!snapshot.IsAlive)
            {
                HandleDeadActor(actor, entity);
                continue;
            }

            Cell targetCell = field.GetCell(snapshot.CellX, snapshot.CellY);
            if (targetCell == null)
            {
                continue;
            }

            Cell currentCell = entity.CurrentCell;
            if (!ReferenceEquals(currentCell, targetCell))
            {
                entity.SetCell(targetCell);
                SyncActorViewPosition(actor, targetCell);
                _characterRoster.UpdateEntityLayout(entity, currentCell);
                _eventBus.Publish(new CharacterMoved(actor));
            }
        }
    }

    private ICellLayoutOccupant ResolveOrSpawnActor(ActorStateSnapshot snapshot, FieldGrid field)
    {
        if (snapshot.ActorId <= 0 || _actorIdentityService == null)
        {
            return null;
        }

        if (_actorIdentityService.TryGetActor(snapshot.ActorId, out ICellLayoutOccupant existingActor) && existingActor != null)
        {
            return existingActor;
        }

        if (string.Equals(snapshot.ActorType, "character", StringComparison.OrdinalIgnoreCase))
        {
            CharacterInstance character = ResolveCharacterForSnapshot(snapshot);
            if (character != null)
            {
                _actorIdentityService.TryBind(character, snapshot.ActorId);
                return character;
            }
        }

        if (!snapshot.IsAlive || !string.Equals(snapshot.ActorType, "enemy", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        Cell spawnCell = field.GetCell(snapshot.CellX, snapshot.CellY);
        if (spawnCell == null || _enemySpawner == null)
        {
            return null;
        }

        string enemyType = string.IsNullOrWhiteSpace(snapshot.CharacterId)
            ? snapshot.DisplayName
            : snapshot.CharacterId;

        EnemyInstance spawnedEnemy = _enemySpawner.SpawnReplicatedEnemy(enemyType, spawnCell);
        if (spawnedEnemy == null)
        {
            return null;
        }

        _actorIdentityService.TryBind(spawnedEnemy, snapshot.ActorId);
        return spawnedEnemy;
    }

    private CharacterInstance ResolveCharacterForSnapshot(ActorStateSnapshot snapshot)
    {
        if (_characterRoster == null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(snapshot.OwnerPlayerId) &&
            _characterRoster.TryGetCharacterByPlayerId(snapshot.OwnerPlayerId, out CharacterInstance byOwner) &&
            byOwner != null)
        {
            return byOwner;
        }

        IReadOnlyList<CharacterInstance> characters = _characterRoster.AllCharacters;
        for (int i = 0; i < characters.Count; i++)
        {
            CharacterInstance candidate = characters[i];
            if (candidate == null)
            {
                continue;
            }

            bool sameCharacterId = string.Equals(
                candidate.CharacterDefinition?.Id ?? string.Empty,
                snapshot.CharacterId ?? string.Empty,
                StringComparison.Ordinal);
            bool sameName = string.Equals(
                candidate.Name ?? string.Empty,
                snapshot.DisplayName ?? string.Empty,
                StringComparison.Ordinal);

            if (sameCharacterId && sameName)
            {
                return candidate;
            }
        }

        return null;
    }

    private void HandleDeadActor(ICellLayoutOccupant actor, Entity entity)
    {
        if (actor is EnemyInstance enemy)
        {
            _enemySpawner?.RemoveEnemy(enemy);
            return;
        }

        Cell previousCell = entity.CurrentCell;
        if (previousCell == null)
        {
            return;
        }

        entity.SetCell(null);
        _characterRoster.UpdateEntityLayout(entity, previousCell);
    }

    private void ApplyRemovedReplicaEnemies(IReadOnlyList<ActorStateSnapshot> actorSnapshots)
    {
        if (_actorIdentityService == null || _enemySpawner == null)
        {
            return;
        }

        var snapshotActorIds = new HashSet<int>();
        if (actorSnapshots != null)
        {
            for (int i = 0; i < actorSnapshots.Count; i++)
            {
                int actorId = actorSnapshots[i].ActorId;
                if (actorId > 0)
                {
                    snapshotActorIds.Add(actorId);
                }
            }
        }

        IReadOnlyList<int> knownActorIds = _actorIdentityService.GetActorIds();
        for (int i = 0; i < knownActorIds.Count; i++)
        {
            int actorId = knownActorIds[i];
            if (snapshotActorIds.Contains(actorId))
            {
                continue;
            }

            if (!_actorIdentityService.TryGetActor(actorId, out ICellLayoutOccupant actor) || actor is not EnemyInstance enemy)
            {
                continue;
            }

            _enemySpawner.RemoveEnemy(enemy);
        }
    }

    private void SyncActorViewPosition(ICellLayoutOccupant actor, Cell targetCell)
    {
        if (actor == null || targetCell == null || _config == null)
        {
            return;
        }

        if (actor is CharacterInstance character && character.View != null)
        {
            character.View.SetPosition(character.GetCoordinatesForCellView(targetCell.X, targetCell.Y));
            return;
        }

        if (actor is EnemyInstance enemy && enemy.View != null)
        {
            enemy.View.transform.position = new Vector3(
                targetCell.X * _config.CellDistance,
                _config.CHARACTER_HEIGHT,
                targetCell.Y * _config.CellDistance);
            enemy.SetWorldVisible(targetCell.VisibilityState == CellVisibility.Visible);
        }
    }
}
