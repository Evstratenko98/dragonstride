using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;

public sealed class OnlineInputCommandForwarder : IPostInitializable, IDisposable
{
    private const int MoveSubmitCooldownMs = 120;

    private readonly IEventBus _eventBus;
    private readonly IMatchRuntimeRoleService _runtimeRoleService;
    private readonly IGameCommandGateway _commandGateway;
    private readonly IMatchStateApplier _matchStateApplier;

    private IDisposable _moveRequestedSubscription;
    private IDisposable _endTurnRequestedSubscription;
    private IDisposable _openCellRequestedSubscription;
    private IDisposable _attackTargetSelectedSubscription;

    private DateTime _lastMoveSubmitUtc = DateTime.MinValue;
    private int _moveInFlight;

    public OnlineInputCommandForwarder(
        IEventBus eventBus,
        IMatchRuntimeRoleService runtimeRoleService,
        IGameCommandGateway commandGateway,
        IMatchStateApplier matchStateApplier)
    {
        _eventBus = eventBus;
        _runtimeRoleService = runtimeRoleService;
        _commandGateway = commandGateway;
        _matchStateApplier = matchStateApplier;
    }

    public void PostInitialize()
    {
        _moveRequestedSubscription = _eventBus.Subscribe<MoveCommandRequested>(OnMoveRequested);
        _endTurnRequestedSubscription = _eventBus.Subscribe<EndTurnRequested>(OnEndTurnRequested);
        _openCellRequestedSubscription = _eventBus.Subscribe<OpenCellRequested>(OnOpenCellRequested);
        _attackTargetSelectedSubscription = _eventBus.Subscribe<AttackTargetSelected>(OnAttackTargetSelected);
    }

    public void Dispose()
    {
        _moveRequestedSubscription?.Dispose();
        _endTurnRequestedSubscription?.Dispose();
        _openCellRequestedSubscription?.Dispose();
        _attackTargetSelectedSubscription?.Dispose();
    }

    private async void OnMoveRequested(MoveCommandRequested message)
    {
        if (!ShouldForwardOnlineInput())
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
        if (!ShouldForwardOnlineInput())
        {
            return;
        }

        await _commandGateway.SubmitEndTurnAsync();
    }

    private async void OnOpenCellRequested(OpenCellRequested _)
    {
        if (!ShouldForwardOnlineInput())
        {
            return;
        }

        await _commandGateway.SubmitOpenCellAsync();
    }

    private async void OnAttackTargetSelected(AttackTargetSelected message)
    {
        if (!ShouldForwardOnlineInput())
        {
            return;
        }

        await _commandGateway.SubmitAttackAsync(message.TargetActorId);
    }

    private bool ShouldForwardOnlineInput()
    {
        if (_runtimeRoleService == null || !_runtimeRoleService.IsOnlineMatch)
        {
            return false;
        }

        if (_runtimeRoleService.IsClientReplica &&
            (_matchStateApplier == null || !_matchStateApplier.HasReceivedInitialSnapshot))
        {
            return false;
        }

        return true;
    }
}
