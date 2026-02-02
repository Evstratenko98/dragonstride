public readonly struct CharacterMoved
{
    public CharacterInstance Character { get; }

    public CharacterMoved(CharacterInstance character)
    {
        Character = character;
    }
}
