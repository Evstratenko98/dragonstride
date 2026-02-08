public class Character : Entity
{
    private const string HealthFlaskSmallId = "health_flask_small";
    private const int MinLevel = 1;
    private const int MaxLevel = 10;
    private const int LevelStatBonus = 10;

    public CharacterClass Class { get; }
    public Inventory Inventory { get; private set; }
    public CharacterEquipment Equipment { get; private set; }

    public Character(CharacterClass characterClass)
    {
        Class = characterClass;
    }

    public void InitializeInventory(int capacity)
    {
        Inventory = new Inventory(capacity);
    }

    public void InitializeEquipment(int capacity = 2)
    {
        Equipment = new CharacterEquipment(this, capacity);
    }

    public bool TryUseConsumable(int inventorySlotIndex)
    {
        if (Inventory == null || inventorySlotIndex < 0 || inventorySlotIndex >= Inventory.Slots.Count)
        {
            return false;
        }

        InventorySlot slot = Inventory.Slots[inventorySlotIndex];
        if (slot == null || slot.IsEmpty || slot.Definition == null || slot.Definition.Type != ItemType.Consumable)
        {
            return false;
        }

        bool effectApplied = ApplyConsumableEffect(slot.Definition);
        if (!effectApplied)
        {
            return false;
        }

        return Inventory.RemoveOneAt(inventorySlotIndex);
    }

    public bool TryLevelUp()
    {
        if (Level >= MaxLevel)
        {
            return false;
        }

        SetLevel(Level + 1);
        return true;
    }

    public override void SetLevel(int value)
    {
        int currentLevel = Level;
        int clampedLevel = UnityEngine.Mathf.Clamp(value, MinLevel, MaxLevel);
        if (clampedLevel == currentLevel)
        {
            return;
        }

        int levelsGained = UnityEngine.Mathf.Max(0, clampedLevel - currentLevel);
        if (levelsGained > 0)
        {
            int totalBonus = levelsGained * LevelStatBonus;
            AddAttack(totalBonus);
            AddArmor(totalBonus);
        }

        base.SetLevel(clampedLevel);
    }

    private bool ApplyConsumableEffect(ItemDefinition item)
    {
        if (item == null)
        {
            return false;
        }

        // Явные ветки по ID позволяют гибко добавлять уникальные эффекты под каждый consumable.
        if (item.Id == HealthFlaskSmallId)
        {
            int restored = RestoreHealth(item.HealAmount);
            return restored > 0;
        }

        if (item.HealAmount > 0)
        {
            int restored = RestoreHealth(item.HealAmount);
            return restored > 0;
        }

        return false;
    }
}
