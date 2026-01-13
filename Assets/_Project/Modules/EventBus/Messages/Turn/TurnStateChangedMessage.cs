public readonly struct TurnStateChangedMessage
{
    public CharacterInstance Character { get; }
    public TurnState State { get; }

    public TurnStateChangedMessage(CharacterInstance character, TurnState state)
    {
        Character = character;
        State = state;
    }
}
