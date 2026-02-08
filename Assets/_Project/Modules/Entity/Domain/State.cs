using System;

public class State
{
    public const int PermanentDuration = -1;

    public string Id { get; }
    public string Name { get; }
    public EntityStatModifier Modifiers { get; }
    public int RemainingRounds { get; private set; }
    public bool IsPermanent => RemainingRounds == PermanentDuration;

    public State(string id, string name, EntityStatModifier modifiers, int durationInRounds = PermanentDuration)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("State id cannot be empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("State name cannot be empty.", nameof(name));
        }

        if (durationInRounds == 0 || durationInRounds < PermanentDuration)
        {
            throw new ArgumentOutOfRangeException(nameof(durationInRounds), "State duration must be positive or -1 for permanent.");
        }

        Id = id;
        Name = name;
        Modifiers = modifiers;
        RemainingRounds = durationInRounds;
    }

    internal void ApplyTo(Entity entity)
    {
        if (entity == null)
        {
            return;
        }

        ApplyModifiers(entity, 1);
        OnApplied(entity);
    }

    internal void RemoveFrom(Entity entity)
    {
        if (entity == null)
        {
            return;
        }

        OnRemoved(entity);
        ApplyModifiers(entity, -1);
    }

    internal bool TickRound(Entity entity)
    {
        if (entity == null)
        {
            return false;
        }

        OnRoundPassed(entity);

        if (IsPermanent)
        {
            return false;
        }

        RemainingRounds--;
        return RemainingRounds <= 0;
    }

    protected virtual void OnApplied(Entity entity)
    {
    }

    protected virtual void OnRemoved(Entity entity)
    {
    }

    protected virtual void OnRoundPassed(Entity entity)
    {
    }

    private void ApplyModifiers(Entity entity, int sign)
    {
        entity.AddHealth(Modifiers.HealthModifier * sign);
        entity.AddAttack(Modifiers.AttackModifier * sign);
        entity.AddArmor(Modifiers.ArmorModifier * sign);
        entity.AddDodge(Modifiers.DodgeModifier * sign);
        entity.AddInitiative(Modifiers.InitiativeModifier * sign);
        entity.AddSpeed(Modifiers.SpeedModifier * sign);
        entity.AddLuck(Modifiers.LuckModifier * sign);
    }
}
