public readonly struct InventorySlotSnapshot
{
    public int SlotIndex { get; }
    public string ItemId { get; }
    public int Count { get; }

    public InventorySlotSnapshot(int slotIndex, string itemId, int count)
    {
        SlotIndex = slotIndex;
        ItemId = itemId ?? string.Empty;
        Count = count < 0 ? 0 : count;
    }
}
