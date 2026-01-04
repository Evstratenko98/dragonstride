public class CharacterModel
{
    public Inventory Inventory { get; private set; }
    // -----------------------------------
    //       ПОЛОЖЕНИЕ НА КАРТЕ
    // -----------------------------------
    public ICellModel CurrentCell { get; private set; }

    public void SetCell(ICellModel cell)
    {
        CurrentCell = cell;
    }

    // -----------------------------------
    //       ХАРАКТЕРИСТИКИ ПЕРСОНАЖА
    // -----------------------------------

    // БАЗОВЫЕ СТАТЫ
    public int Health { get; private set; } = 100;
    public int Attack { get; private set; } = 10;
    public int Armor { get; private set; } = 5;

    // ПРОЦЕНТНЫЕ СТАТЫ (0–100)
    public float DodgeChance { get; private set; } = 0.10f; // 10%
    public int Initiative { get; private set; } = 0;

    // ПАРАМЕТРЫ ДВИЖЕНИЯ / РИСКА
    public int Speed { get; private set; } = 0;
    public float Luck { get; private set; } = 0.10f; // 10%

    // -----------------------------------
    //             МОДИФИКАТОРЫ
    // -----------------------------------

    public void AddHealth(int value) => Health += value;
    public void AddAttack(int value) => Attack += value;
    public void AddArmor(int value) => Armor += value;

    public void AddDodge(float value) => DodgeChance += value;
    public void AddInitiative(int value) => Initiative += value;

    public void AddSpeed(int value) => Speed += value;
    public void AddLuck(float value) => Luck += value;

    // Сеттеры для прямого изменения (если нужно)
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
}
