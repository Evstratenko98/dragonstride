using System;
using System.Collections.Generic;

public sealed class FieldGenerator
{
    private static readonly (CellType Type, float Weight)[] RandomCellTypeWeights =
    {
        (CellType.Loot, 0.25f),
        (CellType.Fight, 0.25f)
    };

    private readonly Random _random = new Random();
    
    public FieldGenerator()
    {
        _random = new Random();
    }

    public FieldGrid Create(int width, int height, float extraConnectionChance)
    {
        var field = new FieldGrid(width, height);
        GeneratePerfectMaze(field);
        AddExtraConnections(field, extraConnectionChance);
        EnsureFullConnectivity(field);
        AssignStartAndEnd(field);
        AssignSpecialCells(field);
        return field;
    }

    private void AssignStartAndEnd(FieldGrid field)
    {
        var startCell = field.GetCell(field.Width / 2, field.Height / 2);
        startCell?.SetType(CellType.Start);
        startCell?.RevealType();
        startCell?.MarkOpened();

        var finishCell = GetRandomFinishCell(field);
        finishCell?.SetType(CellType.End);
    }

    private void AssignSpecialCells(FieldGrid field)
    {
        var candidates = new List<Cell>();

        foreach (var cell in field.GetAllCells())
        {
            if (cell.Type == CellType.Common)
            {
                candidates.Add(cell);
            }
        }

        for (int i = 0; i < candidates.Count; i++)
        {
            var randomType = RollRandomCellType();

            if (randomType != CellType.Common)
            {
                candidates[i].SetType(randomType);
            }
        }
    }

    private CellType RollRandomCellType()
    {
        float roll = (float)_random.NextDouble();
        float cumulative = 0f;

        for (int i = 0; i < RandomCellTypeWeights.Length; i++)
        {
            cumulative += RandomCellTypeWeights[i].Weight;
            if (roll < cumulative)
            {
                return RandomCellTypeWeights[i].Type;
            }
        }

        return CellType.Common;
    }

    private Cell GetRandomFinishCell(FieldGrid field)
    {
        var candidates = new List<Cell>();

        foreach (var cell in field.GetAllCells())
        {
            int x = cell.X;
            int y = cell.Y;

            if (IsEdge(field, x, y))
            {
                candidates.Add(cell);
                continue;
            }

            bool neighborOnEdge =
                (field.IsInside(x - 1, y) && IsEdge(field, x - 1, y)) ||
                (field.IsInside(x + 1, y) && IsEdge(field, x + 1, y)) ||
                (field.IsInside(x, y - 1) && IsEdge(field, x, y - 1)) ||
                (field.IsInside(x, y + 1) && IsEdge(field, x, y + 1));

            if (neighborOnEdge)
            {
                candidates.Add(cell);
            }
        }

        if (candidates.Count == 0)
        {
            return field.GetCell(field.Width - 1, field.Height - 1);
        }

        return candidates[_random.Next(candidates.Count)];
    }

    private static bool IsEdge(FieldGrid field, int x, int y)
    {
        return x == 0 || x == field.Width - 1 || y == 0 || y == field.Height - 1;
    }

    private void GeneratePerfectMaze(FieldGrid field)
    {
        bool[,] visited = new bool[field.Width, field.Height];
        var stack = new Stack<(int x, int y)>();

        stack.Push((0, 0));
        visited[0, 0] = true;

        while (stack.Count > 0)
        {
            var current = stack.Peek();
            var unvisited = GetUnvisitedNeighbors(current.x, current.y, visited, field);

            if (unvisited.Count == 0)
            {
                stack.Pop();
                continue;
            }

            var next = unvisited[_random.Next(unvisited.Count)];
            var a = field.GetCell(current.x, current.y);
            var b = field.GetCell(next.x, next.y);
            field.CreateLink(a, b);

            visited[next.x, next.y] = true;
            stack.Push(next);
        }
    }

    private List<(int x, int y)> GetUnvisitedNeighbors(int x, int y, bool[,] visited, FieldGrid field)
    {
        var list = new List<(int, int)>();

        if (x > 0 && !visited[x - 1, y]) list.Add((x - 1, y));
        if (x < field.Width - 1 && !visited[x + 1, y]) list.Add((x + 1, y));
        if (y > 0 && !visited[x, y - 1]) list.Add((x, y - 1));
        if (y < field.Height - 1 && !visited[x, y + 1]) list.Add((x, y + 1));

        return list;
    }

    private void AddExtraConnections(FieldGrid field, float chance)
    {
        for (int x = 0; x < field.Width; x++)
        {
            for (int y = 0; y < field.Height; y++)
            {
                if (_random.NextDouble() > chance)
                {
                    continue;
                }

                var neighbors = GetDirectNeighbors(x, y, field);
                if (neighbors.Count == 0)
                {
                    continue;
                }

                var to = neighbors[_random.Next(neighbors.Count)];
                var a = field.GetCell(x, y);
                var b = field.GetCell(to.x, to.y);
                field.CreateLink(a, b);
            }
        }
    }

    private List<(int x, int y)> GetDirectNeighbors(int x, int y, FieldGrid field)
    {
        var list = new List<(int, int)>();

        if (x > 0) list.Add((x - 1, y));
        if (x < field.Width - 1) list.Add((x + 1, y));
        if (y > 0) list.Add((x, y - 1));
        if (y < field.Height - 1) list.Add((x, y + 1));

        return list;
    }

    private void EnsureFullConnectivity(FieldGrid field)
    {
        var start = field.GetCell(0, 0);

        var visited = new HashSet<Cell>();
        var queue = new Queue<Cell>();

        visited.Add(start);
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            foreach (var neighbor in current.Neighbors)
            {
                if (visited.Add(neighbor))
                {
                    queue.Enqueue(neighbor);
                }
            }
        }

        foreach (var cell in field.GetAllCells())
        {
            if (visited.Contains(cell))
            {
                continue;
            }

            var direct = GetDirectNeighbors(cell.X, cell.Y, field);
            foreach (var neighborCandidate in direct)
            {
                var neighbor = field.GetCell(neighborCandidate.x, neighborCandidate.y);

                if (visited.Contains(neighbor))
                {
                    field.CreateLink(cell, neighbor);
                    break;
                }
            }
        }
    }
}
