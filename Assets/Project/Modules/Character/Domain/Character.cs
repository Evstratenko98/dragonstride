public class Character
{
    public Inventory Inventory { get; private set; }
    public CharacterEquipment Equipment { get; private set; }
    public Cell CurrentCell { get; private set; }

    public void SetCell(Cell cell)
    {
        CurrentCell = cell;
    }

    public int Health { get; private set; } = 100;
    public int Attack { get; private set; } = 10;
    public int Armor { get; private set; } = 5;

    public float DodgeChance { get; private set; } = 0.10f;
    public int Initiative { get; private set; } = 0;

    public int Speed { get; private set; } = 0;
    public float Luck { get; private set; } = 0.10f;

    public void AddHealth(int value) => Health += value;
    public void AddAttack(int value) => Attack += value;
    public void AddArmor(int value) => Armor += value;

    public void AddDodge(float value) => DodgeChance += value;
    public void AddInitiative(int value) => Initiative += value;

    public void AddSpeed(int value) => Speed += value;
    public void AddLuck(float value) => Luck += value;

    public void SetHealth(int value) => Health = value;
    public void SetAttack(int value) => Attack = value;
    public void SetArmor(int value) => Armor = value;

    public void SetDodge(float value) => DodgeChance = value;
    public void SetInitiative(int value) => Initiative = value;

    public void SetSpeed(int value) => Speed = value;
    public void SetLuck(float value) => Luck = value;
    
    public void InitializeInventory(int capacity)
    {
        Inventory = new Inventory(capacity);
    }

    public void InitializeEquipment(int capacity = 2)
    {
        Equipment = new CharacterEquipment(this, capacity);
    }
}
