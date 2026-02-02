public class EquipmentSlot
{
    public ItemDefinition Definition { get; private set; }

    public bool IsEmpty => Definition == null;

    public void Set(ItemDefinition item)
    {
        Definition = item;
    }

    public void Clear()
    {
        Definition = null;
    }
}
