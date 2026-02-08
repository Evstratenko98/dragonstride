public sealed class StartCellOpenHandler : ICellOpenHandler
{
    public CellType CellType => CellType.Start;

    public bool TryOpen(ICellLayoutOccupant actor, Cell cell)
    {
        return false;
    }
}
