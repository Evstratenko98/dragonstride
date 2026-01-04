public class LinkModel : ILinkModel
{
    public ICellModel A { get; }
    public ICellModel B { get; }

    public LinkModel(ICellModel a, ICellModel b)
    {
        A = a;
        B = b;
    }

    public bool Connects(ICellModel x, ICellModel y)
    {
        return (x == A && y == B) || (x == B && y == A);
    }
}