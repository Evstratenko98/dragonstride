using System.Collections.Generic;

public sealed class InventorySnapshotService : IInventorySnapshotService
{
    private readonly IActorIdentityService _actorIdentityService;
    private readonly ItemConfig _itemConfig;
    private readonly Dictionary<string, ItemDefinition> _itemById = new();

    public InventorySnapshotService(IActorIdentityService actorIdentityService, ItemConfig itemConfig)
    {
        _actorIdentityService = actorIdentityService;
        _itemConfig = itemConfig;
        BuildItemLookup();
    }

    public CharacterInventorySnapshot Capture(int actorId)
    {
        if (_actorIdentityService == null ||
            !_actorIdentityService.TryGetActor(actorId, out ICellLayoutOccupant actor) ||
            actor is not CharacterInstance character ||
            character.Model?.Inventory == null)
        {
            return new CharacterInventorySnapshot(actorId, new List<InventorySlotSnapshot>());
        }

        var slots = new List<InventorySlotSnapshot>(character.Model.Inventory.Slots.Count);
        for (int i = 0; i < character.Model.Inventory.Slots.Count; i++)
        {
            InventorySlot slot = character.Model.Inventory.Slots[i];
            slots.Add(new InventorySlotSnapshot(
                i,
                slot?.Definition != null ? slot.Definition.Id : string.Empty,
                slot?.Count ?? 0));
        }

        return new CharacterInventorySnapshot(actorId, slots);
    }

    public void Apply(CharacterInventorySnapshot snapshot)
    {
        if (_actorIdentityService == null ||
            !_actorIdentityService.TryGetActor(snapshot.ActorId, out ICellLayoutOccupant actor) ||
            actor is not CharacterInstance character ||
            character.Model?.Inventory == null)
        {
            return;
        }

        IReadOnlyList<InventorySlot> liveSlots = character.Model.Inventory.Slots;
        for (int i = 0; i < liveSlots.Count; i++)
        {
            liveSlots[i].Clear();
        }

        if (snapshot.Slots == null)
        {
            return;
        }

        for (int i = 0; i < snapshot.Slots.Count; i++)
        {
            InventorySlotSnapshot slotSnapshot = snapshot.Slots[i];
            if (slotSnapshot.SlotIndex < 0 || slotSnapshot.SlotIndex >= liveSlots.Count)
            {
                continue;
            }

            InventorySlot liveSlot = liveSlots[slotSnapshot.SlotIndex];
            if (liveSlot == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(slotSnapshot.ItemId) || slotSnapshot.Count <= 0)
            {
                liveSlot.Clear();
                continue;
            }

            if (!_itemById.TryGetValue(slotSnapshot.ItemId, out ItemDefinition itemDefinition) || itemDefinition == null)
            {
                liveSlot.Clear();
                continue;
            }

            liveSlot.Set(itemDefinition, slotSnapshot.Count);
        }
    }

    private void BuildItemLookup()
    {
        _itemById.Clear();
        if (_itemConfig?.AllItems == null)
        {
            return;
        }

        for (int i = 0; i < _itemConfig.AllItems.Count; i++)
        {
            ItemDefinition definition = _itemConfig.AllItems[i];
            if (definition == null || string.IsNullOrWhiteSpace(definition.Id))
            {
                continue;
            }

            _itemById[definition.Id] = definition;
        }
    }
}
