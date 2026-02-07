using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class EnemySpawner
{
    private readonly ConfigScriptableObject _config;
    private readonly GameObject _enemyPrefab;
    private readonly FieldRoot _fieldRoot;
    private readonly CharacterRoster _characterRoster;
    private readonly TurnActorRegistry _turnActorRegistry;
    private readonly IEventBus _eventBus;

    private readonly List<EnemyInstance> _enemies = new();
    private int _enemyCounter = 1;

    public EnemySpawner(
        ConfigScriptableObject config,
        GameObject enemyPrefab,
        FieldRoot fieldRoot,
        CharacterRoster characterRoster,
        TurnActorRegistry turnActorRegistry,
        IEventBus eventBus
    )
    {
        _config = config;
        _enemyPrefab = enemyPrefab;
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

        if (_enemyPrefab == null)
        {
            Debug.LogError("[EnemySpawner] Enemy prefab is not configured in GameScope.");
            return null;
        }

        if (_enemies.Any(enemy => enemy?.Entity?.CurrentCell == cell))
        {
            return null;
        }

        var model = new SlimeEnemy();
        model.SetName($"Enemy {_enemyCounter++}");
        var enemy = new EnemyInstance(_config, model, _enemyPrefab, _fieldRoot, _eventBus);
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
        _enemyCounter = 1;
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
