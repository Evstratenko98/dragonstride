public abstract class CharacterClass
{
    public abstract string Name { get; }

    // Применяет модификатор к модели
    public abstract void Apply(CharacterModel model);
}
