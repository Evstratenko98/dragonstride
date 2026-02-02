public readonly struct TurnPhaseChanged
{
    public CharacterInstance Character { get; }
    public TurnState State { get; }

    public TurnPhaseChanged(CharacterInstance character, TurnState state)
    {
        Character = character;
        State = state;
    }
}
