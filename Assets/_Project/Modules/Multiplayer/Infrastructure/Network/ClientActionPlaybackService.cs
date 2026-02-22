using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public sealed class ClientActionPlaybackService : IClientActionPlaybackService
{
    private readonly IMatchRuntimeRoleService _runtimeRoleService;
    private readonly IMatchNetworkService _matchNetworkService;
    private readonly IMatchClientTurnStateService _clientTurnStateService;
    private readonly IActorIdentityService _actorIdentityService;
    private readonly CharacterRoster _characterRoster;
    private readonly EnemySpawner _enemySpawner;
    private readonly FieldState _fieldState;
    private readonly FieldPresenter _fieldPresenter;
    private readonly ConfigScriptableObject _config;
    private readonly IInventorySnapshotService _inventorySnapshotService;
    private readonly IEventBus _eventBus;

    private readonly SemaphoreSlim _playbackLock = new(1, 1);

    public bool IsBusy { get; private set; }
    public long LastAppliedActionSequence { get; private set; }

    public ClientActionPlaybackService(
        IMatchRuntimeRoleService runtimeRoleService,
        IMatchNetworkService matchNetworkService,
        IMatchClientTurnStateService clientTurnStateService,
        IActorIdentityService actorIdentityService,
        CharacterRoster characterRoster,
        EnemySpawner enemySpawner,
        FieldState fieldState,
        FieldPresenter fieldPresenter,
        ConfigScriptableObject config,
        IInventorySnapshotService inventorySnapshotService,
        IEventBus eventBus)
    {
        _runtimeRoleService = runtimeRoleService;
        _matchNetworkService = matchNetworkService;
        _clientTurnStateService = clientTurnStateService;
        _actorIdentityService = actorIdentityService;
        _characterRoster = characterRoster;
        _enemySpawner = enemySpawner;
        _fieldState = fieldState;
        _fieldPresenter = fieldPresenter;
        _config = config;
        _inventorySnapshotService = inventorySnapshotService;
        _eventBus = eventBus;
    }

    public async Task ApplyBatchAsync(MatchActionBatch batch, CancellationToken ct = default)
    {
        if (batch.ActionSequence <= 0)
        {
            return;
        }

        await _playbackLock.WaitAsync(ct);
        try
        {
            if (batch.ActionSequence <= LastAppliedActionSequence)
            {
                return;
            }

            IsBusy = true;
            IReadOnlyList<ActionEventEnvelope> events = batch.Events;
            if (events != null)
            {
                for (int i = 0; i < events.Count; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    await ApplyEventAsync(events[i], ct);
                }
            }

            LastAppliedActionSequence = batch.ActionSequence;
        }
        finally
        {
            IsBusy = false;
            _playbackLock.Release();
        }
    }

    private async Task ApplyEventAsync(ActionEventEnvelope actionEvent, CancellationToken ct)
    {
        bool shouldMutateReplicaWorld = _runtimeRoleService != null && _runtimeRoleService.IsClientReplica;

        switch (actionEvent.Type)
        {
            case ActionEventType.TurnStateChanged:
                ApplyTurnState(actionEvent);
                break;

            case ActionEventType.ActorMoved:
                if (shouldMutateReplicaWorld)
                {
                    if (TryBeginActorMoved(actionEvent, out ICellLayoutOccupant movedActor, out Entity movedEntity, out Cell previousCell))
                    {
                        if (actionEvent.DurationMs > 0)
                        {
                            await Task.Delay(actionEvent.DurationMs, ct);
                        }

                        _characterRoster?.UpdateEntityLayout(movedEntity, previousCell);
                        _eventBus?.Publish(new CharacterMoved(movedActor));
                        return;
                    }
                }
                break;

            case ActionEventType.AttackResolved:
                if (shouldMutateReplicaWorld)
                {
                    ApplyAttackResolved(actionEvent);
                }
                break;

            case ActionEventType.CellOpened:
                if (shouldMutateReplicaWorld)
                {
                    ApplyCellOpened(actionEvent);
                }
                break;

            case ActionEventType.EnemySpawned:
                if (shouldMutateReplicaWorld)
                {
                    ApplyEnemySpawned(actionEvent);
                }
                break;

            case ActionEventType.EnemyDespawned:
                if (shouldMutateReplicaWorld)
                {
                    ApplyEnemyDespawned(actionEvent);
                }
                break;

            case ActionEventType.LootGenerated:
                ApplyLootGenerated(actionEvent);
                break;

            case ActionEventType.LootTaken:
                ApplyLootTaken(actionEvent);
                break;

            case ActionEventType.InventoryUpdated:
                ApplyInventoryUpdated(actionEvent);
                break;
        }

        if (actionEvent.DurationMs > 0)
        {
            await Task.Delay(actionEvent.DurationMs, ct);
        }
    }

    private void ApplyTurnState(ActionEventEnvelope actionEvent)
    {
        TurnState turnState = (TurnState)actionEvent.IntValue1;
        int stepsTotal = Math.Max(0, actionEvent.FromX);
        int stepsRemaining = Math.Max(0, actionEvent.IntValue2);
        string ownerPlayerId = actionEvent.StrValue1 ?? string.Empty;

        bool isLocalTurn = !string.IsNullOrWhiteSpace(ownerPlayerId) &&
                           !string.IsNullOrWhiteSpace(_matchNetworkService?.LocalPlayerId) &&
                           string.Equals(ownerPlayerId, _matchNetworkService.LocalPlayerId, StringComparison.Ordinal) &&
                           IsTurnBoundActionState(turnState);

        if (_clientTurnStateService != null)
        {
            var actors = new List<ActorStateSnapshot>(1)
            {
                new(
                    actionEvent.ActorId,
                    "character",
                    ownerPlayerId,
                    string.Empty,
                    string.Empty,
                    -1,
                    -1,
                    0,
                    1,
                    false,
                    true)
            };

            var snapshot = new MatchStateSnapshot(
                LastAppliedActionSequence,
                GameState.Playing,
                turnState,
                actionEvent.ActorId,
                stepsTotal,
                stepsRemaining,
                false,
                actionEvent.StrValue2,
                "in_game",
                actors,
                Array.Empty<OpenedCellSnapshot>(),
                Array.Empty<CharacterInventorySnapshot>());

            _clientTurnStateService.UpdateFromSnapshot(snapshot, _matchNetworkService?.LocalPlayerId ?? string.Empty);
            isLocalTurn = _clientTurnStateService.IsLocalTurn;
        }

        _eventBus.Publish(new OnlineTurnStateUpdated(
            actionEvent.ActorId,
            ownerPlayerId,
            ResolveActorDisplayName(actionEvent.ActorId, ownerPlayerId),
            turnState,
            stepsTotal,
            stepsRemaining,
            isLocalTurn));
    }

    private bool TryBeginActorMoved(
        ActionEventEnvelope actionEvent,
        out ICellLayoutOccupant movedActor,
        out Entity movedEntity,
        out Cell previousCell)
    {
        movedActor = null;
        movedEntity = null;
        previousCell = null;

        if (_fieldState?.CurrentField == null || _actorIdentityService == null)
        {
            return false;
        }

        if (!_actorIdentityService.TryGetActor(actionEvent.ActorId, out ICellLayoutOccupant actor) || actor?.Entity == null)
        {
            return false;
        }

        Cell targetCell = _fieldState.CurrentField.GetCell(actionEvent.ToX, actionEvent.ToY);
        if (targetCell == null)
        {
            return false;
        }

        movedActor = actor;
        previousCell = actor.Entity.CurrentCell;
        movedEntity = actor.Entity;
        actor.Entity.SetCell(targetCell);

        float speed = _config != null ? Mathf.Max(0.01f, _config.ENTITY_SPEED) : 4f;
        Vector3 targetPosition = GetWorldPosition(targetCell);

        if (actor is CharacterInstance character)
        {
            character.MoveToPosition(targetPosition, speed);
        }
        else if (actor is EnemyInstance enemy)
        {
            enemy.MoveToPosition(targetPosition, speed);
        }
        
        return true;
    }

    private void ApplyAttackResolved(ActionEventEnvelope actionEvent)
    {
        if (_actorIdentityService == null || actionEvent.TargetActorId <= 0)
        {
            return;
        }

        if (!_actorIdentityService.TryGetActor(actionEvent.TargetActorId, out ICellLayoutOccupant target) || target?.Entity == null)
        {
            return;
        }

        target.Entity.SetHealth(Math.Max(0, actionEvent.IntValue1));

        if (!actionEvent.BoolValue2)
        {
            return;
        }

        if (target is EnemyInstance enemy)
        {
            _enemySpawner?.RemoveEnemy(enemy);
        }
    }

    private void ApplyCellOpened(ActionEventEnvelope actionEvent)
    {
        FieldGrid field = _fieldState?.CurrentField;
        if (field == null)
        {
            return;
        }

        Cell cell = field.GetCell(actionEvent.ToX, actionEvent.ToY);
        if (cell == null)
        {
            return;
        }

        if (Enum.IsDefined(typeof(CellType), actionEvent.IntValue1))
        {
            CellType type = (CellType)actionEvent.IntValue1;
            if (cell.Type != type)
            {
                cell.SetType(type);
            }
        }

        if (!cell.IsTypeRevealed)
        {
            cell.RevealType();
        }

        if (!cell.IsOpened)
        {
            cell.MarkOpened();
        }

        _fieldPresenter?.RefreshCell(cell);
    }

    private void ApplyEnemySpawned(ActionEventEnvelope actionEvent)
    {
        if (_fieldState?.CurrentField == null || _enemySpawner == null)
        {
            return;
        }

        if (_actorIdentityService != null && _actorIdentityService.TryGetActor(actionEvent.ActorId, out ICellLayoutOccupant existing) && existing != null)
        {
            return;
        }

        Cell spawnCell = _fieldState.CurrentField.GetCell(actionEvent.ToX, actionEvent.ToY);
        if (spawnCell == null)
        {
            return;
        }

        string enemyType = string.IsNullOrWhiteSpace(actionEvent.StrValue2)
            ? actionEvent.StrValue1
            : actionEvent.StrValue2;

        EnemyInstance spawnedEnemy = _enemySpawner.SpawnReplicatedEnemy(enemyType, spawnCell);
        if (spawnedEnemy == null)
        {
            return;
        }

        if (actionEvent.ActorId > 0)
        {
            _actorIdentityService?.TryBind(spawnedEnemy, actionEvent.ActorId);
        }

        if (actionEvent.IntValue2 > 0)
        {
            spawnedEnemy.Entity.SetHealth(actionEvent.IntValue2);
        }
    }

    private void ApplyEnemyDespawned(ActionEventEnvelope actionEvent)
    {
        if (_actorIdentityService == null || _enemySpawner == null)
        {
            return;
        }

        if (!_actorIdentityService.TryGetActor(actionEvent.ActorId, out ICellLayoutOccupant actor) || actor is not EnemyInstance enemy)
        {
            return;
        }

        _enemySpawner.RemoveEnemy(enemy);
    }

    private void ApplyLootGenerated(ActionEventEnvelope actionEvent)
    {
        IReadOnlyList<LootItemSnapshot> loot = TimelinePayloadSerializer.DeserializeLoot(actionEvent.StrValue2);
        _eventBus.Publish(new OnlineLootGenerated(actionEvent.ActorId, actionEvent.StrValue1, loot));
    }

    private void ApplyLootTaken(ActionEventEnvelope actionEvent)
    {
        _eventBus.Publish(new OnlineLootTaken(actionEvent.ActorId));
    }

    private void ApplyInventoryUpdated(ActionEventEnvelope actionEvent)
    {
        if (_inventorySnapshotService == null)
        {
            return;
        }

        CharacterInventorySnapshot snapshot = TimelinePayloadSerializer.DeserializeInventory(actionEvent.ActorId, actionEvent.StrValue2);
        _inventorySnapshotService.Apply(snapshot);
    }

    private Vector3 GetWorldPosition(Cell cell)
    {
        float cellDistance = _config != null ? _config.CellDistance : 1f;
        float y = _config != null ? _config.CHARACTER_HEIGHT : 0.75f;
        return new Vector3(cell.X * cellDistance, y, cell.Y * cellDistance);
    }

    private static bool IsTurnBoundActionState(TurnState state)
    {
        return state == TurnState.ActionSelection ||
               state == TurnState.Movement ||
               state == TurnState.Attack ||
               state == TurnState.OpenCell ||
               state == TurnState.Trade;
    }

    private string ResolveActorDisplayName(int actorId, string ownerPlayerId)
    {
        if (_actorIdentityService != null &&
            actorId > 0 &&
            _actorIdentityService.TryGetActor(actorId, out ICellLayoutOccupant actor) &&
            actor?.Entity != null &&
            !string.IsNullOrWhiteSpace(actor.Entity.Name))
        {
            return actor.Entity.Name;
        }

        if (!string.IsNullOrWhiteSpace(ownerPlayerId))
        {
            return ownerPlayerId;
        }

        return string.Empty;
    }
}
