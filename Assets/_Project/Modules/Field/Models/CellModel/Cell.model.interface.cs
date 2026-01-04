using System;
using System.Collections.Generic;
using UnityEngine;

public interface ICellModel
{
    int X { get; }
    int Y { get; }
    CellModelType Type { get; set; }

    IReadOnlyList<ICellModel> Neighbors { get; }

    void AddNeighbor(ICellModel neighbor);

    bool CanMoveTo(ICellModel other);
    ICellModel GetNeighbor(Vector2Int dir);

    void SetType(CellModelType type);
}
