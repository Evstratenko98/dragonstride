public class LinkModel
{
    public CellModel A { get; }
    public CellModel B { get; }

    public LinkModel(CellModel a, CellModel b)
    {
        A = a;
        B = b;
    }

    public bool Connects(CellModel x, CellModel y)
    {
        return (x == A && y == B) || (x == B && y == A);
    }
}