using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class MatchStateApplier : IMatchStateApplier
{
    private readonly IMatchRuntimeRoleService _runtimeRoleService;
    private readonly FieldState _fieldState;
    private readonly CharacterRoster _characterRoster;
    private readonly IActorIdentityService _actorIdentityService;
    private readonly FieldPresenter _fieldPresenter;
    private readonly ConfigScriptableObject _config;
    private readonly IEventBus _eventBus;

    public bool HasReceivedInitialSnapshot { get; private set; }
    public long LastAppliedSequence { get; private set; }

    public MatchStateApplier(
        IMatchRuntimeRoleService runtimeRoleService,
        FieldState fieldState,
        CharacterRoster characterRoster,
        IActorIdentityService actorIdentityService,
        FieldPresenter fieldPresenter,
        ConfigScriptableObject config,
        IEventBus eventBus)
    {
        _runtimeRoleService = runtimeRoleService;
        _fieldState = fieldState;
        _characterRoster = characterRoster;
        _actorIdentityService = actorIdentityService;
        _fieldPresenter = fieldPresenter;
        _config = config;
        _eventBus = eventBus;
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

        ApplyOpenedCells(snapshot.OpenedCells);
        ApplyActors(snapshot.Actors);

        LastAppliedSequence = snapshot.Sequence;
        bool isInitial = !HasReceivedInitialSnapshot;
        HasReceivedInitialSnapshot = true;
        _eventBus.Publish(new MatchSnapshotApplied(snapshot.Sequence, isInitial));
        return true;
    }

    private void ApplyOpenedCells(IReadOnlyList<OpenedCellSnapshot> openedCells)
    {
        if (openedCells == null || openedCells.Count == 0)
        {
            return;
        }

        FieldGrid field = _fieldState?.CurrentField;
        if (field == null)
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

    private void ApplyActors(IReadOnlyList<ActorStateSnapshot> actors)
    {
        if (actors == null || actors.Count == 0 || _actorIdentityService == null)
        {
            return;
        }

        FieldGrid field = _fieldState?.CurrentField;
        if (field == null)
        {
            return;
        }

        for (int i = 0; i < actors.Count; i++)
        {
            ActorStateSnapshot snapshot = actors[i];
            if (!_actorIdentityService.TryGetActor(snapshot.ActorId, out ICellLayoutOccupant actor) || actor == null)
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
                Cell previousCell = entity.CurrentCell;
                if (previousCell != null)
                {
                    entity.SetCell(null);
                    _characterRoster.UpdateEntityLayout(entity, previousCell);
                }

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
            }
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
