using System.Collections.Generic;

public readonly struct MatchActionBatch
{
    public long ActionSequence { get; }
    public long ResultSnapshotSequence { get; }
    public string SourcePlayerId { get; }
    public IReadOnlyList<ActionEventEnvelope> Events { get; }
    public bool BlocksTurnInput { get; }

    public MatchActionBatch(
        long actionSequence,
        long resultSnapshotSequence,
        string sourcePlayerId,
        IReadOnlyList<ActionEventEnvelope> events,
        bool blocksTurnInput)
    {
        ActionSequence = actionSequence;
        ResultSnapshotSequence = resultSnapshotSequence;
        SourcePlayerId = sourcePlayerId ?? string.Empty;
        Events = events;
        BlocksTurnInput = blocksTurnInput;
    }

    public MatchActionBatch WithResultSnapshotSequence(long sequence)
    {
        return new MatchActionBatch(
            ActionSequence,
            sequence,
            SourcePlayerId,
            Events,
            BlocksTurnInput);
    }
}
