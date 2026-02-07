public readonly struct CharacterMoved
{
    public ICellLayoutOccupant Actor { get; }

    public CharacterMoved(ICellLayoutOccupant actor)
    {
        Actor = actor;
    }
}
