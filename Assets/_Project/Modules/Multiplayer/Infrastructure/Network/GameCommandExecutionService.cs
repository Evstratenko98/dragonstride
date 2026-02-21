using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public sealed class GameCommandExecutionService : IGameCommandExecutionService
{
    private readonly CharacterMovementDriver _movementDriver;
    private readonly AttackDriver _attackDriver;
    private readonly TurnFlow _turnFlow;
    private readonly SemaphoreSlim _executionLock = new(1, 1);

    public GameCommandExecutionService(
        CharacterMovementDriver movementDriver,
        AttackDriver attackDriver,
        TurnFlow turnFlow)
    {
        _movementDriver = movementDriver;
        _attackDriver = attackDriver;
        _turnFlow = turnFlow;
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
                GameCommandType.EndTurn => _turnFlow.TryEndTurnByAuthority(),
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
}
