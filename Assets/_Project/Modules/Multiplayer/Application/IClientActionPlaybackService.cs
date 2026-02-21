using System.Threading;
using System.Threading.Tasks;

public interface IClientActionPlaybackService
{
    bool IsBusy { get; }
    long LastAppliedActionSequence { get; }

    Task ApplyBatchAsync(MatchActionBatch batch, CancellationToken ct = default);
}
