public sealed class CrownOwnershipService
{
    public const string CrownStateId = "crown_owner";
    public const string CrownStateName = "ВЛАДЕЛЕЦ КОРОНЫ";

    private static readonly State CrownState = new State(
        CrownStateId,
        CrownStateName,
        new EntityStatModifier());

    private readonly IEventBus _eventBus;
    private Entity _owner;

    public CrownOwnershipService(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public void OnEntityKilled(ICellLayoutOccupant killer, ICellLayoutOccupant victim)
    {
        Entity killerEntity = killer?.Entity;
        Entity victimEntity = victim?.Entity;
        if (victimEntity == null)
        {
            return;
        }

        bool victimHadCrown = HasCrown(victimEntity);
        bool victimIsBoss = victimEntity is BossEnemy;

        if (!victimHadCrown && !victimIsBoss)
        {
            return;
        }

        if (victimHadCrown)
        {
            victimEntity.RemoveState(CrownState);
            if (ReferenceEquals(_owner, victimEntity))
            {
                _owner = null;
            }
        }

        AssignCrownTo(killerEntity);
    }

    public bool TryFinishGame(ICellLayoutOccupant actor)
    {
        if (actor is not CharacterInstance)
        {
            return false;
        }

        Entity entity = actor.Entity;
        Cell currentCell = entity?.CurrentCell;
        if (entity == null || currentCell == null)
        {
            return false;
        }

        if (currentCell.Type != CellType.Start || !HasCrown(entity))
        {
            return false;
        }

        _eventBus.Publish(new GameStateChanged(GameState.Finished));
        return true;
    }

    public bool HasCrown(Entity entity)
    {
        if (entity == null)
        {
            return false;
        }

        var states = entity.States;
        for (int i = 0; i < states.Count; i++)
        {
            if (states[i]?.Id == CrownStateId)
            {
                return true;
            }
        }

        return false;
    }

    private void AssignCrownTo(Entity entity)
    {
        if (entity == null)
        {
            return;
        }

        if (_owner != null && !ReferenceEquals(_owner, entity))
        {
            _owner.RemoveState(CrownState);
        }

        if (!HasCrown(entity))
        {
            entity.AddState(CrownState);
        }

        _owner = entity;
    }
}
