using System;
using System.Collections.Generic;
using System.Text;

public sealed class FieldSnapshotService : IFieldSnapshotService
{
    public FieldGridSnapshot Capture(FieldGrid grid)
    {
        if (grid == null)
        {
            return default;
        }

        var cells = new List<FieldCellSnapshot>();
        foreach (Cell cell in grid.GetAllCells())
        {
            if (cell == null)
            {
                continue;
            }

            cells.Add(new FieldCellSnapshot(
                cell.X,
                cell.Y,
                cell.Type,
                cell.IsOpened,
                cell.IsTypeRevealed));
        }

        var links = new List<FieldLinkSnapshot>();
        foreach (Link link in grid.GetAllLinks())
        {
            if (link?.A == null || link.B == null)
            {
                continue;
            }

            links.Add(new FieldLinkSnapshot(
                link.A.X,
                link.A.Y,
                link.B.X,
                link.B.Y));
        }

        Cell startCell = ResolveStartCell(grid);
        int startX = startCell?.X ?? 0;
        int startY = startCell?.Y ?? 0;
        string checksum = ComputeChecksum(grid.Width, grid.Height, cells, links, startX, startY);

        return new FieldGridSnapshot(
            grid.Width,
            grid.Height,
            cells,
            links,
            startX,
            startY,
            checksum);
    }

    public FieldGrid Build(FieldGridSnapshot snapshot)
    {
        if (snapshot.Width <= 0 || snapshot.Height <= 0)
        {
            return null;
        }

        var grid = new FieldGrid(snapshot.Width, snapshot.Height);

        if (snapshot.Cells != null)
        {
            for (int i = 0; i < snapshot.Cells.Count; i++)
            {
                FieldCellSnapshot cellSnapshot = snapshot.Cells[i];
                Cell cell = grid.GetCell(cellSnapshot.X, cellSnapshot.Y);
                if (cell == null)
                {
                    continue;
                }

                cell.SetType(cellSnapshot.Type);
                if (cellSnapshot.IsTypeRevealed)
                {
                    cell.RevealType();
                }

                if (cellSnapshot.IsOpened)
                {
                    cell.MarkOpened();
                }
            }
        }

        if (snapshot.Links != null)
        {
            for (int i = 0; i < snapshot.Links.Count; i++)
            {
                FieldLinkSnapshot link = snapshot.Links[i];
                Cell a = grid.GetCell(link.AX, link.AY);
                Cell b = grid.GetCell(link.BX, link.BY);
                if (a == null || b == null)
                {
                    continue;
                }

                grid.CreateLink(a, b);
            }
        }

        Cell startCell = grid.GetCell(snapshot.StartX, snapshot.StartY);
        if (startCell != null && startCell.Type != CellType.Start)
        {
            startCell.SetType(CellType.Start);
            startCell.RevealType();
            startCell.MarkOpened();
        }

        return grid;
    }

    private static Cell ResolveStartCell(FieldGrid grid)
    {
        foreach (Cell cell in grid.GetCellsByType(CellType.Start))
        {
            if (cell != null)
            {
                return cell;
            }
        }

        return grid.GetCell(grid.Width / 2, grid.Height / 2);
    }

    private static string ComputeChecksum(
        int width,
        int height,
        IReadOnlyList<FieldCellSnapshot> cells,
        IReadOnlyList<FieldLinkSnapshot> links,
        int startX,
        int startY)
    {
        var builder = new StringBuilder(4096);
        builder.Append(width).Append('|').Append(height).Append('|').Append(startX).Append('|').Append(startY).Append(';');

        if (cells != null)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                FieldCellSnapshot cell = cells[i];
                builder.Append(cell.X).Append(',').Append(cell.Y).Append(',')
                    .Append((int)cell.Type).Append(',')
                    .Append(cell.IsOpened ? '1' : '0').Append(',')
                    .Append(cell.IsTypeRevealed ? '1' : '0').Append(';');
            }
        }

        builder.Append('#');
        if (links != null)
        {
            for (int i = 0; i < links.Count; i++)
            {
                FieldLinkSnapshot link = links[i];
                int ax = link.AX;
                int ay = link.AY;
                int bx = link.BX;
                int by = link.BY;
                if (ax > bx || (ax == bx && ay > by))
                {
                    Swap(ref ax, ref bx);
                    Swap(ref ay, ref by);
                }

                builder.Append(ax).Append(',').Append(ay).Append(',')
                    .Append(bx).Append(',').Append(by).Append(';');
            }
        }

        unchecked
        {
            uint hash = 2166136261;
            string source = builder.ToString();
            for (int i = 0; i < source.Length; i++)
            {
                hash ^= source[i];
                hash *= 16777619;
            }

            return hash.ToString("X8");
        }
    }

    private static void Swap(ref int a, ref int b)
    {
        int temp = a;
        a = b;
        b = temp;
    }
}
