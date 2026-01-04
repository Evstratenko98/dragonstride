public class BruteClass : ICharacterClass
{
    public string Name => "Громила";

    public void Apply(CharacterModel model)
    {
        model.AddHealth(20);
        model.AddAttack(5);
        model.AddArmor(5);
        model.AddSpeed(-1);
    }
}