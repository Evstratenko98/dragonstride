public readonly struct DiceRolledMessage
{
    public ICharacterInstance Character { get; }
    public int Steps { get; }

    public DiceRolledMessage(ICharacterInstance character, int steps)
    {
        Character = character;
        Steps = steps;
    }
}
