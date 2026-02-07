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

    private readonly List<EnemyInstance> _enemies = new();

    public EnemySpawner(
        ConfigScriptableObject config,
        EnemyPrefabs enemyPrefabs,
        FieldRoot fieldRoot,
        CharacterRoster characterRoster,
        TurnActorRegistry turnActorRegistry,
        IEventBus eventBus
    )
    {
        _config = config;
        _enemyPrefabs = enemyPrefabs;
        _fieldRoot = fieldRoot;
        _characterRoster = characterRoster;
        _turnActorRegistry = turnActorRegistry;
        _eventBus = eventBus;
    }

    public EnemyInstance SpawnOnCell(Cell cell)
    {
        if (cell == null)
        {
            return null;
        }

        GameObject prefab = _enemyPrefabs?.WolfPrefab != null
            ? _enemyPrefabs.WolfPrefab
            : _enemyPrefabs?.SlimePrefab;
        if (prefab == null)
        {
            Debug.LogError("[EnemySpawner] Wolf prefab is not configured in GameScope.");
            return null;
        }

        if (_enemies.Any(enemy => enemy?.Entity?.CurrentCell == cell))
        {
            return null;
        }

        var model = new WolfEnemy();
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

        DespawnEnemy(enemy);
        return true;
    }

    private void DespawnEnemy(EnemyInstance enemy)
    {
        _characterRoster.UnregisterLayoutOccupant(enemy);
        _turnActorRegistry.Unregister(enemy);
        enemy.Destroy();
    }
}
