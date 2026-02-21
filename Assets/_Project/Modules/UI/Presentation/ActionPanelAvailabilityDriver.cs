using System;
using VContainer.Unity;

public class ActionPanelAvailabilityDriver : IPostInitializable, IDisposable
{
    private readonly TurnActorRegistry _turnActorRegistry;
    private readonly IEventBus _eventBus;
    private readonly TurnFlow _turnFlow;
    private readonly IMatchRuntimeRoleService _runtimeRoleService;
    private readonly IMatchClientTurnStateService _clientTurnStateService;
    private readonly IActorIdentityService _actorIdentityService;

    private IDisposable _turnStateSubscription;
    private IDisposable _characterMovedSubscription;
    private IDisposable _onlineTurnStateSubscription;
    private IDisposable _snapshotAppliedSubscription;

    private ICellLayoutOccupant _currentActor;
    private int _onlineCurrentActorId;

    public ActionPanelAvailabilityDriver(
        TurnActorRegistry turnActorRegistry,
        IEventBus eventBus,
        TurnFlow turnFlow,
        IMatchRuntimeRoleService runtimeRoleService,
        IMatchClientTurnStateService clientTurnStateService,
        IActorIdentityService actorIdentityService)
    {
        _turnActorRegistry = turnActorRegistry;
        _eventBus = eventBus;
        _turnFlow = turnFlow;
        _runtimeRoleService = runtimeRoleService;
        _clientTurnStateService = clientTurnStateService;
        _actorIdentityService = actorIdentityService;
    }

    public void PostInitialize()
    {
        _turnStateSubscription = _eventBus.Subscribe<TurnPhaseChanged>(OnTurnStateChanged);
        _characterMovedSubscription = _eventBus.Subscribe<CharacterMoved>(OnCharacterMoved);
        _onlineTurnStateSubscription = _eventBus.Subscribe<OnlineTurnStateUpdated>(OnOnlineTurnStateUpdated);
        _snapshotAppliedSubscription = _eventBus.Subscribe<MatchSnapshotApplied>(OnMatchSnapshotApplied);
        PublishAvailability();
    }

    public void Dispose()
    {
        _turnStateSubscription?.Dispose();
        _characterMovedSubscription?.Dispose();
        _onlineTurnStateSubscription?.Dispose();
        _snapshotAppliedSubscription?.Dispose();
    }

    private void OnTurnStateChanged(TurnPhaseChanged msg)
    {
        _currentActor = msg.Actor;
        PublishAvailability();
    }

    private void OnCharacterMoved(CharacterMoved _)
    {
        PublishAvailability();
    }

    private void OnOnlineTurnStateUpdated(OnlineTurnStateUpdated msg)
    {
        _onlineCurrentActorId = msg.CurrentActorId;

        if (_runtimeRoleService != null && _runtimeRoleService.IsClientReplica)
        {
            if (_actorIdentityService != null && _onlineCurrentActorId > 0 &&
                _actorIdentityService.TryGetActor(_onlineCurrentActorId, out ICellLayoutOccupant actor))
            {
                _currentActor = actor;
            }
            else
            {
                _currentActor = null;
            }
        }

        PublishAvailability();
    }

    private void OnMatchSnapshotApplied(MatchSnapshotApplied _)
    {
        PublishAvailability();
    }

    private void PublishAvailability()
    {
        if (_runtimeRoleService != null && _runtimeRoleService.IsClientReplica)
        {
            PublishAvailabilityForClientReplica();
            return;
        }

        PublishAvailabilityForAuthoritative();
    }

    private void PublishAvailabilityForClientReplica()
    {
        if (_clientTurnStateService == null ||
            !_clientTurnStateService.HasInitialState ||
            !_clientTurnStateService.IsLocalTurn ||
            !IsTurnActionState(_clientTurnStateService.CurrentTurnState))
        {
            _eventBus.Publish(new AttackAvailabilityChanged(false));
            return;
        }

        if (_currentActor?.Entity?.CurrentCell == null)
        {
            _eventBus.Publish(new AttackAvailabilityChanged(false));
            return;
        }

        Cell currentCell = _currentActor.Entity.CurrentCell;
        if (currentCell.Type == CellType.Start)
        {
            _eventBus.Publish(new AttackAvailabilityChanged(false));
            return;
        }

        _eventBus.Publish(new AttackAvailabilityChanged(HasTargetInCurrentCell(currentCell)));
    }

    private void PublishAvailabilityForAuthoritative()
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

        Cell currentCell = _currentActor.Entity.CurrentCell;
        if (currentCell.Type == CellType.Start)
        {
            _eventBus.Publish(new AttackAvailabilityChanged(false));
            return;
        }

        _eventBus.Publish(new AttackAvailabilityChanged(HasTargetInCurrentCell(currentCell)));
    }

    private bool HasTargetInCurrentCell(Cell currentCell)
    {
        bool hasTarget = false;
        var actors = _turnActorRegistry.GetActiveActorsSnapshot();
        for (int i = 0; i < actors.Count; i++)
        {
            ICellLayoutOccupant target = actors[i];
            if (target == null || target == _currentActor)
            {
                continue;
            }

            Cell targetCell = target.Entity?.CurrentCell;
            if (targetCell == currentCell)
            {
                hasTarget = true;
                break;
            }
        }

        return hasTarget;
    }

    private static bool IsTurnActionState(TurnState state)
    {
        return state == TurnState.ActionSelection ||
               state == TurnState.Movement ||
               state == TurnState.Attack ||
               state == TurnState.OpenCell ||
               state == TurnState.Trade;
    }
}
