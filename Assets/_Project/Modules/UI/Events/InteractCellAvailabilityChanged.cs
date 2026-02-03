public readonly struct InteractCellAvailabilityChanged
{
    public bool IsAvailable { get; }

    public InteractCellAvailabilityChanged(bool isAvailable)
    {
        IsAvailable = isAvailable;
    }
}
