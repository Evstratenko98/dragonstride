public class RunnerClass : ICharacterClass
{
    public string Name => "Бегун";

    public void Apply(CharacterModel model)
    {
        model.AddSpeed(1);
        model.AddInitiative(1);
        model.AddArmor(-5);
    }
}