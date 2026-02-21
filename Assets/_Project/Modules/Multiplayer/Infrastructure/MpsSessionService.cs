using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

public sealed class MpsSessionService : IMultiplayerSessionService
{
    private const string DefaultErrorCode = "unknown_error";
    private const string OperationInProgressErrorCode = "operation_in_progress";
    private const string GameTagPropertyKey = "game";
    private const string GameTagPropertyValue = "dragonstride3";
    private const int DefaultQueryCount = 20;

    private readonly IMultiplayerBootstrapService _bootstrapService;
    private readonly MultiplayerConfig _config;
    private readonly SemaphoreSlim _operationLock = new(1, 1);

    private ISession _activeSession;
    private MultiplayerSessionSnapshot _activeSnapshot;
    private bool _isOperationInProgress;

    public bool IsOperationInProgress => _isOperationInProgress;
    public bool HasActiveSession => _activeSession != null;
    public MultiplayerSessionSnapshot ActiveSession => _activeSnapshot;

    public MpsSessionService(IMultiplayerBootstrapService bootstrapService, MultiplayerConfig config)
    {
        _bootstrapService = bootstrapService;
        _config = config;
    }

    public Task<MultiplayerOperationResult<MultiplayerSessionSnapshot>> CreateSessionAsync(
        MultiplayerCreateSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteLockedAsync(async token =>
        {
            if (HasActiveSession)
            {
                return MultiplayerOperationResult<MultiplayerSessionSnapshot>.Failure(
                    "already_in_session",
                    "Current player already has an active session.");
            }

            MultiplayerOperationResult<bool> readyResult = await EnsureReadyAsync(token);
            if (!readyResult.IsSuccess)
            {
                return MultiplayerOperationResult<MultiplayerSessionSnapshot>.Failure(
                    readyResult.ErrorCode,
                    readyResult.ErrorMessage);
            }

            IMultiplayerService multiplayer = MultiplayerService.Instance;
            if (multiplayer == null)
            {
                return MultiplayerOperationResult<MultiplayerSessionSnapshot>.Failure(
                    "service_unavailable",
                    "MultiplayerService.Instance is not available.");
            }

            string name = string.IsNullOrWhiteSpace(request.Name)
                ? $"DragonStride Lobby {DateTime.UtcNow:HH:mm:ss}"
                : request.Name.Trim();

            int maxPlayers = Math.Max(2, request.MaxPlayers);
            var createOptions = new SessionOptions
            {
                Name = name,
                MaxPlayers = maxPlayers,
                IsPrivate = request.IsPrivate,
                IsLocked = request.IsLocked,
                SessionProperties = new Dictionary<string, SessionProperty>
                {
                    {
                        GameTagPropertyKey,
                        new SessionProperty(GameTagPropertyValue, VisibilityPropertyOptions.Public, PropertyIndex.String1)
                    }
                }
            };

            try
            {
                IHostSession createdSession = await multiplayer.CreateSessionAsync(createOptions);
                SetActiveSession(createdSession);
                return MultiplayerOperationResult<MultiplayerSessionSnapshot>.Success(_activeSnapshot);
            }
            catch (Exception exception)
            {
                return MapException<MultiplayerSessionSnapshot>(exception);
            }
        }, cancellationToken);
    }

    public Task<MultiplayerOperationResult<IReadOnlyList<MultiplayerSessionSummary>>> QueryPublicSessionsAsync(
        MultiplayerQuerySessionsRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteLockedAsync(async token =>
        {
            MultiplayerOperationResult<bool> readyResult = await EnsureReadyAsync(token);
            if (!readyResult.IsSuccess)
            {
                return MultiplayerOperationResult<IReadOnlyList<MultiplayerSessionSummary>>.Failure(
                    readyResult.ErrorCode,
                    readyResult.ErrorMessage);
            }

            IMultiplayerService multiplayer = MultiplayerService.Instance;
            if (multiplayer == null)
            {
                return MultiplayerOperationResult<IReadOnlyList<MultiplayerSessionSummary>>.Failure(
                    "service_unavailable",
                    "MultiplayerService.Instance is not available.");
            }

            int count = request.Count > 0 ? request.Count : DefaultQueryCount;
            int skip = Math.Max(0, request.Skip);

            var queryOptions = new QuerySessionsOptions
            {
                Count = count,
                Skip = skip,
                FilterOptions = new List<FilterOption>
                {
                    new(FilterField.StringIndex1, GameTagPropertyValue, FilterOperation.Equal)
                },
                SortOptions = new List<SortOption>
                {
                    new(SortOrder.Descending, SortField.LastUpdated)
                }
            };

            try
            {
                QuerySessionsResults queryResult = await multiplayer.QuerySessionsAsync(queryOptions);
                var sessions = new List<MultiplayerSessionSummary>();

                if (queryResult?.Sessions != null)
                {
                    for (int i = 0; i < queryResult.Sessions.Count; i++)
                    {
                        ISessionInfo sessionInfo = queryResult.Sessions[i];
                        if (sessionInfo == null)
                        {
                            continue;
                        }

                        int maxPlayers = Math.Max(0, sessionInfo.MaxPlayers);
                        int availableSlots = Math.Max(0, sessionInfo.AvailableSlots);
                        int playerCount = Math.Max(0, maxPlayers - availableSlots);

                        sessions.Add(new MultiplayerSessionSummary(
                            sessionInfo.Id,
                            sessionInfo.Name,
                            sessionInfo.HostId,
                            playerCount,
                            maxPlayers,
                            availableSlots,
                            sessionInfo.IsLocked,
                            sessionInfo.HasPassword));
                    }
                }

                return MultiplayerOperationResult<IReadOnlyList<MultiplayerSessionSummary>>.Success(sessions);
            }
            catch (Exception exception)
            {
                return MapException<IReadOnlyList<MultiplayerSessionSummary>>(exception);
            }
        }, cancellationToken);
    }

    public Task<MultiplayerOperationResult<MultiplayerSessionSnapshot>> JoinSessionByCodeAsync(
        string sessionCode,
        CancellationToken cancellationToken = default)
    {
        return ExecuteLockedAsync(async token =>
        {
            if (HasActiveSession)
            {
                return MultiplayerOperationResult<MultiplayerSessionSnapshot>.Failure(
                    "already_in_session",
                    "Current player already has an active session.");
            }

            if (string.IsNullOrWhiteSpace(sessionCode))
            {
                return MultiplayerOperationResult<MultiplayerSessionSnapshot>.Failure(
                    "invalid_session_code",
                    "Join code is empty.");
            }

            MultiplayerOperationResult<bool> readyResult = await EnsureReadyAsync(token);
            if (!readyResult.IsSuccess)
            {
                return MultiplayerOperationResult<MultiplayerSessionSnapshot>.Failure(
                    readyResult.ErrorCode,
                    readyResult.ErrorMessage);
            }

            IMultiplayerService multiplayer = MultiplayerService.Instance;
            if (multiplayer == null)
            {
                return MultiplayerOperationResult<MultiplayerSessionSnapshot>.Failure(
                    "service_unavailable",
                    "MultiplayerService.Instance is not available.");
            }

            try
            {
                ISession joinedSession = await multiplayer.JoinSessionByCodeAsync(sessionCode.Trim());
                SetActiveSession(joinedSession);
                return MultiplayerOperationResult<MultiplayerSessionSnapshot>.Success(_activeSnapshot);
            }
            catch (Exception exception)
            {
                return MapException<MultiplayerSessionSnapshot>(exception);
            }
        }, cancellationToken);
    }

    public Task<MultiplayerOperationResult<MultiplayerSessionSnapshot>> JoinSessionByIdAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        return ExecuteLockedAsync(async token =>
        {
            if (HasActiveSession)
            {
                return MultiplayerOperationResult<MultiplayerSessionSnapshot>.Failure(
                    "already_in_session",
                    "Current player already has an active session.");
            }

            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return MultiplayerOperationResult<MultiplayerSessionSnapshot>.Failure(
                    "invalid_session_id",
                    "Session id is empty.");
            }

            MultiplayerOperationResult<bool> readyResult = await EnsureReadyAsync(token);
            if (!readyResult.IsSuccess)
            {
                return MultiplayerOperationResult<MultiplayerSessionSnapshot>.Failure(
                    readyResult.ErrorCode,
                    readyResult.ErrorMessage);
            }

            IMultiplayerService multiplayer = MultiplayerService.Instance;
            if (multiplayer == null)
            {
                return MultiplayerOperationResult<MultiplayerSessionSnapshot>.Failure(
                    "service_unavailable",
                    "MultiplayerService.Instance is not available.");
            }

            try
            {
                ISession joinedSession = await multiplayer.JoinSessionByIdAsync(sessionId.Trim());
                SetActiveSession(joinedSession);
                return MultiplayerOperationResult<MultiplayerSessionSnapshot>.Success(_activeSnapshot);
            }
            catch (Exception exception)
            {
                return MapException<MultiplayerSessionSnapshot>(exception);
            }
        }, cancellationToken);
    }

    public Task<MultiplayerOperationResult<MultiplayerSessionSnapshot>> RefreshActiveSessionAsync(
        CancellationToken cancellationToken = default)
    {
        return ExecuteLockedAsync(async token =>
        {
            if (_activeSession == null)
            {
                return MultiplayerOperationResult<MultiplayerSessionSnapshot>.Failure(
                    "not_in_session",
                    "No active session to refresh.");
            }

            MultiplayerOperationResult<bool> readyResult = await EnsureReadyAsync(token);
            if (!readyResult.IsSuccess)
            {
                return MultiplayerOperationResult<MultiplayerSessionSnapshot>.Failure(
                    readyResult.ErrorCode,
                    readyResult.ErrorMessage);
            }

            try
            {
                await _activeSession.RefreshAsync();
                _activeSnapshot = ToSnapshot(_activeSession);
                return MultiplayerOperationResult<MultiplayerSessionSnapshot>.Success(_activeSnapshot);
            }
            catch (Exception exception)
            {
                return MapException<MultiplayerSessionSnapshot>(exception);
            }
        }, cancellationToken);
    }

    public Task<MultiplayerOperationResult<bool>> LeaveActiveSessionAsync(CancellationToken cancellationToken = default)
    {
        return ExecuteLockedAsync(async token =>
        {
            if (_activeSession == null)
            {
                return MultiplayerOperationResult<bool>.Success(true);
            }

            ISession sessionToLeave = _activeSession;
            ClearActiveSession();

            MultiplayerOperationResult<bool> readyResult = await EnsureReadyAsync(token);
            if (!readyResult.IsSuccess)
            {
                return MultiplayerOperationResult<bool>.Failure(
                    readyResult.ErrorCode,
                    $"{readyResult.ErrorMessage} Local active session state was cleared.");
            }

            try
            {
                await sessionToLeave.LeaveAsync();
                return MultiplayerOperationResult<bool>.Success(true);
            }
            catch (Exception exception)
            {
                MultiplayerOperationResult<bool> mapped = MapException<bool>(exception);
                return MultiplayerOperationResult<bool>.Failure(
                    mapped.ErrorCode,
                    $"{mapped.ErrorMessage} Local active session state was cleared.");
            }
        }, cancellationToken);
    }

    private async Task<MultiplayerOperationResult<T>> ExecuteLockedAsync<T>(
        Func<CancellationToken, Task<MultiplayerOperationResult<T>>> operation,
        CancellationToken cancellationToken)
    {
        if (!_operationLock.Wait(0))
        {
            return MultiplayerOperationResult<T>.Failure(
                OperationInProgressErrorCode,
                "Another multiplayer operation is already in progress.");
        }

        _isOperationInProgress = true;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await operation(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return MultiplayerOperationResult<T>.Failure("cancelled", "Operation cancelled.");
        }
        catch (Exception exception)
        {
            return MapException<T>(exception);
        }
        finally
        {
            _isOperationInProgress = false;
            _operationLock.Release();
        }
    }

    private async Task<MultiplayerOperationResult<bool>> EnsureReadyAsync(CancellationToken cancellationToken)
    {
        try
        {
            MultiplayerBootstrapResult bootstrapResult = await _bootstrapService.InitializeAsync(cancellationToken);
            if (!bootstrapResult.IsSuccess)
            {
                return MultiplayerOperationResult<bool>.Failure(bootstrapResult.ErrorCode, bootstrapResult.ErrorMessage);
            }

            return MultiplayerOperationResult<bool>.Success(true);
        }
        catch (Exception exception)
        {
            return MapException<bool>(exception);
        }
    }

    private void SetActiveSession(ISession session)
    {
        _activeSession = session;
        _activeSnapshot = session == null ? default : ToSnapshot(session);
    }

    private void ClearActiveSession()
    {
        _activeSession = null;
        _activeSnapshot = default;
    }

    private static MultiplayerSessionSnapshot ToSnapshot(ISession session)
    {
        if (session == null)
        {
            return default;
        }

        return new MultiplayerSessionSnapshot(
            session.Id ?? string.Empty,
            session.Code ?? string.Empty,
            session.Name ?? string.Empty,
            session.Host ?? string.Empty,
            Math.Max(0, session.PlayerCount),
            Math.Max(0, session.MaxPlayers),
            Math.Max(0, session.AvailableSlots),
            session.IsHost,
            session.IsLocked,
            session.IsPrivate,
            session.HasPassword);
    }

    private static MultiplayerOperationResult<T> MapException<T>(Exception exception)
    {
        if (exception is OperationCanceledException)
        {
            return MultiplayerOperationResult<T>.Failure("cancelled", "Operation cancelled.");
        }

        if (exception is SessionException sessionException)
        {
            return MultiplayerOperationResult<T>.Failure(
                $"session_{sessionException.Error}".ToLowerInvariant(),
                NormalizeMessage(sessionException.Message));
        }

        if (exception is RequestFailedException requestFailedException)
        {
            return MultiplayerOperationResult<T>.Failure(
                $"request_failed_{requestFailedException.ErrorCode}",
                NormalizeMessage(requestFailedException.Message));
        }

        Debug.LogWarning($"[MpsSessionService] Unexpected error: {exception}");
        return MultiplayerOperationResult<T>.Failure(DefaultErrorCode, NormalizeMessage(exception.Message));
    }

    private static string NormalizeMessage(string message)
    {
        return string.IsNullOrWhiteSpace(message) ? "Unknown error." : message;
    }
}
