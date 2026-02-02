public class RunnerClass : CharacterClass
{
    public override string Name => "Бегун";

    public override void Apply(Character model)
    {
        model.AddSpeed(1);
        model.AddInitiative(1);
        model.AddArmor(-5);
    }
}