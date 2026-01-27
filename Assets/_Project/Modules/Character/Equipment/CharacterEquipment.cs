using System.Collections.Generic;
using UnityEngine;

public class CharacterEquipment
{
    private readonly List<EquipmentSlot> _slots;
    private readonly CharacterModel _model;

    public int Capacity { get; }
    public IReadOnlyList<EquipmentSlot> Slots => _slots;

    public CharacterEquipment(CharacterModel model, int capacity = 2)
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
            Debug.LogWarning($"[CharacterEquipment] Invalid equip attempt inv={inventorySlotIndex} slot={equipmentSlotIndex}.");
            return false;
        }

        var inventorySlot = inventory.Slots[inventorySlotIndex];
        if (inventorySlot.IsEmpty)
        {
            Debug.LogWarning("[CharacterEquipment] Inventory slot is empty.");
            return false;
        }

        var item = inventorySlot.Definition;
        if (item.Type != ItemType.Weapon)
        {
            Debug.LogWarning("[CharacterEquipment] Only weapons can be equipped.");
            return false;
        }

        var equipmentSlot = _slots[equipmentSlotIndex];
        if (!equipmentSlot.IsEmpty)
        {
            Debug.LogWarning("[CharacterEquipment] Equipment slot is already occupied.");
            return false;
        }

        if (!inventory.RemoveOne(item))
        {
            Debug.LogWarning("[CharacterEquipment] Failed to remove item from inventory.");
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
            Debug.LogWarning($"[CharacterEquipment] Invalid unequip slot={equipmentSlotIndex}.");
            return false;
        }

        var equipmentSlot = _slots[equipmentSlotIndex];
        if (equipmentSlot.IsEmpty)
        {
            Debug.LogWarning("[CharacterEquipment] Equipment slot is empty.");
            return false;
        }

        var item = equipmentSlot.Definition;
        if (!inventory.AddItem(item))
        {
            Debug.LogWarning("[CharacterEquipment] Failed to add item back to inventory.");
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
            Debug.LogWarning("[CharacterEquipment] No free equipment slots.");
            return false;
        }

        return EquipFromInventory(inventory, inventorySlotIndex, freeSlotIndex);
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
