using System.Collections.Generic;

public interface IMazeGenerator
{
    /// <summary>
    /// Запускает генерацию лабиринта.
    /// На вход получает сервис поля (сетку), на выходе — изменённые модели клеток и связей.
    /// </summary>
    void Generate(IFieldService field, float extraConnectionChance);
}
