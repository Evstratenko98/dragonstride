public abstract class CharacterClass
{
    public abstract string Name { get; }

    public abstract void Apply(Character model);
}
