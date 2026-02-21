using System.Collections.Generic;

public readonly struct FieldGridSnapshot
{
    public int Width { get; }
    public int Height { get; }
    public IReadOnlyList<FieldCellSnapshot> Cells { get; }
    public IReadOnlyList<FieldLinkSnapshot> Links { get; }
    public int StartX { get; }
    public int StartY { get; }
    public string Checksum { get; }

    public FieldGridSnapshot(
        int width,
        int height,
        IReadOnlyList<FieldCellSnapshot> cells,
        IReadOnlyList<FieldLinkSnapshot> links,
        int startX,
        int startY,
        string checksum)
    {
        Width = width;
        Height = height;
        Cells = cells;
        Links = links;
        StartX = startX;
        StartY = startY;
        Checksum = checksum;
    }
}
