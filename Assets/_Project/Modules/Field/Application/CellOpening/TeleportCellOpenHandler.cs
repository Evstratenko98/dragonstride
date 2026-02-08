public sealed class TeleportCellOpenHandler : ICellOpenHandler
{
    public CellType CellType => CellType.Teleport;

    public bool TryOpen(ICellLayoutOccupant actor, Cell cell)
    {
        // TODO: implement teleport opening logic.
        return true;
    }
}
