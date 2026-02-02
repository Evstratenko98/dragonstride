public class JesterClass : CharacterClass
{
    public override string Name => "Шут";

    public override void Apply(Character model)
    {
        model.AddLuck(0.10f);
        model.AddDodge(0.10f);
        model.AddHealth(-20);
    }
}