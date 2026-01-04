public interface ICharacterFactory
{
    ICharacterInstance Create(string name, int prefabIndex, ICharacterClass characterClass);
}
