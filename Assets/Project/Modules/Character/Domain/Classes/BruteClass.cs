public class BruteClass : CharacterClass
{
    public override string Name => "Громила";

    public override void Apply(Character model)
    {
        model.AddHealth(20);
        model.AddAttack(5);
        model.AddArmor(5);
        model.AddSpeed(-1);
    }
}