public sealed class WolfEnemy : Enemy
{
    public WolfEnemy()
        : base(new HunterBehavior(), "Wolf")
    {
    }
}
