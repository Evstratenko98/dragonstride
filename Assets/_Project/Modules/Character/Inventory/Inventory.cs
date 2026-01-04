using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
        // 1. Если предмет стакается — ищем подходящий слот
        if (item.Stackable)
        {
            var stackSlot = _slots.FirstOrDefault(s => s.Definition == item);
            if (stackSlot != null)
            {
                stackSlot.Add(count);
                return true;
            }
        }

        // 2. Ищем пустой слот
        var emptySlot = _slots.FirstOrDefault(s => s.IsEmpty);
        if (emptySlot != null)
        {
            emptySlot.Set(item, count);
            return true;
        }

        Debug.Log("Inventory full!");
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
    
    // public bool HasFreeSlot() =>
    //     _slots.Any(slot => slot.IsEmpty);
    //
    // public int CountOf(ItemDefinition item)
    // {
    //     var slot = _slots.FirstOrDefault(s => s.Definition == item);
    //     return slot?.Count ?? 0;
    // }
}