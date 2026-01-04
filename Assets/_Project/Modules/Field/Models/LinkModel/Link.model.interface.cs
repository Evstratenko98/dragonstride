public interface ILinkModel
{
    ICellModel A { get; }
    ICellModel B { get; }

    bool Connects(ICellModel x, ICellModel y);
}
