public readonly struct FieldLinkSnapshot
{
    public int AX { get; }
    public int AY { get; }
    public int BX { get; }
    public int BY { get; }

    public FieldLinkSnapshot(int ax, int ay, int bx, int by)
    {
        AX = ax;
        AY = ay;
        BX = bx;
        BY = by;
    }
}
