using System.Collections.Generic;

public class CharacterEquipment
{
    private readonly List<EquipmentSlot> _slots;
    private readonly Character _model;

    public int Capacity { get; }
    public IReadOnlyList<EquipmentSlot> Slots => _slots;

    public CharacterEquipment(Character model, int capacity = 2)
    {
        _model = model;
        Capacity = capacity;
        _slots = new List<EquipmentSlot>(capacity);

        for (int i = 0; i < capacity; i++)
            _slots.Add(new EquipmentSlot());
    }

    public bool EquipFromInventory(Inventory inventory, int inventorySlotIndex, int equipmentSlotIndex)
    {
        if (!IsValidInventorySlot(inventory, inventorySlotIndex) || !IsValidEquipmentSlot(equipmentSlotIndex))
        {
            return false;
        }

        var inventorySlot = inventory.Slots[inventorySlotIndex];
        if (inventorySlot.IsEmpty)
        {
            return false;
        }

        var item = inventorySlot.Definition;
        if (item.Type != ItemType.Weapon)
        {
            return false;
        }

        var equipmentSlot = _slots[equipmentSlotIndex];
        if (!equipmentSlot.IsEmpty)
        {
            return false;
        }

        if (!inventory.RemoveOne(item))
        {
            return false;
        }

        equipmentSlot.Set(item);
        ApplyItemModifiers(item, 1);
        return true;
    }

    public bool UnequipToInventory(Inventory inventory, int equipmentSlotIndex)
    {
        if (!IsValidEquipmentSlot(equipmentSlotIndex))
        {
            return false;
        }

        var equipmentSlot = _slots[equipmentSlotIndex];
        if (equipmentSlot.IsEmpty)
        {
            return false;
        }

        var item = equipmentSlot.Definition;
        if (!inventory.AddItem(item))
        {
            return false;
        }

        equipmentSlot.Clear();
        ApplyItemModifiers(item, -1);
        return true;
    }

    public bool EquipFirstFreeSlot(Inventory inventory, int inventorySlotIndex)
    {
        var freeSlotIndex = _slots.FindIndex(slot => slot.IsEmpty);
        if (freeSlotIndex < 0)
        {
            return false;
        }

        return EquipFromInventory(inventory, inventorySlotIndex, freeSlotIndex);
    }

    public bool SwapSlots(int fromIndex, int toIndex)
    {
        if (!IsValidEquipmentSlot(fromIndex) || !IsValidEquipmentSlot(toIndex))
        {
            return false;
        }

        if (fromIndex == toIndex)
        {
            return true;
        }

        var fromSlot = _slots[fromIndex];
        var toSlot = _slots[toIndex];
        var fromItem = fromSlot.Definition;
        var toItem = toSlot.Definition;

        fromSlot.Set(toItem);
        toSlot.Set(fromItem);
        return true;
    }

    public bool UnequipToInventorySlot(Inventory inventory, int equipmentSlotIndex, int inventorySlotIndex)
    {
        if (!IsValidEquipmentSlot(equipmentSlotIndex))
        {
            return false;
        }

        if (!IsValidInventorySlot(inventory, inventorySlotIndex))
        {
            return false;
        }

        var equipmentSlot = _slots[equipmentSlotIndex];
        if (equipmentSlot.IsEmpty)
        {
            return false;
        }

        var inventorySlot = inventory.Slots[inventorySlotIndex];
        var item = equipmentSlot.Definition;

        if (!inventorySlot.IsEmpty && inventorySlot.Definition != item)
        {
            return false;
        }

        if (inventorySlot.IsEmpty)
        {
            inventorySlot.Set(item, 1);
        }
        else
        {
            inventorySlot.Add(1);
        }

        equipmentSlot.Clear();
        ApplyItemModifiers(item, -1);
        return true;
    }

    private bool IsValidInventorySlot(Inventory inventory, int index)
    {
        return inventory != null && index >= 0 && index < inventory.Slots.Count;
    }

    private bool IsValidEquipmentSlot(int index)
    {
        return index >= 0 && index < _slots.Count;
    }

    private void ApplyItemModifiers(ItemDefinition item, int sign)
    {
        if (_model == null || item == null)
        {
            return;
        }

        _model.AddHealth(item.HealthModifier * sign);
        _model.AddAttack(item.AttackModifier * sign);
        _model.AddArmor(item.ArmorModifier * sign);
        _model.AddDodge(item.DodgeModifier * sign);
        _model.AddInitiative(item.InitiativeModifier * sign);
        _model.AddSpeed(item.SpeedModifier * sign);
        _model.AddLuck(item.LuckModifier * sign);
    }
}
