public abstract class Enemy : Entity
{
    public Behavior Behavior { get; }

    protected Enemy(Behavior behavior)
    {
        Behavior = behavior;
    }
}
