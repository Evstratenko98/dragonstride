public class SavageClass : ICharacterClass
{
    public string Name => "Дикарь";

    public void Apply(CharacterModel model)
    {
        model.AddHealth(10);
        model.AddAttack(5);
        model.AddArmor(-5);
    }
}