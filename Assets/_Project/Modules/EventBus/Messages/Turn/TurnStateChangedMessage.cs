public readonly struct TurnStateChangedMessage
{
    public ICharacterInstance Character { get; }
    public TurnState State { get; }

    public TurnStateChangedMessage(ICharacterInstance character, TurnState state)
    {
        Character = character;
        State = state;
    }
}
