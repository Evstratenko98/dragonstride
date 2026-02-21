using System.Threading;
using System.Threading.Tasks;

public interface IMatchActionTimelineService
{
    bool IsPlaybackInProgress { get; }

    void PrimeBaseline(MatchStateSnapshot snapshot);
    Task<MultiplayerOperationResult<MatchActionBatch>> BuildBatchForAcceptedCommandAsync(
        GameCommandEnvelope command,
        CancellationToken ct = default);
    Task PlayBatchLocallyAsync(MatchActionBatch batch, CancellationToken ct = default);
}
