using System.Linq;
using Project.Modules.Field.Domain;

namespace Project.Modules.Field.Application;

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
