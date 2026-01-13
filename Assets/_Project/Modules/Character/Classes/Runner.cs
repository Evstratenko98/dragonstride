public class RunnerClass : CharacterClass
{
    public override string Name => "Бегун";

    public override void Apply(CharacterModel model)
    {
        model.AddSpeed(1);
        model.AddInitiative(1);
        model.AddArmor(-5);
    }
}