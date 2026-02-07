public readonly struct CharacterClicked
{
    public CharacterInstance Character { get; }

    public CharacterClicked(CharacterInstance character)
    {
        Character = character;
    }
}
