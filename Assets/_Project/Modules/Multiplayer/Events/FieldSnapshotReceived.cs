public readonly struct FieldSnapshotReceived
{
    public FieldGridSnapshot Snapshot { get; }

    public FieldSnapshotReceived(FieldGridSnapshot snapshot)
    {
        Snapshot = snapshot;
    }
}
