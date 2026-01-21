using System.Collections.Generic;
using UnityEngine;

public class CharacterCellLayoutService
{
    private readonly ConfigScriptableObject _config;

    public CharacterCellLayoutService(ConfigScriptableObject config)
    {
        _config = config;
    }

    public void ApplyLayout(CellModel cell, IReadOnlyList<CharacterInstance> occupants)
    {
        if (cell == null || occupants == null || occupants.Count == 0)
        {
            return;
        }

        Vector3 cellCenter = new Vector3(
            cell.X * _config.CELL_SIZE,
            _config.CHARACTER_HEIGHT,
            cell.Y * _config.CELL_SIZE
        );

        if (occupants.Count == 1)
        {
            occupants[0].View?.SetPosition(cellCenter);
            return;
        }

        float radius = _config.CELL_SIZE * 0.25f;
        float angleStep = Mathf.PI * 2f / occupants.Count;

        for (int i = 0; i < occupants.Count; i++)
        {
            float angle = angleStep * i;
            float offsetX = Mathf.Cos(angle) * radius;
            float offsetZ = Mathf.Sin(angle) * radius;
            Vector3 position = new Vector3(cellCenter.x + offsetX, cellCenter.y, cellCenter.z + offsetZ);
            occupants[i].View?.SetPosition(position);
        }
    }
}
