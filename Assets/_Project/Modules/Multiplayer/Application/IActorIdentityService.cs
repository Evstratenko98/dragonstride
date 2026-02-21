public interface IActorIdentityService
{
    int GetOrAssign(ICellLayoutOccupant actor);
    int GetId(ICellLayoutOccupant actor);
    bool TryGetActor(int actorId, out ICellLayoutOccupant actor);
    void Remove(ICellLayoutOccupant actor);
    void Clear();
}
