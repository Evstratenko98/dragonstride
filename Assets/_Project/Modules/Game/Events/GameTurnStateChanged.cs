public readonly struct GameTurnStateChanged
{
    public CharacterInstance Character { get; }
    public GameTurnState State { get; }

    public GameTurnStateChanged(CharacterInstance character, GameTurnState state)
    {
        Character = character;
        State = state;
    }
}
