using System.Collections.Generic;

public readonly struct CharacterInventorySnapshot
{
    public int ActorId { get; }
    public IReadOnlyList<InventorySlotSnapshot> Slots { get; }

    public CharacterInventorySnapshot(int actorId, IReadOnlyList<InventorySlotSnapshot> slots)
    {
        ActorId = actorId;
        Slots = slots;
    }
}
