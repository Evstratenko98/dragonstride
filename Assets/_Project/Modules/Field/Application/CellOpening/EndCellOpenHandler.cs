public sealed class EndCellOpenHandler : ICellOpenHandler
{
    public CellType CellType => CellType.End;

    public bool TryOpen(ICellLayoutOccupant actor, Cell cell)
    {
        return false;
    }
}
