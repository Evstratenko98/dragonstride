using System;
using VContainer.Unity;

public class ActionPanelAvailabilityDriver : IPostInitializable, IDisposable
{
    private readonly TurnActorRegistry _turnActorRegistry;
    private readonly IEventBus _eventBus;
    private readonly TurnFlow _turnFlow;
    private IDisposable _turnStateSubscription;
    private IDisposable _characterMovedSubscription;

    private ICellLayoutOccupant _currentActor;

    public ActionPanelAvailabilityDriver(TurnActorRegistry turnActorRegistry, IEventBus eventBus, TurnFlow turnFlow)
    {
        _turnActorRegistry = turnActorRegistry;
        _eventBus = eventBus;
        _turnFlow = turnFlow;
    }

    public void PostInitialize()
    {
        _turnStateSubscription = _eventBus.Subscribe<TurnPhaseChanged>(OnTurnStateChanged);
        _characterMovedSubscription = _eventBus.Subscribe<CharacterMoved>(OnCharacterMoved);
        PublishAvailability();
    }

    public void Dispose()
    {
        _turnStateSubscription?.Dispose();
        _characterMovedSubscription?.Dispose();
    }

    private void OnTurnStateChanged(TurnPhaseChanged msg)
    {
        _currentActor = msg.Actor;

        PublishAvailability();
    }

    private void OnCharacterMoved(CharacterMoved msg)
    {
        PublishAvailability();
    }

    private void PublishAvailability()
    {
        if (_currentActor?.Entity?.CurrentCell == null)
        {
            _eventBus.Publish(new AttackAvailabilityChanged(false));
            return;
        }

        if (_turnFlow != null && _turnFlow.HasAttacked)
        {
            _eventBus.Publish(new AttackAvailabilityChanged(false));
            return;
        }

        var currentCell = _currentActor.Entity.CurrentCell;
        if (currentCell.Type == CellType.Start)
        {
            _eventBus.Publish(new AttackAvailabilityChanged(false));
            return;
        }

        bool hasTarget = false;
        var actors = _turnActorRegistry.GetActiveActorsSnapshot();
        for (int i = 0; i < actors.Count; i++)
        {
            var target = actors[i];
            if (target == null || target == _currentActor)
            {
                continue;
            }

            var targetCell = target.Entity?.CurrentCell;
            if (targetCell == currentCell)
            {
                hasTarget = true;
                break;
            }
        }

        _eventBus.Publish(new AttackAvailabilityChanged(hasTarget));
    }
}
