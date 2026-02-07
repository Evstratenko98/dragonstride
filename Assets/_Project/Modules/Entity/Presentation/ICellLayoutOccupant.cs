using UnityEngine;

public interface ICellLayoutOccupant
{
    Entity Entity { get; }
    void MoveToPosition(Vector3 targetPosition, float speed);
}
