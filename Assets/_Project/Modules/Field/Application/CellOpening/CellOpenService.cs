using System.Collections.Generic;

public sealed class CellOpenService
{
    private readonly FieldPresenter _fieldPresenter;
    private readonly Dictionary<CellType, ICellOpenHandler> _handlers;

    public CellOpenService(FieldPresenter fieldPresenter, IEnumerable<ICellOpenHandler> handlers)
    {
        _fieldPresenter = fieldPresenter;
        _handlers = new Dictionary<CellType, ICellOpenHandler>();

        foreach (var handler in handlers)
        {
            if (handler != null)
            {
                _handlers[handler.CellType] = handler;
            }
        }
    }

    public bool TryOpen(ICellLayoutOccupant actor)
    {
        var entity = actor?.Entity;
        var cell = entity?.CurrentCell;
        if (cell == null)
        {
            return false;
        }

        if (cell.IsOpened)
        {
            return false;
        }

        if (!_handlers.TryGetValue(cell.Type, out var handler))
        {
            return false;
        }

        bool opened = handler.TryOpen(actor, cell);
        if (!opened)
        {
            return false;
        }

        cell.MarkOpened();
        _fieldPresenter.RefreshCell(cell);
        return true;
    }
}
