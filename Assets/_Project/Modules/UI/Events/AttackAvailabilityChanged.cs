public readonly struct AttackAvailabilityChanged
{
    public bool IsAvailable { get; }

    public AttackAvailabilityChanged(bool isAvailable)
    {
        IsAvailable = isAvailable;
    }
}
