using System.Collections.Generic;
using UnityEngine;

public class CharacterLayoutService
{
    private readonly ConfigScriptableObject _config;

    public CharacterLayoutService(ConfigScriptableObject config)
    {
        _config = config;
    }

    public CharacterLayoutData CalculateLayout(int count)
    {
        var offsets = new List<Vector3>();
        if (count <= 0)
        {
            return new CharacterLayoutData(offsets, 1f);
        }

        int gridSize = Mathf.CeilToInt(Mathf.Sqrt(count));
        float cellSize = _config.CELL_SIZE;
        float slotSize = cellSize / gridSize;
        float scale = Mathf.Min(1f, slotSize / cellSize);
        float startOffset = -cellSize * 0.5f + slotSize * 0.5f;

        for (int i = 0; i < count; i++)
        {
            int row = i / gridSize;
            int col = i % gridSize;
            float offsetX = startOffset + col * slotSize;
            float offsetZ = startOffset + row * slotSize;
            offsets.Add(new Vector3(offsetX, 0f, offsetZ));
        }

        return new CharacterLayoutData(offsets, scale);
    }

    public void ApplyLayout(CellModel cell, IReadOnlyList<CharacterInstance> characters)
    {
        if (cell == null || characters == null || characters.Count == 0)
        {
            return;
        }

        CharacterLayoutData layout = CalculateLayout(characters.Count);
        Vector3 cellCenter = new Vector3(
            cell.X * _config.CELL_SIZE,
            _config.CHARACTER_HEIGHT,
            cell.Y * _config.CELL_SIZE
        );

        for (int i = 0; i < characters.Count; i++)
        {
            CharacterInstance character = characters[i];
            if (character?.View == null)
            {
                continue;
            }

            Vector3 position = cellCenter + layout.LocalOffsets[i];
            character.View.SetPositionAndScale(position, layout.Scale);
        }
    }
}

public readonly struct CharacterLayoutData
{
    public IReadOnlyList<Vector3> LocalOffsets { get; }
    public float Scale { get; }

    public CharacterLayoutData(IReadOnlyList<Vector3> localOffsets, float scale)
    {
        LocalOffsets = localOffsets;
        Scale = scale;
    }
}
