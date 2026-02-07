using System.Collections.Generic;

public class TurnActorRegistry
{
    private readonly List<ICellLayoutOccupant> _actors = new();

    public IReadOnlyList<ICellLayoutOccupant> Actors => _actors;

    public void Register(ICellLayoutOccupant actor)
    {
        if (actor == null || actor.Entity == null || _actors.Contains(actor))
        {
            return;
        }

        _actors.Add(actor);
    }

    public void Unregister(ICellLayoutOccupant actor)
    {
        if (actor == null)
        {
            return;
        }

        _actors.Remove(actor);
    }

    public List<ICellLayoutOccupant> GetActiveActorsSnapshot()
    {
        _actors.RemoveAll(actor => actor?.Entity?.CurrentCell == null);
        return new List<ICellLayoutOccupant>(_actors);
    }

    public void Clear()
    {
        _actors.Clear();
    }
}
