using System.Collections.Generic;
using System.Linq;

public class Inventory
{
    private readonly List<InventorySlot> _slots;

    public int Capacity { get; private set; }

    public IReadOnlyList<InventorySlot> Slots => _slots;

    public Inventory(int capacity)
    {
        Capacity = capacity;
        _slots = new List<InventorySlot>(capacity);

        for (int i = 0; i < capacity; i++)
            _slots.Add(new InventorySlot());
    }
    
    public bool AddItem(ItemDefinition item, int count = 1)
    {
        if (item.Stackable)
        {
            var stackSlot = _slots.FirstOrDefault(s => s.Definition == item);
            if (stackSlot != null)
            {
                stackSlot.Add(count);
                return true;
            }
        }

        var emptySlot = _slots.FirstOrDefault(s => s.IsEmpty);
        if (emptySlot != null)
        {
            emptySlot.Set(item, count);
            return true;
        }

        return false;
    }

    public bool RemoveOne(ItemDefinition item)
    {
        var slot = _slots.FirstOrDefault(s => s.Definition == item);
        if (slot == null) return false;

        slot.Remove(1);
        return true;
    }
    
    public bool RemoveAll(ItemDefinition item)
    {
        var slot = _slots.FirstOrDefault(s => s.Definition == item);
        if (slot == null) return false;

        slot.Clear();
        return true;
    }

    public void SwapSlots(int fromIndex, int toIndex)
    {
        if (fromIndex == toIndex)
        {
            return;
        }

        if (fromIndex < 0 || fromIndex >= _slots.Count || toIndex < 0 || toIndex >= _slots.Count)
        {
            return;
        }

        var fromSlot = _slots[fromIndex];
        var toSlot = _slots[toIndex];

        var fromItem = fromSlot.Definition;
        var fromCount = fromSlot.Count;

        if (toSlot.IsEmpty)
        {
            fromSlot.Clear();
        }
        else
        {
            fromSlot.Set(toSlot.Definition, toSlot.Count);
        }

        if (fromItem == null)
        {
            toSlot.Clear();
        }
        else
        {
            toSlot.Set(fromItem, fromCount);
        }
    }
}
