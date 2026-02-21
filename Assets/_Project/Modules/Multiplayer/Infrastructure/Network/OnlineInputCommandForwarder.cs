using System;
using System.Threading;
using UnityEngine;
using VContainer.Unity;

public sealed class OnlineInputCommandForwarder : IPostInitializable, IDisposable
{
    private const int MoveSubmitCooldownMs = 120;

    private readonly IEventBus _eventBus;
    private readonly IMatchRuntimeRoleService _runtimeRoleService;
    private readonly IMatchNetworkService _matchNetworkService;
    private readonly TurnFlow _turnFlow;
    private readonly IGameCommandGateway _commandGateway;
    private readonly IMatchClientTurnStateService _clientTurnStateService;
    private readonly IGameCommandPolicyService _commandPolicyService;
    private readonly IClientActionPlaybackService _clientActionPlaybackService;
    private readonly IMatchActionTimelineService _matchActionTimelineService;

    private IDisposable _moveRequestedSubscription;
    private IDisposable _endTurnRequestedSubscription;
    private IDisposable _openCellRequestedSubscription;
    private IDisposable _attackTargetSelectedSubscription;
    private IDisposable _takeLootRequestedSubscription;

    private DateTime _lastMoveSubmitUtc = DateTime.MinValue;
    private int _moveInFlight;

    public OnlineInputCommandForwarder(
        IEventBus eventBus,
        IMatchRuntimeRoleService runtimeRoleService,
        IMatchNetworkService matchNetworkService,
        TurnFlow turnFlow,
        IGameCommandGateway commandGateway,
        IMatchClientTurnStateService clientTurnStateService,
        IGameCommandPolicyService commandPolicyService,
        IClientActionPlaybackService clientActionPlaybackService,
        IMatchActionTimelineService matchActionTimelineService)
    {
        _eventBus = eventBus;
        _runtimeRoleService = runtimeRoleService;
        _matchNetworkService = matchNetworkService;
        _turnFlow = turnFlow;
        _commandGateway = commandGateway;
        _clientTurnStateService = clientTurnStateService;
        _commandPolicyService = commandPolicyService;
        _clientActionPlaybackService = clientActionPlaybackService;
        _matchActionTimelineService = matchActionTimelineService;
    }

    public void PostInitialize()
    {
        _moveRequestedSubscription = _eventBus.Subscribe<MoveCommandRequested>(OnMoveRequested);
        _endTurnRequestedSubscription = _eventBus.Subscribe<EndTurnRequested>(OnEndTurnRequested);
        _openCellRequestedSubscription = _eventBus.Subscribe<OpenCellRequested>(OnOpenCellRequested);
        _attackTargetSelectedSubscription = _eventBus.Subscribe<AttackTargetSelected>(OnAttackTargetSelected);
        _takeLootRequestedSubscription = _eventBus.Subscribe<TakeLootRequested>(OnTakeLootRequested);
    }

    public void Dispose()
    {
        _moveRequestedSubscription?.Dispose();
        _endTurnRequestedSubscription?.Dispose();
        _openCellRequestedSubscription?.Dispose();
        _attackTargetSelectedSubscription?.Dispose();
        _takeLootRequestedSubscription?.Dispose();
    }

    private async void OnMoveRequested(MoveCommandRequested message)
    {
        if (!ShouldForwardOnlineInput(GameCommandType.Move))
        {
            return;
        }

        if (Interlocked.CompareExchange(ref _moveInFlight, 1, 0) != 0)
        {
            return;
        }

        try
        {
            if (DateTime.UtcNow - _lastMoveSubmitUtc < TimeSpan.FromMilliseconds(MoveSubmitCooldownMs))
            {
                return;
            }

            _lastMoveSubmitUtc = DateTime.UtcNow;
            await _commandGateway.SubmitMoveAsync(message.Direction);
        }
        finally
        {
            Interlocked.Exchange(ref _moveInFlight, 0);
        }
    }

    private async void OnEndTurnRequested(EndTurnRequested _)
    {
        if (!ShouldForwardOnlineInput(GameCommandType.EndTurn))
        {
            return;
        }

        await _commandGateway.SubmitEndTurnAsync();
    }

    private async void OnOpenCellRequested(OpenCellRequested _)
    {
        if (!ShouldForwardOnlineInput(GameCommandType.OpenCell))
        {
            return;
        }

        await _commandGateway.SubmitOpenCellAsync();
    }

    private async void OnAttackTargetSelected(AttackTargetSelected message)
    {
        if (!ShouldForwardOnlineInput(GameCommandType.Attack))
        {
            return;
        }

        await _commandGateway.SubmitAttackAsync(message.TargetActorId);
    }

    private async void OnTakeLootRequested(TakeLootRequested _)
    {
        if (!ShouldForwardOnlineInput(GameCommandType.TakeLoot))
        {
            return;
        }

        await _commandGateway.SubmitTakeLootAsync();
    }

    private bool ShouldForwardOnlineInput(GameCommandType commandType)
    {
        if (_runtimeRoleService == null || !_runtimeRoleService.IsOnlineMatch)
        {
            return false;
        }

        CommandTiming timing = _commandPolicyService?.GetTiming(commandType) ?? CommandTiming.TurnBound;
        bool playbackBusy = (_clientActionPlaybackService != null && _clientActionPlaybackService.IsBusy) ||
                            (_matchActionTimelineService != null && _matchActionTimelineService.IsPlaybackInProgress);
        if (timing == CommandTiming.TurnBound && playbackBusy)
        {
            return false;
        }

        if (!_runtimeRoleService.IsClientReplica)
        {
            return timing == CommandTiming.Anytime || IsHostTurnBoundCommandAllowed(commandType);
        }

        if (_clientTurnStateService == null || !_clientTurnStateService.HasInitialState)
        {
            return false;
        }

        if (timing == CommandTiming.TurnBound && !_clientTurnStateService.IsLocalTurn)
        {
            return false;
        }

        return true;
    }

    private bool IsHostTurnBoundCommandAllowed(GameCommandType commandType)
    {
        if (_turnFlow?.CurrentActor is not CharacterInstance currentCharacter)
        {
            return false;
        }

        string localPlayerId = _matchNetworkService != null ? _matchNetworkService.LocalPlayerId : string.Empty;
        if (string.IsNullOrWhiteSpace(localPlayerId) ||
            !string.Equals(currentCharacter.PlayerId, localPlayerId, StringComparison.Ordinal))
        {
            return false;
        }

        return commandType switch
        {
            GameCommandType.Move => (_turnFlow.State == TurnState.ActionSelection || _turnFlow.State == TurnState.Movement)
                                    && _turnFlow.StepsRemaining > 0,
            GameCommandType.Attack => _turnFlow.State == TurnState.ActionSelection,
            GameCommandType.OpenCell => _turnFlow.State == TurnState.ActionSelection || _turnFlow.State == TurnState.Movement,
            GameCommandType.TakeLoot => _turnFlow.State == TurnState.ActionSelection ||
                                        _turnFlow.State == TurnState.Movement ||
                                        _turnFlow.State == TurnState.OpenCell ||
                                        _turnFlow.State == TurnState.Trade,
            GameCommandType.EndTurn => _turnFlow.State == TurnState.ActionSelection ||
                                       _turnFlow.State == TurnState.Movement ||
                                       _turnFlow.State == TurnState.Attack ||
                                       _turnFlow.State == TurnState.OpenCell ||
                                       _turnFlow.State == TurnState.Trade,
            _ => false
        };
    }
}
