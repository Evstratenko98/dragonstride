public class InventorySlot
{
    public ItemDefinition Definition { get; private set; }
    public int Count { get; private set; }

    public bool IsEmpty => Definition == null;

    public void Set(ItemDefinition item, int count)
    {
        Definition = item;
        Count = count;
    }

    public void Clear()
    {
        Definition = null;
        Count = 0;
    }

    public void Add(int value)
    {
        Count += value;
    }

    public void Remove(int value)
    {
        Count -= value;
        if (Count <= 0)
            Clear();
    }
}