public readonly struct CharacterMovedMessage
{
    public CharacterInstance Character { get; }
    public CellModel PreviousCell { get; }
    public CellModel CurrentCell { get; }

    public CharacterMovedMessage(CharacterInstance character, CellModel previousCell, CellModel currentCell)
    {
        Character = character;
        PreviousCell = previousCell;
        CurrentCell = currentCell;
    }
}
