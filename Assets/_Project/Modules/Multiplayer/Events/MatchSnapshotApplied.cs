public readonly struct MatchSnapshotApplied
{
    public long Sequence { get; }
    public bool IsInitial { get; }

    public MatchSnapshotApplied(long sequence, bool isInitial)
    {
        Sequence = sequence;
        IsInitial = isInitial;
    }
}
