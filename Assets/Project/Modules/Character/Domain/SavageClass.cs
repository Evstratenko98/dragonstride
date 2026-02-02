public class SavageClass : CharacterClass
{
    public override string Name => "Дикарь";

    public override void Apply(Character model)
    {
        model.AddHealth(10);
        model.AddAttack(5);
        model.AddArmor(-5);
    }
}