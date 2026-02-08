using System;
using VContainer.Unity;

public sealed class CellOpenDriver : IPostInitializable, IDisposable
{
    private readonly IEventBus _eventBus;
    private readonly CellOpenService _cellOpenService;

    private IDisposable _turnStateSubscription;

    public CellOpenDriver(IEventBus eventBus, CellOpenService cellOpenService)
    {
        _eventBus = eventBus;
        _cellOpenService = cellOpenService;
    }

    public void PostInitialize()
    {
        _turnStateSubscription = _eventBus.Subscribe<TurnPhaseChanged>(OnTurnStateChanged);
    }

    public void Dispose()
    {
        _turnStateSubscription?.Dispose();
    }

    private void OnTurnStateChanged(TurnPhaseChanged message)
    {
        if (message.State != TurnState.OpenCell)
        {
            return;
        }

        _cellOpenService.TryOpen(message.Actor);
    }
}
