using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface IMultiplayerSessionService
{
    bool IsOperationInProgress { get; }
    bool HasActiveSession { get; }
    MultiplayerSessionSnapshot ActiveSession { get; }

    Task<MultiplayerOperationResult<MultiplayerSessionSnapshot>> CreateSessionAsync(
        MultiplayerCreateSessionRequest request,
        CancellationToken cancellationToken = default);

    Task<MultiplayerOperationResult<IReadOnlyList<MultiplayerSessionSummary>>> QueryPublicSessionsAsync(
        MultiplayerQuerySessionsRequest request,
        CancellationToken cancellationToken = default);

    Task<MultiplayerOperationResult<MultiplayerSessionSnapshot>> JoinSessionByCodeAsync(
        string sessionCode,
        CancellationToken cancellationToken = default);

    Task<MultiplayerOperationResult<MultiplayerSessionSnapshot>> JoinSessionByIdAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    Task<MultiplayerOperationResult<MultiplayerSessionSnapshot>> RefreshActiveSessionAsync(
        CancellationToken cancellationToken = default);

    Task<MultiplayerOperationResult<bool>> LeaveActiveSessionAsync(
        CancellationToken cancellationToken = default);
}
