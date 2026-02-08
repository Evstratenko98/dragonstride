public sealed class FightCellOpenHandler : ICellOpenHandler
{
    private readonly EnemySpawner _enemySpawner;

    public CellType CellType => CellType.Fight;

    public FightCellOpenHandler(EnemySpawner enemySpawner)
    {
        _enemySpawner = enemySpawner;
    }

    public bool TryOpen(ICellLayoutOccupant actor, Cell cell)
    {
        _enemySpawner.SpawnOnCell(cell);
        return true;
    }
}
