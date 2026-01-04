using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public interface ICharacterService
{
    /// <summary>
    /// True, если хотя бы один персонаж находится в движении.
    /// </summary>
    bool IsMoving { get; }

    /// <summary>
    /// Список всех персонажей, зарегистрированных в игре.
    /// </summary>
    IReadOnlyList<ICharacterInstance> AllCharacters { get; }

    /// <summary>
    /// Создаёт нового персонажа на указанной клетке.
    /// </summary>
    ICharacterInstance CreateCharacter(
        ICellModel startCell, 
        string name, 
        int prefabIndex,  
        ICharacterClass characterClass);

    /// <summary>
    /// Пытается выполнить движение выбранного персонажа в указанном направлении.
    /// Возвращает Task, потому что движение может быть асинхронным.
    /// </summary>
    Task TryMove(ICharacterInstance characterInstance, Vector2Int dir);

    void RemoveAllCharacters();
}
