using System;
using UnityEngine;

public sealed class TurnAuthorityService : ITurnAuthorityService
{
    private readonly TurnFlow _turnFlow;
    private readonly IActorIdentityService _actorIdentityService;
    private readonly IMatchRuntimeRoleService _runtimeRoleService;
    private readonly IMatchPauseService _matchPauseService;

    public TurnAuthorityService(
        TurnFlow turnFlow,
        IActorIdentityService actorIdentityService,
        IMatchRuntimeRoleService runtimeRoleService,
        IMatchPauseService matchPauseService)
    {
        _turnFlow = turnFlow;
        _actorIdentityService = actorIdentityService;
        _runtimeRoleService = runtimeRoleService;
        _matchPauseService = matchPauseService;
    }

    public CommandValidationResult Validate(GameCommandEnvelope command)
    {
        if (_runtimeRoleService == null || !_runtimeRoleService.IsOnlineMatch)
        {
            return CommandValidationResult.Success();
        }

        if (_matchPauseService != null && _matchPauseService.IsPaused)
        {
            return CommandValidationResult.Failure("match_paused", _matchPauseService.Reason);
        }

        if (command.CommandType == GameCommandType.None)
        {
            return CommandValidationResult.Failure("invalid_command", "Command type is not set.");
        }

        if (string.IsNullOrWhiteSpace(command.PlayerId))
        {
            return CommandValidationResult.Failure("missing_player", "Command player id is empty.");
        }

        CharacterInstance currentCharacter = _turnFlow?.CurrentActor as CharacterInstance;
        if (currentCharacter == null)
        {
            return CommandValidationResult.Failure("not_player_turn", "Current turn actor is not a player character.");
        }

        if (!string.Equals(currentCharacter.PlayerId, command.PlayerId, StringComparison.Ordinal))
        {
            return CommandValidationResult.Failure("not_your_turn", "Command sender does not own the current actor.");
        }

        return command.CommandType switch
        {
            GameCommandType.Move => ValidateMove(command.Direction),
            GameCommandType.Attack => ValidateAttack(command.TargetActorId),
            GameCommandType.OpenCell => ValidateOpenCell(),
            GameCommandType.EndTurn => ValidateEndTurn(),
            _ => CommandValidationResult.Failure("unsupported_command", "Unsupported command type.")
        };
    }

    private CommandValidationResult ValidateMove(Vector2Int direction)
    {
        if (_turnFlow.State != TurnState.ActionSelection && _turnFlow.State != TurnState.Movement)
        {
            return CommandValidationResult.Failure("invalid_turn_state", "Move is available only during action/movement phases.");
        }

        if (_turnFlow.StepsRemaining <= 0)
        {
            return CommandValidationResult.Failure("no_steps", "No movement steps remaining.");
        }

        bool isCardinal = direction == Vector2Int.left ||
                          direction == Vector2Int.right ||
                          direction == Vector2Int.up ||
                          direction == Vector2Int.down;
        if (!isCardinal)
        {
            return CommandValidationResult.Failure("invalid_direction", "Direction must be cardinal.");
        }

        return CommandValidationResult.Success();
    }

    private CommandValidationResult ValidateAttack(int targetActorId)
    {
        if (_turnFlow.State != TurnState.ActionSelection)
        {
            return CommandValidationResult.Failure("invalid_turn_state", "Attack is available only in action selection phase.");
        }

        if (targetActorId <= 0)
        {
            return CommandValidationResult.Failure("invalid_target", "Target actor id is invalid.");
        }

        if (_actorIdentityService == null || !_actorIdentityService.TryGetActor(targetActorId, out ICellLayoutOccupant target) || target == null)
        {
            return CommandValidationResult.Failure("target_not_found", "Target actor was not found.");
        }

        return CommandValidationResult.Success();
    }

    private CommandValidationResult ValidateOpenCell()
    {
        if (_turnFlow.State != TurnState.ActionSelection && _turnFlow.State != TurnState.Movement)
        {
            return CommandValidationResult.Failure("invalid_turn_state", "Open cell is unavailable in the current turn state.");
        }

        return CommandValidationResult.Success();
    }

    private CommandValidationResult ValidateEndTurn()
    {
        if (_turnFlow.State != TurnState.ActionSelection &&
            _turnFlow.State != TurnState.Movement &&
            _turnFlow.State != TurnState.Attack &&
            _turnFlow.State != TurnState.OpenCell &&
            _turnFlow.State != TurnState.Trade)
        {
            return CommandValidationResult.Failure("invalid_turn_state", "End turn is unavailable in the current turn state.");
        }

        return CommandValidationResult.Success();
    }
}
