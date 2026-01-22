using System;
using System.Collections.Generic;
using UnityEngine;

public class CellModel
{
    public int X { get; }  // индексы!
    public int Y { get; }  // индексы!

    public CellModelType Type { get; set; } = CellModelType.Common;
    public CellVisibilityState VisibilityState { get; private set; } = CellVisibilityState.Unseen;

    private readonly List<CellModel> _neighbors = new();
    public IReadOnlyList<CellModel> Neighbors => _neighbors;

    public CellModel(int x, int y, CellModelType type = CellModelType.Common)
    {
        X = x;
        Y = y;
        Type = type;
    }

    public void AddNeighbor(CellModel neighbor)
    {
        if (!_neighbors.Contains(neighbor))
            _neighbors.Add(neighbor);
    }

    public bool CanMoveTo(CellModel other)
    {
        return _neighbors.Contains(other);
    }

    public CellModel GetNeighbor(Vector2Int dir)
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

    public void SetVisibility(CellVisibilityState state)
    {
        VisibilityState = state;
    }
}
