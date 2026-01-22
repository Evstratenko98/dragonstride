using System.Collections.Generic;

public class FogOfWarService
{
    private readonly HashSet<CellModel> _visibleCells = new();

    public void Initialize(FieldService fieldService)
    {
        if (fieldService == null)
            return;

        _visibleCells.Clear();

        foreach (var cell in fieldService.GetAllCells())
        {
            cell.SetFogState(FogVisibilityState.Unseen);
        }
    }

    public void RevealFromCharacters(IReadOnlyList<CharacterInstance> characters, int visionRange)
    {
        if (characters == null)
            return;

        foreach (var cell in _visibleCells)
        {
            if (cell.FogState == FogVisibilityState.Visible)
                cell.SetFogState(FogVisibilityState.Seen);
        }

        _visibleCells.Clear();

        foreach (var character in characters)
        {
            RevealFromCell(character?.Model?.CurrentCell, visionRange);
        }
    }

    private void RevealFromCell(CellModel origin, int visionRange)
    {
        if (origin == null)
            return;

        var visited = new HashSet<CellModel>();
        var queue = new Queue<(CellModel cell, int depth)>();

        queue.Enqueue((origin, 0));
        visited.Add(origin);

        while (queue.Count > 0)
        {
            var (cell, depth) = queue.Dequeue();

            cell.SetFogState(FogVisibilityState.Visible);
            _visibleCells.Add(cell);

            if (depth >= visionRange)
                continue;

            foreach (var neighbor in cell.Neighbors)
            {
                if (neighbor == null || visited.Contains(neighbor))
                    continue;

                visited.Add(neighbor);
                queue.Enqueue((neighbor, depth + 1));
            }
        }
    }
}
