using UnityEngine;

public abstract class Enemy : Entity
{
    public Behavior Behavior { get; }
    public override Color HealthBarFillColor => Color.red;

    protected Enemy(Behavior behavior, string defaultName)
    {
        Behavior = behavior;
        SetName(defaultName);
    }
}
