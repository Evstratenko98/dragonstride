using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VContainer.Unity;

public sealed class LobbyPresenter : IStartable, IDisposable
{
    private const int QueryPageSize = 20;

    private readonly LobbyView _view;
    private readonly IMultiplayerSessionService _sessionService;
    private readonly ICharacterDraftService _draftService;
    private readonly IMatchSetupContextService _matchSetupContextService;
    private readonly IMatchNetworkService _matchNetworkService;
    private readonly MultiplayerConfig _multiplayerConfig;
    private readonly ISessionSceneRouter _sceneRouter;
    private readonly CancellationTokenSource _pollingCts = new();

    private bool _isBusy;

    public LobbyPresenter(
        LobbyView view,
        IMultiplayerSessionService sessionService,
        ICharacterDraftService draftService,
        IMatchSetupContextService matchSetupContextService,
        IMatchNetworkService matchNetworkService,
        MultiplayerConfig multiplayerConfig,
        ISessionSceneRouter sceneRouter)
    {
        _view = view;
        _sessionService = sessionService;
        _draftService = draftService;
        _matchSetupContextService = matchSetupContextService;
        _matchNetworkService = matchNetworkService;
        _multiplayerConfig = multiplayerConfig;
        _sceneRouter = sceneRouter;
    }

    public void Start()
    {
        _view.SetLobbyPlaceholderText("Loading public lobbies...");
        _view.SetStatus("Initializing lobby session service...");
        _matchSetupContextService.Clear();
        UpdateActiveSessionBlock();
        ApplyButtonsState();

        if (_view.CreateLobbyButton != null)
        {
            _view.CreateLobbyButton.onClick.AddListener(OnCreateLobbyClicked);
        }

        if (_view.RefreshButton != null)
        {
            _view.RefreshButton.onClick.AddListener(OnRefreshClicked);
        }

        if (_view.JoinByCodeButton != null)
        {
            _view.JoinByCodeButton.onClick.AddListener(OnJoinByCodeClicked);
        }

        if (_view.StartMatchButton != null)
        {
            _view.StartMatchButton.onClick.AddListener(OnStartMatchClicked);
        }

        if (_view.BackToMenuButton != null)
        {
            _view.BackToMenuButton.onClick.AddListener(OnBackToMenuClicked);
        }

        _ = RefreshSessionsFromUserActionAsync(_pollingCts.Token);
        _ = PollLoopAsync(_pollingCts.Token);
    }

    public void Dispose()
    {
        if (_view != null)
        {
            if (_view.CreateLobbyButton != null)
            {
                _view.CreateLobbyButton.onClick.RemoveListener(OnCreateLobbyClicked);
            }

            if (_view.RefreshButton != null)
            {
                _view.RefreshButton.onClick.RemoveListener(OnRefreshClicked);
            }

            if (_view.JoinByCodeButton != null)
            {
                _view.JoinByCodeButton.onClick.RemoveListener(OnJoinByCodeClicked);
            }

            if (_view.StartMatchButton != null)
            {
                _view.StartMatchButton.onClick.RemoveListener(OnStartMatchClicked);
            }

            if (_view.BackToMenuButton != null)
            {
                _view.BackToMenuButton.onClick.RemoveListener(OnBackToMenuClicked);
            }
        }

        if (!_pollingCts.IsCancellationRequested)
        {
            _pollingCts.Cancel();
        }

        _pollingCts.Dispose();
    }

    private async void OnCreateLobbyClicked()
    {
        await CreateLobbyAsync(_pollingCts.Token);
    }

    private async void OnRefreshClicked()
    {
        await RefreshSessionsFromUserActionAsync(_pollingCts.Token);
    }

    private async void OnJoinByCodeClicked()
    {
        await JoinByCodeAsync(_pollingCts.Token);
    }

    private async void OnStartMatchClicked()
    {
        await StartMatchAsync(_pollingCts.Token);
    }

    private async void OnBackToMenuClicked()
    {
        await BackToMenuAsync(_pollingCts.Token);
    }

    private async Task CreateLobbyAsync(CancellationToken cancellationToken)
    {
        if (!TryBeginBusy())
        {
            return;
        }

        try
        {
            if (_sessionService.HasActiveSession)
            {
                _view.SetStatus("You are already in an active session.");
                UpdateActiveSessionBlock();
                return;
            }

            int maxPlayers = _multiplayerConfig != null ? _multiplayerConfig.MaxPlayers : 4;
            var request = new MultiplayerCreateSessionRequest(
                $"DragonStride Lobby {DateTime.Now:HH:mm:ss}",
                maxPlayers,
                isPrivate: false,
                isLocked: false,
                enableRelayPreconnect: true,
                region: _multiplayerConfig != null ? _multiplayerConfig.DefaultRegion : "auto");

            _view.SetStatus("Creating lobby...");
            MultiplayerOperationResult<MultiplayerSessionSnapshot> result =
                await _sessionService.CreateSessionAsync(request, cancellationToken);

            if (!result.IsSuccess)
            {
                _view.SetStatus(FormatError("Create lobby failed", result));
                UpdateActiveSessionBlock();
                return;
            }

            MultiplayerOperationResult<bool> networkReadyResult =
                await _matchNetworkService.EnsurePreconnectedAsync(cancellationToken);
            if (!networkReadyResult.IsSuccess)
            {
                _view.SetStatus(FormatError("Lobby created, but network preconnect failed", networkReadyResult));
            }

            UpdateActiveSessionBlock();
            MultiplayerOperationResult<IReadOnlyList<MultiplayerSessionSummary>> refreshResult =
                await QueryAndRenderSessionsAsync(cancellationToken);

            _view.SetStatus(refreshResult.IsSuccess
                ? "Lobby created."
                : $"Lobby created, but list refresh failed: {refreshResult.ErrorCode}");
        }
        catch (OperationCanceledException)
        {
            _view.SetStatus("Lobby creation cancelled.");
        }
        finally
        {
            EndBusy();
        }
    }

    private async Task RefreshSessionsFromUserActionAsync(CancellationToken cancellationToken)
    {
        if (!TryBeginBusy())
        {
            return;
        }

        try
        {
            _view.SetStatus("Refreshing public lobbies...");
            MultiplayerOperationResult<IReadOnlyList<MultiplayerSessionSummary>> result =
                await QueryAndRenderSessionsAsync(cancellationToken);

            if (!result.IsSuccess)
            {
                _view.SetStatus(FormatError("Refresh failed", result));
                return;
            }

            _view.SetStatus($"Lobbies updated: {result.Value.Count}");
        }
        catch (OperationCanceledException)
        {
            _view.SetStatus("Lobby refresh cancelled.");
        }
        finally
        {
            EndBusy();
        }
    }

    private async Task JoinByCodeAsync(CancellationToken cancellationToken)
    {
        if (!TryBeginBusy())
        {
            return;
        }

        try
        {
            if (_sessionService.HasActiveSession)
            {
                _view.SetStatus("Leave current session before joining another.");
                return;
            }

            string joinCode = _view.GetJoinCode().Trim();
            if (string.IsNullOrWhiteSpace(joinCode))
            {
                _view.SetStatus("Enter a join code.");
                return;
            }

            _view.SetStatus("Joining by code...");
            MultiplayerOperationResult<MultiplayerSessionSnapshot> joinResult =
                await _sessionService.JoinSessionByCodeAsync(joinCode, cancellationToken);

            if (!joinResult.IsSuccess)
            {
                _view.SetStatus(FormatError("Join by code failed", joinResult));
                return;
            }

            MultiplayerOperationResult<bool> networkReadyResult =
                await _matchNetworkService.EnsurePreconnectedAsync(cancellationToken);
            if (!networkReadyResult.IsSuccess)
            {
                _view.SetStatus(FormatError("Joined session, but network preconnect failed", networkReadyResult));
            }

            _view.ClearJoinCode();
            UpdateActiveSessionBlock();
            MultiplayerOperationResult<IReadOnlyList<MultiplayerSessionSummary>> refreshResult =
                await QueryAndRenderSessionsAsync(cancellationToken);

            _view.SetStatus(refreshResult.IsSuccess
                ? "Joined session by code."
                : $"Joined session, but list refresh failed: {refreshResult.ErrorCode}");
        }
        catch (OperationCanceledException)
        {
            _view.SetStatus("Join by code cancelled.");
        }
        finally
        {
            EndBusy();
        }
    }

    private async Task JoinByIdAsync(string sessionId, CancellationToken cancellationToken)
    {
        if (!TryBeginBusy())
        {
            return;
        }

        try
        {
            if (_sessionService.HasActiveSession)
            {
                _view.SetStatus("Leave current session before joining another.");
                return;
            }

            if (string.IsNullOrWhiteSpace(sessionId))
            {
                _view.SetStatus("Invalid session id.");
                return;
            }

            _view.SetStatus("Joining selected session...");
            MultiplayerOperationResult<MultiplayerSessionSnapshot> joinResult =
                await _sessionService.JoinSessionByIdAsync(sessionId, cancellationToken);

            if (!joinResult.IsSuccess)
            {
                _view.SetStatus(FormatError("Join session failed", joinResult));
                return;
            }

            MultiplayerOperationResult<bool> networkReadyResult =
                await _matchNetworkService.EnsurePreconnectedAsync(cancellationToken);
            if (!networkReadyResult.IsSuccess)
            {
                _view.SetStatus(FormatError("Joined session, but network preconnect failed", networkReadyResult));
            }

            UpdateActiveSessionBlock();
            MultiplayerOperationResult<IReadOnlyList<MultiplayerSessionSummary>> refreshResult =
                await QueryAndRenderSessionsAsync(cancellationToken);

            _view.SetStatus(refreshResult.IsSuccess
                ? "Joined selected session."
                : $"Joined session, but list refresh failed: {refreshResult.ErrorCode}");
        }
        catch (OperationCanceledException)
        {
            _view.SetStatus("Join cancelled.");
        }
        finally
        {
            EndBusy();
        }
    }

    private async Task StartMatchAsync(CancellationToken cancellationToken)
    {
        if (!TryBeginBusy())
        {
            return;
        }

        try
        {
            if (!_sessionService.HasActiveSession)
            {
                _view.SetStatus("Create or join a session before starting a match.");
                return;
            }

            MultiplayerSessionSnapshot snapshot = _sessionService.ActiveSession;
            if (!snapshot.IsHost)
            {
                _view.SetStatus("Only host can start the match.");
                return;
            }

            _view.SetStatus("Opening character draft...");
            MultiplayerOperationResult<CharacterDraftSnapshot> phaseResult =
                await _draftService.SetPhaseAsync(MpsCharacterDraftService.PhaseDraft, cancellationToken);

            if (!phaseResult.IsSuccess)
            {
                _view.SetStatus(FormatError("Unable to switch to draft phase", phaseResult));
                return;
            }

            bool loaded = await _sceneRouter.LoadCharacterSelectAsync();
            if (!loaded)
            {
                _view.SetStatus("Failed to load CharacterSelect.");
            }
        }
        catch (OperationCanceledException)
        {
            _view.SetStatus("Start match cancelled.");
        }
        finally
        {
            EndBusy();
        }
    }

    private async Task BackToMenuAsync(CancellationToken cancellationToken)
    {
        if (!TryBeginBusy())
        {
            return;
        }

        try
        {
            _view.SetStatus("Leaving session...");

            if (_sessionService.HasActiveSession && _sessionService.ActiveSession.IsHost)
            {
                await _draftService.SetPhaseAsync(MpsCharacterDraftService.PhaseLobby, cancellationToken);
            }

            MultiplayerOperationResult<bool> leaveResult =
                await _sessionService.LeaveActiveSessionAsync(cancellationToken);
            await _matchNetworkService.ShutdownAsync(cancellationToken);

            if (!leaveResult.IsSuccess)
            {
                _view.SetStatus(
                    $"Leave failed ({leaveResult.ErrorCode}). Returning to menu with local session reset.");
            }

            bool loaded = await _sceneRouter.LoadMainMenuAsync();
            if (!loaded)
            {
                _view.SetStatus("Failed to return to MainMenu.");
            }

            _matchSetupContextService.Clear();
        }
        catch (OperationCanceledException)
        {
            _view.SetStatus("Back to menu cancelled.");
        }
        finally
        {
            EndBusy();
        }
    }

    private async Task PollLoopAsync(CancellationToken cancellationToken)
    {
        float interval = _multiplayerConfig != null ? _multiplayerConfig.LobbyRefreshIntervalSeconds : 2f;
        interval = Math.Max(3f, interval);
        int delayMs = (int)Math.Round(interval * 1000f);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(delayMs, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (_isBusy || _sceneRouter.IsTransitionInProgress || _sessionService.IsOperationInProgress)
            {
                continue;
            }

            await RefreshSessionsByPollingAsync(cancellationToken);
            await TryAutoRouteBySessionPhaseAsync(cancellationToken);
        }
    }

    private async Task RefreshSessionsByPollingAsync(CancellationToken cancellationToken)
    {
        MultiplayerOperationResult<IReadOnlyList<MultiplayerSessionSummary>> result =
            await QueryAndRenderSessionsAsync(cancellationToken);

        if (!result.IsSuccess)
        {
            return;
        }

        if (!_sessionService.HasActiveSession && result.Value.Count == 0)
        {
            _view.SetStatus("No public lobbies available.");
        }
    }

    private async Task TryAutoRouteBySessionPhaseAsync(CancellationToken cancellationToken)
    {
        if (!_sessionService.HasActiveSession || _isBusy || _sceneRouter.IsTransitionInProgress || _draftService.IsOperationInProgress)
        {
            return;
        }

        MultiplayerOperationResult<CharacterDraftSnapshot> draftResult =
            await _draftService.GetSnapshotAsync(cancellationToken);

        if (!draftResult.IsSuccess)
        {
            return;
        }

        CharacterDraftSnapshot snapshot = draftResult.Value;
        if (string.Equals(snapshot.Phase, MpsCharacterDraftService.PhaseDraft, StringComparison.OrdinalIgnoreCase))
        {
            await _sceneRouter.LoadCharacterSelectAsync();
            return;
        }

        if (string.Equals(snapshot.Phase, MpsCharacterDraftService.PhaseInGame, StringComparison.OrdinalIgnoreCase))
        {
            if (!TrySetMatchSetupFromSnapshot(snapshot))
            {
                _view.SetStatus("Match setup is invalid in current session snapshot.");
                return;
            }

            await _sceneRouter.LoadGameSceneAsync();
        }
    }

    private async Task<MultiplayerOperationResult<IReadOnlyList<MultiplayerSessionSummary>>> QueryAndRenderSessionsAsync(
        CancellationToken cancellationToken)
    {
        var request = new MultiplayerQuerySessionsRequest(QueryPageSize, 0);
        MultiplayerOperationResult<IReadOnlyList<MultiplayerSessionSummary>> result =
            await _sessionService.QueryPublicSessionsAsync(request, cancellationToken);

        if (result.IsSuccess)
        {
            RenderSessionList(result.Value);
        }
        else
        {
            _view.ClearSessionRows();
            _view.SetLobbyPlaceholderText("Unable to load lobby list.");
        }

        UpdateActiveSessionBlock();
        ApplyButtonsState();
        return result;
    }

    private void RenderSessionList(IReadOnlyList<MultiplayerSessionSummary> sessions)
    {
        _view.ClearSessionRows();

        if (sessions == null || sessions.Count == 0)
        {
            _view.SetLobbyPlaceholderText("No public lobbies found. Create one to begin.");
            return;
        }

        _view.SetLobbyPlaceholderText($"Public lobbies: {sessions.Count}");

        for (int i = 0; i < sessions.Count; i++)
        {
            MultiplayerSessionSummary summary = sessions[i];
            LobbySessionListItemView row = _view.CreateSessionRow();
            if (row == null)
            {
                continue;
            }

            bool canJoin = IsSessionJoinable(summary);
            row.Bind(summary, canJoin, OnSessionSelected);
        }
    }

    private void OnSessionSelected(MultiplayerSessionSummary summary)
    {
        if (!IsSessionJoinable(summary))
        {
            return;
        }

        _ = JoinByIdAsync(summary.SessionId, _pollingCts.Token);
    }

    private bool IsSessionJoinable(MultiplayerSessionSummary summary)
    {
        if (_isBusy || _sceneRouter.IsTransitionInProgress || _sessionService.HasActiveSession)
        {
            return false;
        }

        if (summary.HasPassword)
        {
            return false;
        }

        if (summary.IsLocked || summary.AvailableSlots <= 0)
        {
            return false;
        }

        return true;
    }

    private void UpdateActiveSessionBlock()
    {
        if (!_sessionService.HasActiveSession)
        {
            _view.SetActiveSessionText("Active session: none");
            return;
        }

        MultiplayerSessionSnapshot active = _sessionService.ActiveSession;
        string role = active.IsHost ? "Host" : "Client";
        string sessionCode = string.IsNullOrWhiteSpace(active.SessionCode) ? "-" : active.SessionCode;
        string sessionName = string.IsNullOrWhiteSpace(active.Name) ? "Unnamed lobby" : active.Name;

        _view.SetActiveSessionText(
            $"Active: {sessionName}\nID: {active.SessionId}\nCode: {sessionCode}\nPlayers: {active.PlayerCount}/{active.MaxPlayers}\nRole: {role}");
    }

    private bool TryBeginBusy()
    {
        if (_isBusy || _sceneRouter.IsTransitionInProgress || _sessionService.IsOperationInProgress)
        {
            return false;
        }

        _isBusy = true;
        ApplyButtonsState();
        return true;
    }

    private void EndBusy()
    {
        _isBusy = false;
        ApplyButtonsState();
    }

    private void ApplyButtonsState()
    {
        bool hasActiveSession = _sessionService.HasActiveSession;
        bool isHost = hasActiveSession && _sessionService.ActiveSession.IsHost;
        bool allowCreateAndJoin = !_isBusy && !hasActiveSession && !_sceneRouter.IsTransitionInProgress;
        bool allowRefresh = !_isBusy && !_sceneRouter.IsTransitionInProgress;

        _view.SetCreateLobbyInteractable(allowCreateAndJoin);
        _view.SetJoinByCodeInteractable(allowCreateAndJoin);
        _view.SetRefreshInteractable(allowRefresh);
        _view.SetStartMatchInteractable(!_isBusy && hasActiveSession && isHost && !_sceneRouter.IsTransitionInProgress);
        _view.SetBackToMenuInteractable(!_isBusy && !_sceneRouter.IsTransitionInProgress);
    }

    private static string FormatError<T>(string title, MultiplayerOperationResult<T> result)
    {
        return $"{title} ({result.ErrorCode}): {result.ErrorMessage}";
    }

    private bool TrySetMatchSetupFromSnapshot(CharacterDraftSnapshot snapshot)
    {
        var roster = new List<CharacterSpawnRequest>();

        for (int i = 0; i < snapshot.Players.Count; i++)
        {
            CharacterDraftPlayerSnapshot player = snapshot.Players[i];
            if (!player.IsConfirmed)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(player.CharacterId) || !IsNameValid(player.CharacterName))
            {
                return false;
            }

            roster.Add(new CharacterSpawnRequest(player.PlayerId, player.CharacterId, player.CharacterName));
        }

        if (roster.Count == 0)
        {
            return false;
        }

        _matchSetupContextService.SetRoster(
            roster,
            isOnlineMatch: true,
            matchSeed: snapshot.MatchSeed);
        return true;
    }

    private static bool IsNameValid(string value)
    {
        string name = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        if (name.Length < 3 || name.Length > 16)
        {
            return false;
        }

        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            if (char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == '_')
            {
                continue;
            }

            return false;
        }

        return true;
    }
}
