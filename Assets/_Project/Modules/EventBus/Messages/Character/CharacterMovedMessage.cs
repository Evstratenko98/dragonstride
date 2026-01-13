public readonly struct CharacterMovedMessage
{
    public CharacterInstance Character { get; }

    public CharacterMovedMessage(CharacterInstance character)
    {
        Character = character;
    }
}
