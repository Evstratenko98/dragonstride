public interface ICellOpenHandler
{
    CellType CellType { get; }
    bool TryOpen(ICellLayoutOccupant actor, Cell cell);
}
