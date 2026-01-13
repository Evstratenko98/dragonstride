public readonly struct DiceRolledMessage
{
    public CharacterInstance Character { get; }
    public int Steps { get; }

    public DiceRolledMessage(CharacterInstance character, int steps)
    {
        Character = character;
        Steps = steps;
    }
}
