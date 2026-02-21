public readonly struct FieldCellSnapshot
{
    public int X { get; }
    public int Y { get; }
    public CellType Type { get; }
    public bool IsOpened { get; }
    public bool IsTypeRevealed { get; }

    public FieldCellSnapshot(int x, int y, CellType type, bool isOpened, bool isTypeRevealed)
    {
        X = x;
        Y = y;
        Type = type;
        IsOpened = isOpened;
        IsTypeRevealed = isTypeRevealed;
    }
}
