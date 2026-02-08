public sealed class BossEnemy : Enemy
{
    public BossEnemy()
        : base(new HunterBehavior(), "Boss")
    {
    }
}
