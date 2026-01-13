using System.Collections.Generic;

public class MazeGenerator
{
    private System.Random _rng = new System.Random();

    public void Generate(FieldService field, float extraConnectionChance)
    {
        int width = field.Width;
        int height = field.Height;

        // 1. Создаём идеальный лабиринт DFS
        GeneratePerfectMaze(field);

        // 2. Добавляем дополнительные короткие связи
        AddExtraConnections(field, extraConnectionChance);

        // 3. Гарантируем полную связность (BFS как в твоём старом коде)
        EnsureFullConnectivity(field);
    }

    // -------------------------------------------------------------------
    //                     1. ИДЕАЛЬНЫЙ DFS-ЛАБИРИНТ
    // -------------------------------------------------------------------
    private void GeneratePerfectMaze(FieldService field)
    {
        int width = field.Width;
        int height = field.Height;

        bool[,] visited = new bool[width, height];
        Stack<(int x, int y)> stack = new Stack<(int x, int y)>();

        // Начинаем с (0,0)
        stack.Push((0, 0));
        visited[0, 0] = true;

        while (stack.Count > 0)
        {
            var current = stack.Peek();

            List<(int x, int y)> unvisited = GetUnvisitedNeighbors(current.x, current.y, visited, field);

            if (unvisited.Count == 0)
            {
                stack.Pop();
                continue;
            }

            var next = unvisited[_rng.Next(unvisited.Count)];

            // Создаём модельную связь
            var a = field.GetCell(current.x, current.y);
            var b = field.GetCell(next.x, next.y);
            field.CreateLink(a, b);

            visited[next.x, next.y] = true;
            stack.Push(next);
        }
    }

    private List<(int x, int y)> GetUnvisitedNeighbors(int x, int y, bool[,] visited, FieldService field)
    {
        List<(int, int)> list = new List<(int, int)>();

        if (x > 0 && !visited[x - 1, y]) list.Add((x - 1, y));
        if (x < field.Width - 1 && !visited[x + 1, y]) list.Add((x + 1, y));
        if (y > 0 && !visited[x, y - 1]) list.Add((x, y - 1));
        if (y < field.Height - 1 && !visited[x, y + 1]) list.Add((x, y + 1));

        return list;
    }

    // -------------------------------------------------------------------
    //                2. ДОПОЛНИТЕЛЬНЫЕ КОРОТКИЕ СВЯЗИ
    // -------------------------------------------------------------------
    private void AddExtraConnections(FieldService field, float chance)
    {
        for (int x = 0; x < field.Width; x++)
        {
            for (int y = 0; y < field.Height; y++)
            {
                if (_rng.NextDouble() > chance)
                    continue;

                var neighbors = GetDirectNeighbors(x, y, field);

                if (neighbors.Count > 0)
                {
                    var to = neighbors[_rng.Next(neighbors.Count)];

                    var a = field.GetCell(x, y);
                    var b = field.GetCell(to.x, to.y);

                    // Проверка существования включена в FieldService
                    field.CreateLink(a, b);
                }
            }
        }
    }

    private List<(int x, int y)> GetDirectNeighbors(int x, int y, FieldService field)
    {
        List<(int, int)> list = new List<(int, int)>();

        if (x > 0) list.Add((x - 1, y));
        if (x < field.Width - 1) list.Add((x + 1, y));
        if (y > 0) list.Add((x, y - 1));
        if (y < field.Height - 1) list.Add((x, y + 1));

        return list;
    }

    // -------------------------------------------------------------------
    //                  3. ГАРАНТИЯ СВЯЗНОСТИ (BFS)
    // -------------------------------------------------------------------
    private void EnsureFullConnectivity(FieldService field)
    {
        var start = field.GetCell(0, 0);

        HashSet<CellModel> visited = new HashSet<CellModel>();
        Queue<CellModel> queue = new Queue<CellModel>();

        visited.Add(start);
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            foreach (var n in current.Neighbors)
            {
                if (!visited.Contains(n))
                {
                    visited.Add(n);
                    queue.Enqueue(n);
                }
            }
        }

        // Все непосещённые — подключаем
        foreach (var cell in field.GetAllCells())
        {
            if (visited.Contains(cell))
                continue;

            // ищем любого соседа
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
