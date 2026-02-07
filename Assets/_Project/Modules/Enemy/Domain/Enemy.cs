public abstract class Enemy : Entity
{
    public Behavior Behavior { get; }

    protected Enemy(Behavior behavior, string defaultName)
    {
        Behavior = behavior;
        SetName(defaultName);
    }
}
