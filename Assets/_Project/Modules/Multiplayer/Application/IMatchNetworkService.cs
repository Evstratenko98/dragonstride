using System.Threading;
using System.Threading.Tasks;

public interface IMatchNetworkService
{
    bool IsReady { get; }
    bool IsHost { get; }
    bool IsClient { get; }
    string LocalPlayerId { get; }

    Task<MultiplayerOperationResult<bool>> EnsurePreconnectedAsync(CancellationToken ct = default);
    Task<MultiplayerOperationResult<bool>> WaitForMatchConnectivityAsync(CancellationToken ct = default);
    Task ShutdownAsync(CancellationToken ct = default);
}
