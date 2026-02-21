public interface IInventorySnapshotService
{
    CharacterInventorySnapshot Capture(int actorId);
    void Apply(CharacterInventorySnapshot snapshot);
}
