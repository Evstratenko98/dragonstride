using System.Collections.Generic;
using UnityEngine;

public sealed class HunterBehavior : Behavior
{
    public override void ExecuteTurn(
        EnemyInstance enemy,
        IRandomSource randomSource,
        TurnFlow turnFlow,
        CharacterRoster characterRoster,
        CrownOwnershipService crownOwnershipService
    )
    {
        Cell startCell = enemy?.Entity?.CurrentCell;
        if (startCell == null)
        {
            Debug.Log("[HunterBehavior] Enemy has no current cell. Turn ends.");
            turnFlow.EndTurn();
            return;
        }

        Debug.Log($"[HunterBehavior] {enemy.Entity.Name} rolled {turnFlow.StepsAvailable} step(s).");

        CharacterInstance target = SelectTarget(startCell, characterRoster, randomSource);
        if (target?.Entity?.CurrentCell == null)
        {
            Debug.Log($"[HunterBehavior] {enemy.Entity.Name} found no reachable target. Turn ends.");
            turnFlow.EndTurn();
            return;
        }

        Debug.Log($"[HunterBehavior] {enemy.Entity.Name} selected target {target.Entity.Name}.");

        while (turnFlow.StepsRemaining > 0)
        {
            if (enemy.Entity.CurrentCell == target.Entity.CurrentCell)
            {
                AttackTarget(enemy, target, randomSource, characterRoster, crownOwnershipService);
                Debug.Log($"[HunterBehavior] {enemy.Entity.Name} ended turn after reaching target.");
                turnFlow.EndTurn();
                return;
            }

            Cell nextCell = GetNextCellToTarget(enemy.Entity.CurrentCell, target.Entity.CurrentCell);
            if (nextCell == null)
            {
                Debug.Log($"[HunterBehavior] {enemy.Entity.Name} cannot find a path to {target.Entity.Name}. Turn ends.");
                turnFlow.EndTurn();
                return;
            }

            Cell previousCell = enemy.Entity.CurrentCell;
            bool moved = enemy.MoveTo(nextCell);
            if (!moved)
            {
                Debug.Log($"[HunterBehavior] {enemy.Entity.Name} failed to move from ({previousCell.X},{previousCell.Y}) to ({nextCell.X},{nextCell.Y}). Turn ends.");
                turnFlow.EndTurn();
                return;
            }

            Debug.Log($"[HunterBehavior] {enemy.Entity.Name} moved ({previousCell.X},{previousCell.Y}) -> ({nextCell.X},{nextCell.Y}). Steps left before consume: {turnFlow.StepsRemaining}.");
            turnFlow.RegisterStep();
            characterRoster.UpdateEntityLayout(enemy.Entity, previousCell);
            Debug.Log($"[HunterBehavior] {enemy.Entity.Name} steps remaining: {turnFlow.StepsRemaining}.");
        }

        if (enemy.Entity.CurrentCell == target.Entity.CurrentCell)
        {
            AttackTarget(enemy, target, randomSource, characterRoster, crownOwnershipService);
            Debug.Log($"[HunterBehavior] {enemy.Entity.Name} ended turn after reaching target.");
            turnFlow.EndTurn();
            return;
        }

        Debug.Log($"[HunterBehavior] {enemy.Entity.Name} did not reach target. Turn ends.");
        turnFlow.EndTurn();
    }

    private static CharacterInstance SelectTarget(Cell startCell, CharacterRoster characterRoster, IRandomSource randomSource)
    {
        if (characterRoster == null)
        {
            return null;
        }

        List<CharacterInstance> characters = new();
        for (int i = 0; i < characterRoster.AllCharacters.Count; i++)
        {
            CharacterInstance candidate = characterRoster.AllCharacters[i];
            if (candidate?.Entity?.CurrentCell != null)
            {
                characters.Add(candidate);
            }
        }

        if (characters.Count == 0)
        {
            return null;
        }

        Dictionary<Cell, int> distances = ComputeDistances(startCell);
        int bestDistance = int.MaxValue;
        List<CharacterInstance> distanceTied = new();

        for (int i = 0; i < characters.Count; i++)
        {
            CharacterInstance candidate = characters[i];
            if (!distances.TryGetValue(candidate.Entity.CurrentCell, out int distance))
            {
                continue;
            }

            if (distance < bestDistance)
            {
                bestDistance = distance;
                distanceTied.Clear();
                distanceTied.Add(candidate);
            }
            else if (distance == bestDistance)
            {
                distanceTied.Add(candidate);
            }
        }

        if (distanceTied.Count == 0)
        {
            return null;
        }

        int minHealth = int.MaxValue;
        List<CharacterInstance> healthTied = new();
        for (int i = 0; i < distanceTied.Count; i++)
        {
            CharacterInstance candidate = distanceTied[i];
            int health = candidate.Entity.Health;
            if (health < minHealth)
            {
                minHealth = health;
                healthTied.Clear();
                healthTied.Add(candidate);
            }
            else if (health == minHealth)
            {
                healthTied.Add(candidate);
            }
        }

        if (healthTied.Count == 1)
        {
            return healthTied[0];
        }

        int index = randomSource.Range(0, healthTied.Count);
        return healthTied[index];
    }

    private static Cell GetNextCellToTarget(Cell from, Cell target)
    {
        if (from == null || target == null || from == target)
        {
            return null;
        }

        var queue = new Queue<Cell>();
        var visited = new HashSet<Cell> { from };
        var previous = new Dictionary<Cell, Cell>();

        queue.Enqueue(from);
        while (queue.Count > 0)
        {
            Cell cell = queue.Dequeue();
            if (cell == target)
            {
                break;
            }

            for (int i = 0; i < cell.Neighbors.Count; i++)
            {
                Cell neighbor = cell.Neighbors[i];
                if (neighbor == null || visited.Contains(neighbor))
                {
                    continue;
                }

                visited.Add(neighbor);
                previous[neighbor] = cell;
                queue.Enqueue(neighbor);
            }
        }

        if (!visited.Contains(target))
        {
            return null;
        }

        Cell cursor = target;
        while (previous.TryGetValue(cursor, out Cell parent))
        {
            if (parent == from)
            {
                return cursor;
            }

            cursor = parent;
        }

        return null;
    }

    private static Dictionary<Cell, int> ComputeDistances(Cell start)
    {
        var distances = new Dictionary<Cell, int>();
        var queue = new Queue<Cell>();

        distances[start] = 0;
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            Cell cell = queue.Dequeue();
            int distance = distances[cell];

            for (int i = 0; i < cell.Neighbors.Count; i++)
            {
                Cell neighbor = cell.Neighbors[i];
                if (neighbor == null || distances.ContainsKey(neighbor))
                {
                    continue;
                }

                distances[neighbor] = distance + 1;
                queue.Enqueue(neighbor);
            }
        }

        return distances;
    }

    private static void AttackTarget(
        EnemyInstance enemy,
        CharacterInstance target,
        IRandomSource randomSource,
        CharacterRoster characterRoster,
        CrownOwnershipService crownOwnershipService
    )
    {
        if (enemy?.Entity == null || target?.Entity == null)
        {
            return;
        }

        Cell enemyCell = enemy.Entity.CurrentCell;
        Cell targetCell = target.Entity.CurrentCell;
        if (enemyCell?.Type == CellType.Start || targetCell?.Type == CellType.Start)
        {
            Debug.Log($"[HunterBehavior] Attack is blocked on Start cell: {enemy.Entity.Name} -> {target.Entity.Name}.");
            return;
        }

        float dodgeRoll = randomSource.Range(0, 100);
        float dodgeThreshold = target.Entity.DodgeChance * 100f;
        if (dodgeRoll < dodgeThreshold)
        {
            Debug.Log($"[HunterBehavior] {target.Entity.Name} dodged the attack from {enemy.Entity.Name}.");
            return;
        }

        int damage = Mathf.Max(0, enemy.Entity.Attack - target.Entity.Armor);
        if (damage <= 0)
        {
            Debug.Log($"[HunterBehavior] {enemy.Entity.Name} attacked {target.Entity.Name}, but dealt no damage.");
            return;
        }

        int newHealth = Mathf.Max(0, target.Entity.Health - damage);
        target.Entity.SetHealth(newHealth);
        Debug.Log($"[HunterBehavior] {target.Entity.Name} took {damage} damage from {enemy.Entity.Name}. Health: {newHealth}.");

        if (newHealth == 0)
        {
            crownOwnershipService?.OnEntityKilled(enemy, target);
            bool reborn = characterRoster.TryRebirthCharacter(target);
            if (reborn)
            {
                Debug.Log($"[HunterBehavior] {target.Entity.Name} has been reborn at the start cell.");
            }
        }
    }
}
