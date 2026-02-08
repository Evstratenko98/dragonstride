using UnityEngine;
using System.Collections.Generic;

public class Entity
{
    public event System.Action StatsChanged;
    public event System.Action StatesChanged;

    private readonly List<State> _states = new();

    public string Name { get; private set; }
    public Cell CurrentCell { get; private set; }
    public IReadOnlyList<State> States => _states;

    public int Health { get; private set; } = 100;
    public int MaxHealth { get; private set; } = 100;
    public int Attack { get; private set; } = 50;
    public int Armor { get; private set; } = 5;
    public int Level { get; private set; } = 1;

    public float DodgeChance { get; private set; } = 0.10f;
    public int Initiative { get; private set; } = 0;
    public int Speed { get; private set; } = 0;
    public float Luck { get; private set; } = 0.10f;
    public virtual Color HealthBarFillColor => Color.green;

    public virtual int CalculateTurnSteps(int diceRoll)
    {
        return System.Math.Max(0, diceRoll + Speed);
    }

    public void AddState(State state)
    {
        if (state == null || _states.Contains(state))
        {
            return;
        }

        _states.Add(state);
        state.ApplyTo(this);
        StatesChanged?.Invoke();
    }

    public bool RemoveState(State state)
    {
        if (state == null || !_states.Remove(state))
        {
            return false;
        }

        state.RemoveFrom(this);
        StatesChanged?.Invoke();
        return true;
    }

    public void TickStates()
    {
        for (int i = _states.Count - 1; i >= 0; i--)
        {
            State state = _states[i];
            if (state.TickRound(this))
            {
                _states.RemoveAt(i);
                state.RemoveFrom(this);
                StatesChanged?.Invoke();
            }
        }
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
        MaxHealth = Mathf.Max(1, MaxHealth + value);
        Health = Mathf.Clamp(Health + value, 0, MaxHealth);
        StatsChanged?.Invoke();
    }

    public int RestoreHealth(int value)
    {
        if (value <= 0)
        {
            return 0;
        }

        int missingHealth = MaxHealth - Health;
        if (missingHealth <= 0)
        {
            return 0;
        }

        int restored = Mathf.Min(value, missingHealth);
        Health += restored;
        StatsChanged?.Invoke();
        return restored;
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
        Health = Mathf.Clamp(value, 0, MaxHealth);
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

    public virtual void SetLevel(int value)
    {
        Level = Mathf.Max(1, value);
        StatsChanged?.Invoke();
    }
}
