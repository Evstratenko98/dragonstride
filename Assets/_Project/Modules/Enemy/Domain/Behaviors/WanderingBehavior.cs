public sealed class WanderingBehavior : Behavior
{
    public override void ExecuteTurn(
        EnemyInstance enemy,
        IRandomSource randomSource,
        TurnFlow turnFlow,
        CharacterRoster characterRoster,
        CrownOwnershipService crownOwnershipService
    )
    {
        var currentCell = enemy.Entity?.CurrentCell;
        if (currentCell == null)
        {
            UnityEngine.Debug.Log("[WanderingBehavior] Enemy has no current cell. Turn ends.");
            turnFlow.EndTurn();
            return;
        }

        if (turnFlow.StepsRemaining > 0 && currentCell.Neighbors.Count > 0)
        {
            UnityEngine.Debug.Log($"[WanderingBehavior] {enemy.Entity.Name} rolled {turnFlow.StepsAvailable} step(s).");
            int nextIndex = randomSource.Range(0, currentCell.Neighbors.Count);
            var nextCell = currentCell.Neighbors[nextIndex];
            Cell previousCell = currentCell;
            bool moved = enemy.MoveTo(nextCell);
            if (moved)
            {
                UnityEngine.Debug.Log($"[WanderingBehavior] {enemy.Entity.Name} moved ({previousCell.X},{previousCell.Y}) -> ({nextCell.X},{nextCell.Y}).");
                turnFlow.RegisterStep();
                characterRoster.UpdateEntityLayout(enemy.Entity, previousCell);
            }
            else
            {
                UnityEngine.Debug.Log($"[WanderingBehavior] {enemy.Entity.Name} failed to move.");
            }
        }

        UnityEngine.Debug.Log($"[WanderingBehavior] {enemy.Entity.Name} turn ends.");
        turnFlow.EndTurn();
    }
}
