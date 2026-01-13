public class SamuraiClass : CharacterClass
{
    public override string Name => "Самурай";

    public override void Apply(CharacterModel model)
    {
        model.AddAttack(5);
        model.AddDodge(0.10f);
        model.AddArmor(-5);
        model.AddHealth(-10);
    }
}