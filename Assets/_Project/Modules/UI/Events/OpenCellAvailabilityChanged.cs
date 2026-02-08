public readonly struct OpenCellAvailabilityChanged
{
    public bool IsAvailable { get; }

    public OpenCellAvailabilityChanged(bool isAvailable)
    {
        IsAvailable = isAvailable;
    }
}
