using System;
using System.Collections.Generic;
using UnityEngine;

public class CellModel : ICellModel
{
    public int X { get; }  // индексы!
    public int Y { get; }  // индексы!

    public CellModelType Type { get; set; } = CellModelType.Common;

    private readonly List<ICellModel> _neighbors = new();
    public IReadOnlyList<ICellModel> Neighbors => _neighbors;

    public CellModel(int x, int y, CellModelType type = CellModelType.Common)
    {
        X = x;
        Y = y;
        Type = type;
    }

    public void AddNeighbor(ICellModel neighbor)
    {
        if (!_neighbors.Contains(neighbor))
            _neighbors.Add(neighbor);
    }

    public bool CanMoveTo(ICellModel other)
    {
        return _neighbors.Contains(other);
    }

    public ICellModel GetNeighbor(Vector2Int dir)
    {
        foreach (var n in _neighbors)
        {
            if (n.X == (X + dir.x) && n.Y == (Y + dir.y))
                return n;
        }

        return null;
    }

    public void SetType(CellModelType type)
    {
        Type = type;
    }
}
