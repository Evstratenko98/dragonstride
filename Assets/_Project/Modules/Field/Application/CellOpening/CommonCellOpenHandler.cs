public sealed class CommonCellOpenHandler : ICellOpenHandler
{
    public CellType CellType => CellType.Common;

    public bool TryOpen(ICellLayoutOccupant actor, Cell cell)
    {
        return false;
    }
}
