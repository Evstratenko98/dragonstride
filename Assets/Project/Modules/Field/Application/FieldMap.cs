using System.Collections.Generic;

public class FieldMap
{
    private readonly System.Random _random = new System.Random();

    public int Width { get; private set; }
    public int Height { get; private set; }

    public Cell[,] Grid { get; private set; }

    private readonly List<Link> _links = new List<Link>();

    public void Initialize(int width, int height)
    {
        Width = width;
        Height = height;

        Grid = new Cell[Width, Height];
        _links.Clear();

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                Grid[x, y] = new Cell(x, y);
            }
        }

        InitializeTypes();
    }

    public bool IsInside(int x, int y)
    {
        return x >= 0 && x < Width &&
               y >= 0 && y < Height;
    }

    public Cell GetCell(int x, int y)
    {
        if (!IsInside(x, y))
            return null;

        return Grid[x, y];
    }

    public IEnumerable<Cell> GetAllCells()
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

    public IEnumerable<Cell> GetCellsByType(CellType type)
    {
        foreach (var cell in GetAllCells())
        {
            if (cell.Type == type)
                yield return cell;
        }
    }

    public void InitializeTypes()
    {
        var startCell = GetCell(Width / 2, Height / 2);
        startCell?.SetType(CellType.Start);

        var finishCell = GetRandomFinishCell();
        finishCell?.SetType(CellType.End);
    }

    public IEnumerable<Link> GetAllLinks()
    {
        return _links;
    }

    public bool LinkExists(Cell a, Cell b)
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

    public Link CreateLink(Cell a, Cell b)
    {
        if (a == null || b == null || a == b)
            return null;

        if (LinkExists(a, b))
            return null;

        var link = new Link(a, b);

        a.AddNeighbor(b);
        b.AddNeighbor(a);

        _links.Add(link);
        return link;
    }

    public void Clear()
    {
        Grid = null;
        _links.Clear();
        Width = 0;
        Height = 0;
    }

    private Cell GetRandomFinishCell()
    {
        var candidates = new List<Cell>();

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

            bool neighborOnEdge =
                (IsInside(x - 1, y) && IsEdge(x - 1, y)) ||
                (IsInside(x + 1, y) && IsEdge(x + 1, y)) ||
                (IsInside(x, y - 1) && IsEdge(x, y - 1)) ||
                (IsInside(x, y + 1) && IsEdge(x, y + 1));

            if (neighborOnEdge)
                candidates.Add(cell);
        }

        if (candidates.Count == 0)
            return GetCell(Width - 1, Height - 1);

        return candidates[_random.Next(0, candidates.Count)];
    }

    private bool IsEdge(int x, int y)
    {
        return x == 0 || x == Width - 1 ||
            y == 0 || y == Height - 1;
    }
}
