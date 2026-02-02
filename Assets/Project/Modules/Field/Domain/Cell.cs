using System.Collections.Generic;

public sealed class Cell
{
    public int X { get; }
    public int Y { get; }

    public CellType Type { get; private set; }
    public CellVisibility VisibilityState { get; private set; }

    private readonly List<Cell> _neighbors = new();
    public IReadOnlyList<Cell> Neighbors => _neighbors;

    public Cell(int x, int y, CellType type = CellType.Common)
    {
        X = x;
        Y = y;
        Type = type;
        VisibilityState = CellVisibility.Unseen;
    }

    public void AddNeighbor(Cell neighbor)
    {
        if (neighbor == null)
        {
            return;
        }

        if (!_neighbors.Contains(neighbor))
        {
            _neighbors.Add(neighbor);
        }
    }

    public bool CanMoveTo(Cell other)
    {
        return other != null && _neighbors.Contains(other);
    }

    public Cell GetNeighbor(int dx, int dy)
    {
        foreach (var neighbor in _neighbors)
        {
            if (neighbor.X == X + dx && neighbor.Y == Y + dy)
            {
                return neighbor;
            }
        }

        return null;
    }

    public void SetType(CellType type)
    {
        Type = type;
    }

    public void SetVisibility(CellVisibility state)
    {
        VisibilityState = state;
    }
}
