public interface IMatchStateApplier
{
    bool HasReceivedInitialSnapshot { get; }
    long LastAppliedSequence { get; }

    bool TryApply(MatchStateSnapshot snapshot);
}
