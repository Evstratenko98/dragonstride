public sealed class BossCellOpenHandler : ICellOpenHandler
{
    private readonly EnemySpawner _enemySpawner;

    public CellType CellType => CellType.Boss;

    public BossCellOpenHandler(EnemySpawner enemySpawner)
    {
        _enemySpawner = enemySpawner;
    }

    public bool TryOpen(ICellLayoutOccupant actor, Cell cell)
    {
        _enemySpawner.SpawnBossOnCell(cell);
        return true;
    }
}
