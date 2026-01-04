public readonly struct CharacterMovedMessage
{
    public ICharacterInstance Character { get; }

    public CharacterMovedMessage(ICharacterInstance character)
    {
        Character = character;
    }
}
