public readonly struct GameTurnStateChangedMessage
{
    public ICharacterInstance Character { get; }
    public GameTurnState State { get; }

    public GameTurnStateChangedMessage(ICharacterInstance character, GameTurnState state)
    {
        Character = character;
        State = state;
    }
}
