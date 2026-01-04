public readonly struct GameStateChangedMessage
{
    public GameState State { get; }

    public GameStateChangedMessage(GameState state)
    {
        State = state;
    }
}
