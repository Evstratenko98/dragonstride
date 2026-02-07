public sealed class SlimeEnemy : Enemy
{
    public SlimeEnemy()
        : base(new WanderingBehavior())
    {
    }

    public override int CalculateTurnSteps(int diceRoll)
    {
        return 1;
    }
}
