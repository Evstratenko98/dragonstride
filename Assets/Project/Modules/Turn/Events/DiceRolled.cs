public readonly struct DiceRolled
{
    public CharacterInstance Character { get; }
    public int Steps { get; }

    public DiceRolled(CharacterInstance character, int steps)
    {
        Character = character;
        Steps = steps;
    }
}
