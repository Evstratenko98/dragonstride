using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class EnemySpawner
{
    private readonly ConfigScriptableObject _config;
    private readonly EnemyPrefabs _enemyPrefabs;
    private readonly FieldRoot _fieldRoot;
    private readonly CharacterRoster _characterRoster;
    private readonly TurnActorRegistry _turnActorRegistry;
    private readonly IEventBus _eventBus;
    private readonly IRandomSource _randomSource;

    private readonly List<EnemyInstance> _enemies = new();
    private readonly List<EnemySpawnOption> _spawnOptions = new();
    private EnemyInstance _bossEnemy;

    public EnemySpawner(
        ConfigScriptableObject config,
        EnemyPrefabs enemyPrefabs,
        FieldRoot fieldRoot,
        CharacterRoster characterRoster,
        TurnActorRegistry turnActorRegistry,
        IEventBus eventBus,
        IRandomSource randomSource
    )
    {
        _config = config;
        _enemyPrefabs = enemyPrefabs;
        _fieldRoot = fieldRoot;
        _characterRoster = characterRoster;
        _turnActorRegistry = turnActorRegistry;
        _eventBus = eventBus;
        _randomSource = randomSource;

        _spawnOptions.Add(new EnemySpawnOption(
            "SlimeEnemy",
            _enemyPrefabs?.SlimeSpawnChancePercent ?? 0,
            () => new SlimeEnemy(),
            () => _enemyPrefabs?.SlimePrefab));

        _spawnOptions.Add(new EnemySpawnOption(
            "WolfEnemy",
            _enemyPrefabs?.WolfSpawnChancePercent ?? 0,
            () => new WolfEnemy(),
            () => _enemyPrefabs?.WolfPrefab));
    }

    public EnemyInstance SpawnOnCell(Cell cell)
    {
        if (cell == null)
        {
            return null;
        }

        if (_enemies.Any(enemy => enemy?.Entity?.CurrentCell == cell))
        {
            return null;
        }

        EnemySpawnOption selectedOption = SelectRandomEnemyOption();
        if (selectedOption == null)
        {
            Debug.LogError("[EnemySpawner] Enemy spawn list is empty or all spawn chances are 0.");
            return null;
        }

        return SpawnEnemy(cell, selectedOption.Name, selectedOption.ModelFactory, selectedOption.PrefabFactory);
    }

    public EnemyInstance SpawnBossOnCell(Cell cell)
    {
        if (cell == null)
        {
            return null;
        }

        if (_bossEnemy?.Entity?.CurrentCell != null)
        {
            return _bossEnemy;
        }

        if (_enemies.Any(enemy => enemy?.Entity?.CurrentCell == cell))
        {
            return null;
        }

        var spawnedBoss = SpawnEnemy(
            cell,
            "BossEnemy",
            () => new BossEnemy(),
            () => _enemyPrefabs?.BossPrefab);

        if (spawnedBoss != null)
        {
            _bossEnemy = spawnedBoss;
        }

        return spawnedBoss;
    }

    public void Reset()
    {
        for (int i = 0; i < _enemies.Count; i++)
        {
            var enemy = _enemies[i];
            if (enemy == null)
            {
                continue;
            }

            DespawnEnemy(enemy);
        }

        _enemies.Clear();
        _bossEnemy = null;
    }

    public bool RemoveEnemy(EnemyInstance enemy)
    {
        if (enemy == null)
        {
            return false;
        }

        if (!_enemies.Remove(enemy))
        {
            return false;
        }

        if (ReferenceEquals(enemy, _bossEnemy))
        {
            _bossEnemy = null;
        }

        DespawnEnemy(enemy);
        return true;
    }

    private EnemyInstance SpawnEnemy(
        Cell cell,
        string enemyName,
        Func<Enemy> modelFactory,
        Func<GameObject> prefabFactory)
    {
        GameObject prefab = prefabFactory?.Invoke();
        if (prefab == null)
        {
            Debug.LogError($"[EnemySpawner] Prefab is not configured for {enemyName}.");
            return null;
        }

        Enemy model = modelFactory?.Invoke();
        if (model == null)
        {
            Debug.LogError($"[EnemySpawner] Model factory returned null for {enemyName}.");
            return null;
        }

        var enemy = new EnemyInstance(_config, model, prefab, _fieldRoot, _eventBus);
        enemy.Spawn(cell);

        if (enemy.View == null)
        {
            return null;
        }

        _enemies.Add(enemy);
        _characterRoster.RegisterLayoutOccupant(enemy);
        _turnActorRegistry.Register(enemy);
        return enemy;
    }

    private EnemySpawnOption SelectRandomEnemyOption()
    {
        var availableOptions = _spawnOptions
            .Where(option => option != null && option.ChancePercent > 0)
            .ToList();

        if (availableOptions.Count == 0)
        {
            return null;
        }

        int totalChance = availableOptions.Sum(option => option.ChancePercent);
        int roll = _randomSource.Range(0, totalChance);
        int cumulativeChance = 0;

        for (int i = 0; i < availableOptions.Count; i++)
        {
            cumulativeChance += availableOptions[i].ChancePercent;
            if (roll < cumulativeChance)
            {
                return availableOptions[i];
            }
        }

        return availableOptions[^1];
    }

    private void DespawnEnemy(EnemyInstance enemy)
    {
        _characterRoster.UnregisterLayoutOccupant(enemy);
        _turnActorRegistry.Unregister(enemy);
        enemy.Destroy();
    }

    private sealed class EnemySpawnOption
    {
        public string Name { get; }
        public int ChancePercent { get; }
        public Func<Enemy> ModelFactory { get; }
        public Func<GameObject> PrefabFactory { get; }

        public EnemySpawnOption(string name, int chancePercent, Func<Enemy> modelFactory, Func<GameObject> prefabFactory)
        {
            Name = name;
            ChancePercent = Mathf.Max(0, chancePercent);
            ModelFactory = modelFactory;
            PrefabFactory = prefabFactory;
        }
    }
}
