using UnityEngine;

public readonly struct MoveCommandRequested
{
    public Vector2Int Direction { get; }

    public MoveCommandRequested(Vector2Int direction)
    {
        Direction = direction;
    }
}
