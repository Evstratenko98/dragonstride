public readonly struct OpenedCellSnapshot
{
    public int X { get; }
    public int Y { get; }
    public string CellType { get; }

    public OpenedCellSnapshot(int x, int y, string cellType)
    {
        X = x;
        Y = y;
        CellType = cellType;
    }
}
