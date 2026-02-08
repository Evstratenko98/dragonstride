public readonly struct EntityStatModifier
{
    public int HealthModifier { get; }
    public int AttackModifier { get; }
    public int ArmorModifier { get; }
    public float DodgeModifier { get; }
    public int InitiativeModifier { get; }
    public int SpeedModifier { get; }
    public float LuckModifier { get; }

    public EntityStatModifier(
        int healthModifier = 0,
        int attackModifier = 0,
        int armorModifier = 0,
        float dodgeModifier = 0f,
        int initiativeModifier = 0,
        int speedModifier = 0,
        float luckModifier = 0f)
    {
        HealthModifier = healthModifier;
        AttackModifier = attackModifier;
        ArmorModifier = armorModifier;
        DodgeModifier = dodgeModifier;
        InitiativeModifier = initiativeModifier;
        SpeedModifier = speedModifier;
        LuckModifier = luckModifier;
    }
}
