using System.Collections.Generic;

public class MazeGenerator
{
    private readonly System.Random _random = new System.Random();

    public void Generate(FieldMap field, float extraConnectionChance)
    {
        GeneratePerfectMaze(field);
        AddExtraConnections(field, extraConnectionChance);
        EnsureFullConnectivity(field);
    }

    private void GeneratePerfectMaze(FieldMap field)
    {
        int width = field.Width;
        int height = field.Height;

        var visited = new bool[width, height];
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

    private List<(int x, int y)> GetUnvisitedNeighbors(int x, int y, bool[,] visited, FieldMap field)
    {
        var list = new List<(int, int)>();

        if (x > 0 && !visited[x - 1, y]) list.Add((x - 1, y));
        if (x < field.Width - 1 && !visited[x + 1, y]) list.Add((x + 1, y));
        if (y > 0 && !visited[x, y - 1]) list.Add((x, y - 1));
        if (y < field.Height - 1 && !visited[x, y + 1]) list.Add((x, y + 1));

        return list;
    }

    private void AddExtraConnections(FieldMap field, float chance)
    {
        for (int x = 0; x < field.Width; x++)
        {
            for (int y = 0; y < field.Height; y++)
            {
                if (_random.NextDouble() > chance)
                    continue;

                var neighbors = GetDirectNeighbors(x, y, field);

                if (neighbors.Count > 0)
                {
                    var to = neighbors[_random.Next(neighbors.Count)];

                    var a = field.GetCell(x, y);
                    var b = field.GetCell(to.x, to.y);

                    field.CreateLink(a, b);
                }
            }
        }
    }

    private List<(int x, int y)> GetDirectNeighbors(int x, int y, FieldMap field)
    {
        var list = new List<(int, int)>();

        if (x > 0) list.Add((x - 1, y));
        if (x < field.Width - 1) list.Add((x + 1, y));
        if (y > 0) list.Add((x, y - 1));
        if (y < field.Height - 1) list.Add((x, y + 1));

        return list;
    }

    private void EnsureFullConnectivity(FieldMap field)
    {
        var start = field.GetCell(0, 0);
        if (start == null)
            return;

        var visited = new HashSet<Cell>();
        var queue = new Queue<Cell>();

        visited.Add(start);
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            foreach (var neighbor in current.Neighbors)
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }

        foreach (var cell in field.GetAllCells())
        {
            if (visited.Contains(cell))
                continue;

            var direct = GetDirectNeighbors(cell.X, cell.Y, field);
            foreach (var dn in direct)
            {
                var neighbor = field.GetCell(dn.x, dn.y);

                if (visited.Contains(neighbor))
                {
                    field.CreateLink(cell, neighbor);
                    break;
                }
            }
        }
    }
}
