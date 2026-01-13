using System.Collections.Generic;

public class FieldService
{
    public int Width { get; private set; }
    public int Height { get; private set; }

    public CellModel[,] Grid { get; private set; }

    private readonly List<LinkModel> _links = new List<LinkModel>();

    // -----------------------------------
    //        ИНИЦИАЛИЗАЦИЯ ПОЛЯ
    // -----------------------------------
    public void Initialize(int width, int height)
    {
        Width = width;
        Height = height;

        Grid = new CellModel[Width, Height];
        _links.Clear();

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                Grid[x, y] = new CellModel(x, y);
            }
        }

        // Назначение типов
        InitializeTypes();
    }

    // -----------------------------------
    //         ДОСТУП К КЛЕТКАМ
    // -----------------------------------
    public bool IsInside(int x, int y)
    {
        return x >= 0 && x < Width &&
               y >= 0 && y < Height;
    }

    public CellModel GetCell(int x, int y)
    {
        if (!IsInside(x, y))
            return null;

        return Grid[x, y];
    }

    public IEnumerable<CellModel> GetAllCells()
    {
        if (Grid == null)
            yield break;

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                yield return Grid[x, y];
            }
        }
    }

    public IEnumerable<CellModel> GetCellsByType(CellModelType type)
    {
        foreach (var cell in GetAllCells())
        {
            if (cell.Type == type)
                yield return cell;
        }
    }

    // -----------------------------------
    //          РАБОТА С ТИПАМИ
    // -----------------------------------
    public void InitializeTypes()
    {
        CellModel startCell = this.GetCell(Width / 2, Height / 2);
        startCell.Type = CellModelType.Start;

        CellModel finishCell = GetRandomFinishCell();
        finishCell.Type = CellModelType.End;
    }


    // -----------------------------------
    //          РАБОТА СО СВЯЗЯМИ
    // -----------------------------------
    public IEnumerable<LinkModel> GetAllLinks()
    {
        return _links;
    }

    public bool LinkExists(CellModel a, CellModel b)
    {
        if (a == null || b == null || a == b)
            return false;

        foreach (var link in _links)
        {
            if (link.Connects(a, b))
                return true;
        }

        return false;
    }

    public LinkModel CreateLink(CellModel a, CellModel b)
    {
        if (a == null || b == null || a == b)
            return null;

        if (LinkExists(a, b))
            return null;

        LinkModel link = new LinkModel(a, b);

        // Обновляем модельные связи соседей
        a.AddNeighbor(b);
        b.AddNeighbor(a);

        _links.Add(link);
        return link;
    }

    private CellModel GetRandomFinishCell()
    {
        List<CellModel> candidates = new List<CellModel>();

        foreach (var cell in GetAllCells())
        {
            int x = cell.X;
            int y = cell.Y;

            bool isOnEdge =
                x == 0 || x == Width - 1 ||
                y == 0 || y == Height - 1;

            if (isOnEdge)
            {
                candidates.Add(cell);
                continue;
            }

            // Проверяем соседей
            bool neighborOnEdge =
                (IsInside(x - 1, y) && IsEdge(x - 1, y)) ||
                (IsInside(x + 1, y) && IsEdge(x + 1, y)) ||
                (IsInside(x, y - 1) && IsEdge(x, y - 1)) ||
                (IsInside(x, y + 1) && IsEdge(x, y + 1));

            if (neighborOnEdge)
                candidates.Add(cell);
        }

        if (candidates.Count == 0)
            return GetCell(Width - 1, Height - 1); // fallback

        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }

    private bool IsEdge(int x, int y)
    {
        return x == 0 || x == Width - 1 ||
            y == 0 || y == Height - 1;
    }

    public void Clear()
    {
        Grid = null;
        _links.Clear();
        Width = 0;
        Height = 0;
    }
}
