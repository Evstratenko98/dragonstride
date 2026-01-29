public class Link
{
    public Cell A { get; }
    public Cell B { get; }

    public Link(Cell a, Cell b)
    {
        A = a;
        B = b;
    }

    public bool Connects(Cell x, Cell y)
    {
        return (x == A && y == B) || (x == B && y == A);
    }
}
