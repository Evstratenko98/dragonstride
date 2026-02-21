public sealed class GameCommandPolicyService : IGameCommandPolicyService
{
    public CommandTiming GetTiming(GameCommandType commandType)
    {
        return commandType switch
        {
            GameCommandType.Move => CommandTiming.TurnBound,
            GameCommandType.Attack => CommandTiming.TurnBound,
            GameCommandType.OpenCell => CommandTiming.TurnBound,
            GameCommandType.EndTurn => CommandTiming.TurnBound,
            _ => CommandTiming.TurnBound
        };
    }
}
