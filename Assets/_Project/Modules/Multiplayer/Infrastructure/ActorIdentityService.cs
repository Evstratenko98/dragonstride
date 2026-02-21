using System.Collections.Generic;

public sealed class ActorIdentityService : IActorIdentityService
{
    private readonly Dictionary<int, ICellLayoutOccupant> _actorsById = new();
    private readonly Dictionary<ICellLayoutOccupant, int> _idsByActor = new();
    private int _nextId = 1;

    public int GetOrAssign(ICellLayoutOccupant actor)
    {
        if (actor == null)
        {
            return 0;
        }

        if (_idsByActor.TryGetValue(actor, out int existingId))
        {
            return existingId;
        }

        int id = _nextId++;
        _idsByActor[actor] = id;
        _actorsById[id] = actor;
        return id;
    }

    public int GetId(ICellLayoutOccupant actor)
    {
        if (actor == null)
        {
            return 0;
        }

        return _idsByActor.TryGetValue(actor, out int id) ? id : 0;
    }

    public bool TryGetActor(int actorId, out ICellLayoutOccupant actor)
    {
        return _actorsById.TryGetValue(actorId, out actor);
    }

    public bool TryBind(ICellLayoutOccupant actor, int actorId)
    {
        if (actor == null || actorId <= 0)
        {
            return false;
        }

        if (_idsByActor.TryGetValue(actor, out int existingId))
        {
            if (existingId == actorId)
            {
                return true;
            }

            if (_actorsById.TryGetValue(actorId, out ICellLayoutOccupant actorForRequestedId) &&
                !ReferenceEquals(actorForRequestedId, actor))
            {
                return false;
            }

            _actorsById.Remove(existingId);
            _idsByActor[actor] = actorId;
            _actorsById[actorId] = actor;
            if (actorId >= _nextId)
            {
                _nextId = actorId + 1;
            }

            return true;
        }

        if (_actorsById.TryGetValue(actorId, out ICellLayoutOccupant existingActor))
        {
            return ReferenceEquals(existingActor, actor);
        }

        _idsByActor[actor] = actorId;
        _actorsById[actorId] = actor;
        if (actorId >= _nextId)
        {
            _nextId = actorId + 1;
        }

        return true;
    }

    public IReadOnlyList<int> GetActorIds()
    {
        return new List<int>(_actorsById.Keys);
    }

    public void Remove(ICellLayoutOccupant actor)
    {
        if (actor == null || !_idsByActor.TryGetValue(actor, out int id))
        {
            return;
        }

        _idsByActor.Remove(actor);
        _actorsById.Remove(id);
    }

    public void Clear()
    {
        _actorsById.Clear();
        _idsByActor.Clear();
        _nextId = 1;
    }
}
