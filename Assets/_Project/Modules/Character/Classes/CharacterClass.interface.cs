public interface ICharacterClass
{
    string Name { get; }

    // Применяет модификатор к модели
    void Apply(CharacterModel model);
}