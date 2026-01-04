using System.Threading.Tasks;
using UnityEngine;

public interface ICharacterInstance
{
    string Name { get; }
    bool IsMoving { get; }
    CharacterModel Model { get; }
    CharacterView View { get; }

    /// <summary>
    /// Спавнит персонажа на указанной клетке.
    /// </summary>
    void Spawn(ICellModel cell);

    /// <summary>
    /// Асинхронное движение к следующей клетке.
    /// </summary>
    Task MoveTo(ICellModel nextTarget);

    /// <summary>
    /// Возвращает мировые координаты клетки.
    /// </summary>
    Vector3 GetCoordinatesForCellView(int x, int y);

    void Destroy();
}
