public sealed class WanderingBehavior : Behavior
{
    public override void ExecuteTurn(
        EnemyInstance enemy,
        IRandomSource randomSource,
        TurnFlow turnFlow,
        CharacterRoster characterRoster
    )
    {
        var currentCell = enemy.Entity?.CurrentCell;
        if (currentCell == null)
        {
            turnFlow.EndTurn();
            return;
        }

        if (turnFlow.StepsRemaining > 0 && currentCell.Neighbors.Count > 0)
        {
            int nextIndex = randomSource.Range(0, currentCell.Neighbors.Count);
            var nextCell = currentCell.Neighbors[nextIndex];
            Cell previousCell = currentCell;
            bool moved = enemy.MoveTo(nextCell);
            if (moved)
            {
                turnFlow.RegisterStep();
                characterRoster.UpdateEntityLayout(enemy.Entity, previousCell);
            }
        }

        turnFlow.EndTurn();
    }
}
