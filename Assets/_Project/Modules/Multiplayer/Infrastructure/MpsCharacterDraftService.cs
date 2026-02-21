using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Multiplayer;

public sealed class MpsCharacterDraftService : ICharacterDraftService
{
    public const string PhaseLobby = "lobby";
    public const string PhaseDraft = "draft";
    public const string PhaseInGame = "in_game";

    private const string SessionPhaseKey = "ds3_phase";
    private const string SessionMatchSeedKey = "ds3_match_seed";
    private const string PlayerPickCharacterIdKey = "ds3_pick_character_id";
    private const string PlayerPickNameKey = "ds3_pick_name";
    private const string PlayerPickConfirmedKey = "ds3_pick_confirmed";
    private const string PlayerPickUpdatedAtKey = "ds3_pick_updated_at";

    private const int MinNameLength = 3;
    private const int MaxNameLength = 16;
    private static readonly TimeSpan SnapshotRefreshThrottle = TimeSpan.FromSeconds(3);

    private readonly IMultiplayerSessionService _sessionService;
    private readonly SemaphoreSlim _operationLock = new(1, 1);

    private bool _isOperationInProgress;
    private DateTime _lastSessionRefreshUtc = DateTime.MinValue;

    public bool IsOperationInProgress => _isOperationInProgress;

    public MpsCharacterDraftService(IMultiplayerSessionService sessionService)
    {
        _sessionService = sessionService;
    }

    public Task<MultiplayerOperationResult<CharacterDraftSnapshot>> GetSnapshotAsync(
        CancellationToken cancellationToken = default)
    {
        return ExecuteLockedAsync(async token =>
        {
            bool needsRefresh = DateTime.UtcNow - _lastSessionRefreshUtc >= SnapshotRefreshThrottle;
            MultiplayerOperationResult<ISession> sessionResult =
                await ResolveSessionAsync(refreshSession: needsRefresh, token);
            if (!sessionResult.IsSuccess)
            {
                return MultiplayerOperationResult<CharacterDraftSnapshot>.Failure(
                    sessionResult.ErrorCode,
                    sessionResult.ErrorMessage);
            }

            if (needsRefresh)
            {
                _lastSessionRefreshUtc = DateTime.UtcNow;
            }

            return MultiplayerOperationResult<CharacterDraftSnapshot>.Success(ToSnapshot(sessionResult.Value));
        }, cancellationToken);
    }

    public Task<MultiplayerOperationResult<CharacterDraftSnapshot>> SetPhaseAsync(
        string phase,
        CancellationToken cancellationToken = default)
    {
        return ExecuteLockedAsync(async token =>
        {
            string normalizedPhase = NormalizePhase(phase);
            if (string.IsNullOrWhiteSpace(normalizedPhase))
            {
                return MultiplayerOperationResult<CharacterDraftSnapshot>.Failure(
                    "invalid_phase",
                    "Phase must be lobby, draft, or in_game.");
            }

            MultiplayerOperationResult<ISession> sessionResult =
                await ResolveSessionAsync(refreshSession: false, token);
            if (!sessionResult.IsSuccess)
            {
                return MultiplayerOperationResult<CharacterDraftSnapshot>.Failure(
                    sessionResult.ErrorCode,
                    sessionResult.ErrorMessage);
            }

            ISession session = sessionResult.Value;
            if (!session.IsHost)
            {
                return MultiplayerOperationResult<CharacterDraftSnapshot>.Failure(
                    "forbidden",
                    "Only host can update session phase.");
            }

            try
            {
                IHostSession hostSession = session.AsHost();
                hostSession.IsLocked = normalizedPhase == PhaseInGame;
                hostSession.SetProperty(SessionPhaseKey, new SessionProperty(normalizedPhase, VisibilityPropertyOptions.Member));
                if (normalizedPhase == PhaseInGame)
                {
                    int matchSeed = ResolveMatchSeed(session);
                    hostSession.SetProperty(
                        SessionMatchSeedKey,
                        new SessionProperty(matchSeed.ToString(CultureInfo.InvariantCulture), VisibilityPropertyOptions.Member));
                }
                await hostSession.SavePropertiesAsync();
                await session.RefreshAsync();
                _lastSessionRefreshUtc = DateTime.UtcNow;
                return MultiplayerOperationResult<CharacterDraftSnapshot>.Success(ToSnapshot(session));
            }
            catch (Exception exception)
            {
                return MapException<CharacterDraftSnapshot>(exception);
            }
        }, cancellationToken);
    }

    public Task<MultiplayerOperationResult<CharacterDraftSnapshot>> SubmitSelectionAsync(
        string characterId,
        string characterName,
        bool confirmed,
        CancellationToken cancellationToken = default)
    {
        return ExecuteLockedAsync(async token =>
        {
            string normalizedCharacterId = string.IsNullOrWhiteSpace(characterId) ? string.Empty : characterId.Trim();
            if (string.IsNullOrWhiteSpace(normalizedCharacterId))
            {
                return MultiplayerOperationResult<CharacterDraftSnapshot>.Failure(
                    "invalid_character",
                    "Character id is required.");
            }

            string normalizedName = NormalizePlayerName(characterName);
            if (!IsNameValid(normalizedName))
            {
                return MultiplayerOperationResult<CharacterDraftSnapshot>.Failure(
                    "invalid_name",
                    $"Name must be {MinNameLength}..{MaxNameLength} symbols and contain only letters, digits, spaces, '-' or '_'.");
            }

            MultiplayerOperationResult<ISession> sessionResult =
                await ResolveSessionAsync(refreshSession: false, token);
            if (!sessionResult.IsSuccess)
            {
                return MultiplayerOperationResult<CharacterDraftSnapshot>.Failure(
                    sessionResult.ErrorCode,
                    sessionResult.ErrorMessage);
            }

            ISession session = sessionResult.Value;
            IPlayer currentPlayer = session.CurrentPlayer;
            if (currentPlayer == null)
            {
                return MultiplayerOperationResult<CharacterDraftSnapshot>.Failure(
                    "player_not_found",
                    "Current player is not available in active session.");
            }

            try
            {
                long updatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                currentPlayer.SetProperty(PlayerPickCharacterIdKey, new PlayerProperty(normalizedCharacterId, VisibilityPropertyOptions.Member));
                currentPlayer.SetProperty(PlayerPickNameKey, new PlayerProperty(normalizedName, VisibilityPropertyOptions.Member));
                currentPlayer.SetProperty(PlayerPickConfirmedKey, new PlayerProperty(confirmed ? "1" : "0", VisibilityPropertyOptions.Member));
                currentPlayer.SetProperty(PlayerPickUpdatedAtKey, new PlayerProperty(updatedAt.ToString(CultureInfo.InvariantCulture), VisibilityPropertyOptions.Member));

                await session.SaveCurrentPlayerDataAsync();
                await session.RefreshAsync();
                _lastSessionRefreshUtc = DateTime.UtcNow;
                return MultiplayerOperationResult<CharacterDraftSnapshot>.Success(ToSnapshot(session));
            }
            catch (Exception exception)
            {
                return MapException<CharacterDraftSnapshot>(exception);
            }
        }, cancellationToken);
    }

    public Task<MultiplayerOperationResult<CharacterDraftSnapshot>> ClearSelectionAsync(
        CancellationToken cancellationToken = default)
    {
        return ExecuteLockedAsync(async token =>
        {
            MultiplayerOperationResult<ISession> sessionResult =
                await ResolveSessionAsync(refreshSession: false, token);
            if (!sessionResult.IsSuccess)
            {
                return MultiplayerOperationResult<CharacterDraftSnapshot>.Failure(
                    sessionResult.ErrorCode,
                    sessionResult.ErrorMessage);
            }

            ISession session = sessionResult.Value;
            IPlayer currentPlayer = session.CurrentPlayer;
            if (currentPlayer == null)
            {
                return MultiplayerOperationResult<CharacterDraftSnapshot>.Failure(
                    "player_not_found",
                    "Current player is not available in active session.");
            }

            try
            {
                long updatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                currentPlayer.SetProperty(PlayerPickCharacterIdKey, new PlayerProperty(string.Empty, VisibilityPropertyOptions.Member));
                currentPlayer.SetProperty(PlayerPickNameKey, new PlayerProperty(string.Empty, VisibilityPropertyOptions.Member));
                currentPlayer.SetProperty(PlayerPickConfirmedKey, new PlayerProperty("0", VisibilityPropertyOptions.Member));
                currentPlayer.SetProperty(PlayerPickUpdatedAtKey, new PlayerProperty(updatedAt.ToString(CultureInfo.InvariantCulture), VisibilityPropertyOptions.Member));
                await session.SaveCurrentPlayerDataAsync();
                await session.RefreshAsync();
                _lastSessionRefreshUtc = DateTime.UtcNow;
                return MultiplayerOperationResult<CharacterDraftSnapshot>.Success(ToSnapshot(session));
            }
            catch (Exception exception)
            {
                return MapException<CharacterDraftSnapshot>(exception);
            }
        }, cancellationToken);
    }

    private async Task<MultiplayerOperationResult<ISession>> ResolveSessionAsync(
        bool refreshSession,
        CancellationToken cancellationToken)
    {
        if (!_sessionService.HasActiveSession)
        {
            return MultiplayerOperationResult<ISession>.Failure("not_in_session", "Current player has no active session.");
        }

        string activeSessionId = _sessionService.ActiveSession.SessionId;
        if (refreshSession || string.IsNullOrWhiteSpace(activeSessionId))
        {
            MultiplayerOperationResult<MultiplayerSessionSnapshot> refreshResult =
                await _sessionService.RefreshActiveSessionAsync(cancellationToken);
            if (!refreshResult.IsSuccess)
            {
                return MultiplayerOperationResult<ISession>.Failure(refreshResult.ErrorCode, refreshResult.ErrorMessage);
            }

            activeSessionId = refreshResult.Value.SessionId;
        }

        IMultiplayerService multiplayer = MultiplayerService.Instance;
        if (multiplayer?.Sessions == null)
        {
            return MultiplayerOperationResult<ISession>.Failure("service_unavailable", "Multiplayer sessions storage is unavailable.");
        }

        if (TryGetSessionHandle(multiplayer, activeSessionId, out ISession sessionHandle))
        {
            return MultiplayerOperationResult<ISession>.Success(sessionHandle);
        }

        if (!refreshSession)
        {
            MultiplayerOperationResult<MultiplayerSessionSnapshot> refreshFallbackResult =
                await _sessionService.RefreshActiveSessionAsync(cancellationToken);
            if (refreshFallbackResult.IsSuccess &&
                TryGetSessionHandle(multiplayer, refreshFallbackResult.Value.SessionId, out sessionHandle))
            {
                return MultiplayerOperationResult<ISession>.Success(sessionHandle);
            }
        }

        return MultiplayerOperationResult<ISession>.Failure(
            "session_handle_not_found",
            "Active session handle was not found in MultiplayerService sessions registry.");
    }

    private static bool TryGetSessionHandle(IMultiplayerService multiplayer, string sessionId, out ISession session)
    {
        session = null;
        if (multiplayer?.Sessions == null || string.IsNullOrWhiteSpace(sessionId))
        {
            return false;
        }

        foreach (KeyValuePair<string, ISession> pair in multiplayer.Sessions)
        {
            ISession value = pair.Value;
            if (value != null && string.Equals(value.Id, sessionId, StringComparison.Ordinal))
            {
                session = value;
                return true;
            }
        }

        return false;
    }

    private static CharacterDraftSnapshot ToSnapshot(ISession session)
    {
        string sessionId = session?.Id ?? string.Empty;
        string phase = GetSessionPropertyValue(session, SessionPhaseKey, PhaseLobby);
        int matchSeed = ParseInt(GetSessionPropertyValue(session, SessionMatchSeedKey, string.Empty));
        string localPlayerId = session?.CurrentPlayer?.Id ?? string.Empty;
        bool isLocalHost = session != null && session.IsHost;

        var players = new List<CharacterDraftPlayerSnapshot>();
        var pickSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var nameSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        bool uniquePicks = true;
        bool uniqueNames = true;

        string localCharacterId = string.Empty;
        string localCharacterName = string.Empty;
        bool localConfirmed = false;

        if (session?.Players != null)
        {
            for (int i = 0; i < session.Players.Count; i++)
            {
                IReadOnlyPlayer player = session.Players[i];
                if (player == null)
                {
                    continue;
                }

                string playerId = player.Id ?? string.Empty;
                string characterId = GetPlayerPropertyValue(player, PlayerPickCharacterIdKey);
                string characterName = NormalizePlayerName(GetPlayerPropertyValue(player, PlayerPickNameKey));
                bool confirmed = GetPlayerPropertyValue(player, PlayerPickConfirmedKey) == "1";
                long updatedAt = ParseLong(GetPlayerPropertyValue(player, PlayerPickUpdatedAtKey));
                bool isHost = string.Equals(playerId, session.Host, StringComparison.Ordinal);

                if (!string.IsNullOrWhiteSpace(characterId) && !pickSet.Add(characterId.Trim()))
                {
                    uniquePicks = false;
                }

                if (!string.IsNullOrWhiteSpace(characterName) && !nameSet.Add(characterName))
                {
                    uniqueNames = false;
                }

                if (string.Equals(playerId, localPlayerId, StringComparison.Ordinal))
                {
                    localCharacterId = characterId;
                    localCharacterName = characterName;
                    localConfirmed = confirmed;
                }

                players.Add(new CharacterDraftPlayerSnapshot(
                    playerId,
                    isHost,
                    characterId,
                    characterName,
                    confirmed,
                    updatedAt));
            }
        }

        bool allConfirmed = players.Count > 0;
        for (int i = 0; i < players.Count; i++)
        {
            CharacterDraftPlayerSnapshot player = players[i];
            if (!player.IsConfirmed ||
                string.IsNullOrWhiteSpace(player.CharacterId) ||
                !IsNameValid(player.CharacterName))
            {
                allConfirmed = false;
                break;
            }
        }

        if (!uniquePicks || !uniqueNames)
        {
            allConfirmed = false;
        }

        return new CharacterDraftSnapshot(
            sessionId,
            phase,
            isLocalHost,
            localPlayerId,
            matchSeed,
            localCharacterId,
            localCharacterName,
            localConfirmed,
            allConfirmed,
            uniquePicks,
            uniqueNames,
            players);
    }

    private Task<MultiplayerOperationResult<T>> ExecuteLockedAsync<T>(
        Func<CancellationToken, Task<MultiplayerOperationResult<T>>> operation,
        CancellationToken cancellationToken)
    {
        return ExecuteLockedInternalAsync(operation, cancellationToken);
    }

    private async Task<MultiplayerOperationResult<T>> ExecuteLockedInternalAsync<T>(
        Func<CancellationToken, Task<MultiplayerOperationResult<T>>> operation,
        CancellationToken cancellationToken)
    {
        if (!_operationLock.Wait(0))
        {
            return MultiplayerOperationResult<T>.Failure("operation_in_progress", "Another draft operation is already running.");
        }

        _isOperationInProgress = true;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await operation(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return MultiplayerOperationResult<T>.Failure("cancelled", "Draft operation was cancelled.");
        }
        finally
        {
            _isOperationInProgress = false;
            _operationLock.Release();
        }
    }

    private static MultiplayerOperationResult<T> MapException<T>(Exception exception)
    {
        if (exception is SessionException sessionException)
        {
            return MultiplayerOperationResult<T>.Failure(
                $"session_{sessionException.Error}",
                string.IsNullOrWhiteSpace(sessionException.Message) ? "Session operation failed." : sessionException.Message);
        }

        if (exception is RequestFailedException requestFailedException)
        {
            return MultiplayerOperationResult<T>.Failure(
                $"request_failed_{requestFailedException.ErrorCode}",
                string.IsNullOrWhiteSpace(requestFailedException.Message)
                    ? "Request failed."
                    : requestFailedException.Message);
        }

        return MultiplayerOperationResult<T>.Failure(
            "unknown_error",
            string.IsNullOrWhiteSpace(exception?.Message) ? "Unknown draft error." : exception.Message);
    }

    private static string GetSessionPropertyValue(ISession session, string key, string fallback)
    {
        if (session?.Properties != null &&
            session.Properties.TryGetValue(key, out SessionProperty property) &&
            !string.IsNullOrWhiteSpace(property?.Value))
        {
            return property.Value.Trim();
        }

        return fallback;
    }

    private static string GetPlayerPropertyValue(IReadOnlyPlayer player, string key)
    {
        if (player?.Properties != null &&
            player.Properties.TryGetValue(key, out PlayerProperty property) &&
            !string.IsNullOrWhiteSpace(property?.Value))
        {
            return property.Value.Trim();
        }

        return string.Empty;
    }

    private static string NormalizePhase(string phase)
    {
        string normalized = string.IsNullOrWhiteSpace(phase) ? string.Empty : phase.Trim().ToLowerInvariant();
        if (normalized == PhaseLobby || normalized == PhaseDraft || normalized == PhaseInGame)
        {
            return normalized;
        }

        return string.Empty;
    }

    private static string NormalizePlayerName(string name)
    {
        return string.IsNullOrWhiteSpace(name) ? string.Empty : name.Trim();
    }

    private static bool IsNameValid(string name)
    {
        string normalized = NormalizePlayerName(name);
        if (normalized.Length < MinNameLength || normalized.Length > MaxNameLength)
        {
            return false;
        }

        for (int i = 0; i < normalized.Length; i++)
        {
            char c = normalized[i];
            if (char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == '_')
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private static long ParseLong(string value)
    {
        return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long parsed)
            ? parsed
            : 0L;
    }

    private static int ParseInt(string value)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
            ? parsed
            : 0;
    }

    private static int ResolveMatchSeed(ISession session)
    {
        int existingSeed = ParseInt(GetSessionPropertyValue(session, SessionMatchSeedKey, string.Empty));
        if (existingSeed != 0)
        {
            return existingSeed;
        }

        int seed = DateTime.UtcNow.GetHashCode();
        if (!string.IsNullOrWhiteSpace(session?.Id))
        {
            seed ^= session.Id.GetHashCode();
        }

        if (seed == 0)
        {
            seed = 1;
        }

        return seed;
    }
}
