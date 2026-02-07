public class Entity
{
    public event System.Action StatsChanged;

    public string Name { get; private set; }
    public Cell CurrentCell { get; private set; }

    public int Health { get; private set; } = 100;
    public int Attack { get; private set; } = 50;
    public int Armor { get; private set; } = 5;

    public float DodgeChance { get; private set; } = 0.10f;
    public int Initiative { get; private set; } = 0;
    public int Speed { get; private set; } = 0;
    public float Luck { get; private set; } = 0.10f;

    public virtual int CalculateTurnSteps(int diceRoll)
    {
        return System.Math.Max(0, diceRoll + Speed);
    }

    public void SetCell(Cell cell)
    {
        CurrentCell = cell;
    }

    public void SetName(string name)
    {
        Name = name;
    }

    public void AddHealth(int value)
    {
        Health += value;
        StatsChanged?.Invoke();
    }

    public void AddAttack(int value)
    {
        Attack += value;
        StatsChanged?.Invoke();
    }

    public void AddArmor(int value)
    {
        Armor += value;
        StatsChanged?.Invoke();
    }

    public void AddDodge(float value)
    {
        DodgeChance += value;
        StatsChanged?.Invoke();
    }

    public void AddInitiative(int value)
    {
        Initiative += value;
        StatsChanged?.Invoke();
    }

    public void AddSpeed(int value)
    {
        Speed += value;
        StatsChanged?.Invoke();
    }

    public void AddLuck(float value)
    {
        Luck += value;
        StatsChanged?.Invoke();
    }

    public void SetHealth(int value)
    {
        Health = value;
        StatsChanged?.Invoke();
    }

    public void SetAttack(int value)
    {
        Attack = value;
        StatsChanged?.Invoke();
    }

    public void SetArmor(int value)
    {
        Armor = value;
        StatsChanged?.Invoke();
    }

    public void SetDodge(float value)
    {
        DodgeChance = value;
        StatsChanged?.Invoke();
    }

    public void SetInitiative(int value)
    {
        Initiative = value;
        StatsChanged?.Invoke();
    }

    public void SetSpeed(int value)
    {
        Speed = value;
        StatsChanged?.Invoke();
    }

    public void SetLuck(float value)
    {
        Luck = value;
        StatsChanged?.Invoke();
    }
}
