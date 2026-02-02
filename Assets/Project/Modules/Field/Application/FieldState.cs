using System.Linq;

public sealed class FieldState
{
    public FieldGrid CurrentField { get; private set; }

    public Cell StartCell => CurrentField?.GetCellsByType(CellType.Start).FirstOrDefault();

    public void SetField(FieldGrid field)
    {
        CurrentField = field;
    }

    public void Clear()
    {
        CurrentField = null;
    }
}
