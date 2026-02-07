public readonly struct TurnPhaseChanged
{
    public ICellLayoutOccupant Actor { get; }
    public TurnState State { get; }

    public TurnPhaseChanged(ICellLayoutOccupant actor, TurnState state)
    {
        Actor = actor;
        State = state;
    }
}
