public readonly struct DiceRolled
{
    public ICellLayoutOccupant Actor { get; }
    public int Steps { get; }

    public DiceRolled(ICellLayoutOccupant actor, int steps)
    {
        Actor = actor;
        Steps = steps;
    }
}
