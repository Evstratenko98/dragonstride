public class JesterClass : ICharacterClass
{
    public string Name => "Шут";

    public void Apply(CharacterModel model)
    {
        model.AddLuck(0.10f);
        model.AddDodge(0.10f);
        model.AddHealth(-20);
    }
}