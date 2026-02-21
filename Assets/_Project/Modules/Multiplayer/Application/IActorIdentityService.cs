using System.Collections.Generic;

public interface IActorIdentityService
{
    int GetOrAssign(ICellLayoutOccupant actor);
    int GetId(ICellLayoutOccupant actor);
    bool TryGetActor(int actorId, out ICellLayoutOccupant actor);
    bool TryBind(ICellLayoutOccupant actor, int actorId);
    IReadOnlyList<int> GetActorIds();
    void Remove(ICellLayoutOccupant actor);
    void Clear();
}
