public readonly struct GameTurnStateChangedMessage
{
    public CharacterInstance Character { get; }
    public GameTurnState State { get; }

    public GameTurnStateChangedMessage(CharacterInstance character, GameTurnState state)
    {
        Character = character;
        State = state;
    }
}
