using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public sealed class GameCommandExecutionService : IGameCommandExecutionService
{
    private readonly CharacterMovementDriver _movementDriver;
    private readonly AttackDriver _attackDriver;
    private readonly TurnFlow _turnFlow;
    private readonly IActorIdentityService _actorIdentityService;
    private readonly ILootSyncService _lootSyncService;
    private readonly SemaphoreSlim _executionLock = new(1, 1);

    public GameCommandExecutionService(
        CharacterMovementDriver movementDriver,
        AttackDriver attackDriver,
        TurnFlow turnFlow,
        IActorIdentityService actorIdentityService,
        ILootSyncService lootSyncService)
    {
        _movementDriver = movementDriver;
        _attackDriver = attackDriver;
        _turnFlow = turnFlow;
        _actorIdentityService = actorIdentityService;
        _lootSyncService = lootSyncService;
    }

    public async Task<CommandSubmitResult> ExecuteAsync(
        GameCommandEnvelope command,
        CancellationToken cancellationToken = default)
    {
        if (!_executionLock.Wait(0))
        {
            return CommandSubmitResult.Rejected("command_execution_in_progress", "Another command is executing.");
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            bool success = command.CommandType switch
            {
                GameCommandType.Move => await _movementDriver.TryExecuteCommandMoveAsync(command.Direction),
                GameCommandType.Attack => _attackDriver.TryExecuteAttackByActorId(command.TargetActorId),
                GameCommandType.OpenCell => _turnFlow.TryOpenCell(),
                GameCommandType.EndTurn => TryEndTurnWithAutoLoot(),
                GameCommandType.TakeLoot => TryTakeLoot(),
                _ => false
            };

            return success
                ? CommandSubmitResult.Accepted()
                : CommandSubmitResult.Rejected("command_execution_failed", "Command could not be applied to match state.");
        }
        catch (OperationCanceledException)
        {
            return CommandSubmitResult.Rejected("cancelled", "Command execution cancelled.");
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning($"[GameCommandExecutionService] Execution failed: {exception}");
            return CommandSubmitResult.Rejected("command_execution_exception", exception.Message);
        }
        finally
        {
            _executionLock.Release();
        }
    }

    private bool TryTakeLoot()
    {
        int actorId = ResolveCurrentActorId();
        if (actorId <= 0 || _lootSyncService == null)
        {
            return false;
        }

        MultiplayerOperationResult<CharacterInventorySnapshot> takeResult = _lootSyncService.ConfirmTakeLoot(actorId);
        return takeResult.IsSuccess;
    }

    private bool TryEndTurnWithAutoLoot()
    {
        int actorId = ResolveCurrentActorId();
        if (actorId > 0 && _lootSyncService != null && _lootSyncService.HasPendingLootForActor(actorId))
        {
            _lootSyncService.AutoTakeLootOnEndTurn(actorId);
        }

        return _turnFlow.TryEndTurnByAuthority();
    }

    private int ResolveCurrentActorId()
    {
        ICellLayoutOccupant actor = _turnFlow?.CurrentActor;
        if (actor == null || _actorIdentityService == null)
        {
            return 0;
        }

        return _actorIdentityService.GetId(actor);
    }
}
