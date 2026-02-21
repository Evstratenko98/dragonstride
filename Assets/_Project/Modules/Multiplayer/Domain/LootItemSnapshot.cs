public readonly struct LootItemSnapshot
{
    public string ItemId { get; }
    public int Count { get; }

    public LootItemSnapshot(string itemId, int count)
    {
        ItemId = itemId ?? string.Empty;
        Count = count < 1 ? 1 : count;
    }
}
