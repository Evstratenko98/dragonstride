public class Character : Entity
{
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
}
