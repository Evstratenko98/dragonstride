using System.Collections.Generic;

namespace Project.Modules.Field.Domain;

public sealed class FieldGrid
{
    private readonly List<Link> _links = new();

    public int Width { get; }
    public int Height { get; }

    public Cell[,] Grid { get; }

    public FieldGrid(int width, int height)
    {
        Width = width;
        Height = height;

        Grid = new Cell[Width, Height];

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                Grid[x, y] = new Cell(x, y);
            }
        }
    }

    public bool IsInside(int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }

    public Cell GetCell(int x, int y)
    {
        if (!IsInside(x, y))
        {
            return null;
        }

        return Grid[x, y];
    }

    public IEnumerable<Cell> GetAllCells()
    {
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
            {
                yield return cell;
            }
        }
    }

    public IEnumerable<Link> GetAllLinks()
    {
        return _links;
    }

    public bool LinkExists(Cell a, Cell b)
    {
        if (a == null || b == null || a == b)
        {
            return false;
        }

        foreach (var link in _links)
        {
            if (link.Connects(a, b))
            {
                return true;
            }
        }

        return false;
    }

    public Link CreateLink(Cell a, Cell b)
    {
        if (a == null || b == null || a == b)
        {
            return null;
        }

        if (LinkExists(a, b))
        {
            return null;
        }

        var link = new Link(a, b);

        a.AddNeighbor(b);
        b.AddNeighbor(a);

        _links.Add(link);
        return link;
    }
}
