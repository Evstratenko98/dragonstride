using System.Collections.Generic;
using UnityEngine;

public class EntityLayout
{
    private readonly ConfigScriptableObject _config;

    public EntityLayout(ConfigScriptableObject config)
    {
        _config = config;
    }

    public void ApplyLayout(Cell cell, IReadOnlyList<ICellLayoutOccupant> occupants)
    {
        if (cell == null || occupants == null || occupants.Count == 0)
        {
            return;
        }

        Vector3 cellCenter = new Vector3(
            cell.X * _config.CellDistance,
            _config.CHARACTER_HEIGHT,
            cell.Y * _config.CellDistance
        );

        if (occupants.Count == 1)
        {
            occupants[0].MoveToPosition(cellCenter, _config.ENTITY_LAYOUT_SPEED);
            return;
        }

        float radius = Mathf.Max(0.1f, _config.ENTITY_LAYOUT_RADIUS);
        float angleStep = Mathf.PI * 2f / occupants.Count;

        for (int i = 0; i < occupants.Count; i++)
        {
            float angle = angleStep * i;
            float offsetX = Mathf.Cos(angle) * radius;
            float offsetZ = Mathf.Sin(angle) * radius;
            Vector3 position = new Vector3(cellCenter.x + offsetX, cellCenter.y, cellCenter.z + offsetZ);
            occupants[i].MoveToPosition(position, _config.ENTITY_LAYOUT_SPEED);
        }
    }
}
