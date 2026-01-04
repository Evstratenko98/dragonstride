using System.Collections.Generic;

public interface IFieldService
{
    int Width { get; }
    int Height { get; }

    /// <summary>
    /// Двумерная сетка моделей клеток.
    /// </summary>
    ICellModel[,] Grid { get; }

    /// <summary>
    /// Инициализирует поле и создаёт сетку моделей.
    /// </summary>
    void Initialize(int width, int height);

    /// <summary>
    /// Возвращает модель клетки по координатам или null, если вне поля.
    /// </summary>
    ICellModel GetCell(int x, int y);

    /// <summary>
    /// Перебор всех клеток.
    /// </summary>
    IEnumerable<ICellModel> GetAllCells();

    IEnumerable<ICellModel> GetCellsByType(CellModelType type);

    /// <summary>
    /// Все связи-модели.
    /// </summary>
    IEnumerable<ILinkModel> GetAllLinks();

    /// <summary>
    /// Находится ли координата внутри границ поля.
    /// </summary>
    bool IsInside(int x, int y);

    /// <summary>
    /// Проверка существования связи между двумя клетками.
    /// </summary>
    bool LinkExists(ICellModel a, ICellModel b);

    /// <summary>
    /// Создать связь между клетками (если её ещё нет).
    /// </summary>
    LinkModel CreateLink(ICellModel a, ICellModel b);

    void Clear();
}
