public readonly struct GameStateChanged
{
    public GameState State { get; }

    public GameStateChanged(GameState state)
    {
        State = state;
    }
}
