using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VContainer.Unity;

public sealed class CharacterSelectPresenter : IStartable, IDisposable
{
    private const string OfflinePlayerId = "offline_local";
    private const float MinDraftPollingIntervalSeconds = 3f;
    private const int MaxRateLimitBackoffStep = 3;
    private const int OperationRetryAttempts = 3;
    private const int RateLimitRetryBaseDelayMs = 1000;
    private static readonly TimeSpan StartValidationSnapshotMaxAge = TimeSpan.FromSeconds(2.5f);
    private static readonly TimeSpan RateLimitStatusCooldown = TimeSpan.FromSeconds(2f);

    private readonly CharacterSelectView _view;
    private readonly CharacterCatalog _characterCatalog;
    private readonly IMultiplayerSessionService _sessionService;
    private readonly ICharacterDraftService _draftService;
    private readonly IMatchSetupContextService _matchSetupContextService;
    private readonly IMatchNetworkService _matchNetworkService;
    private readonly ISessionSceneRouter _sceneRouter;
    private readonly MultiplayerConfig _multiplayerConfig;
    private readonly CancellationTokenSource _pollingCts = new();

    private CharacterDraftSnapshot _snapshot;
    private bool _hasSnapshot;
    private bool _isBusy;
    private bool _isOfflineMode;
    private bool _offlineConfirmed;
    private string _selectedCharacterId = string.Empty;
    private int _rateLimitBackoffStep;
    private DateTime _lastSnapshotUpdatedUtc = DateTime.MinValue;
    private DateTime _lastRateLimitStatusUtc = DateTime.MinValue;

    private enum SnapshotRefreshOutcome
    {
        Success,
        RateLimited,
        Failed
    }

    public CharacterSelectPresenter(
        CharacterSelectView view,
        CharacterCatalog characterCatalog,
        IMultiplayerSessionService sessionService,
        ICharacterDraftService draftService,
        IMatchSetupContextService matchSetupContextService,
        IMatchNetworkService matchNetworkService,
        ISessionSceneRouter sceneRouter,
        MultiplayerConfig multiplayerConfig)
    {
        _view = view;
        _characterCatalog = characterCatalog;
        _sessionService = sessionService;
        _draftService = draftService;
        _matchSetupContextService = matchSetupContextService;
        _matchNetworkService = matchNetworkService;
        _sceneRouter = sceneRouter;
        _multiplayerConfig = multiplayerConfig;
    }

    public void Start()
    {
        if (_view.ConfirmButton != null)
        {
            _view.ConfirmButton.onClick.AddListener(OnConfirmClicked);
        }

        if (_view.UnconfirmButton != null)
        {
            _view.UnconfirmButton.onClick.AddListener(OnUnconfirmClicked);
        }

        if (_view.RefreshButton != null)
        {
            _view.RefreshButton.onClick.AddListener(OnRefreshClicked);
        }

        if (_view.StartMatchButton != null)
        {
            _view.StartMatchButton.onClick.AddListener(OnStartMatchClicked);
        }

        if (_view.BackButton != null)
        {
            _view.BackButton.onClick.AddListener(OnBackClicked);
        }

        if (_view.NameInputField != null)
        {
            _view.NameInputField.onValueChanged.AddListener(OnNameChanged);
        }

        _view.SetListHeader("Choose your character");
        _view.SetStatus("Loading character draft...");

        _isOfflineMode = !_sessionService.HasActiveSession;

        if (_isOfflineMode)
        {
            EnterOfflineMode();
        }
        else
        {
            _ = RefreshOnlineSnapshotAsync(autoRoute: true, _pollingCts.Token);
            _ = PollLoopAsync(_pollingCts.Token);
        }
    }

    public void Dispose()
    {
        if (_view != null)
        {
            if (_view.ConfirmButton != null)
            {
                _view.ConfirmButton.onClick.RemoveListener(OnConfirmClicked);
            }

            if (_view.UnconfirmButton != null)
            {
                _view.UnconfirmButton.onClick.RemoveListener(OnUnconfirmClicked);
            }

            if (_view.RefreshButton != null)
            {
                _view.RefreshButton.onClick.RemoveListener(OnRefreshClicked);
            }

            if (_view.StartMatchButton != null)
            {
                _view.StartMatchButton.onClick.RemoveListener(OnStartMatchClicked);
            }

            if (_view.BackButton != null)
            {
                _view.BackButton.onClick.RemoveListener(OnBackClicked);
            }

            if (_view.NameInputField != null)
            {
                _view.NameInputField.onValueChanged.RemoveListener(OnNameChanged);
            }
        }

        if (!_pollingCts.IsCancellationRequested)
        {
            _pollingCts.Cancel();
        }

        _pollingCts.Dispose();
    }

    private async void OnConfirmClicked()
    {
        if (_isOfflineMode)
        {
            ConfirmOfflineSelection();
            return;
        }

        await ConfirmOnlineSelectionAsync(_pollingCts.Token);
    }

    private async void OnUnconfirmClicked()
    {
        if (_isOfflineMode)
        {
            _offlineConfirmed = false;
            RenderOfflineState();
            return;
        }

        await UnconfirmOnlineSelectionAsync(_pollingCts.Token);
    }

    private async void OnRefreshClicked()
    {
        if (_isOfflineMode)
        {
            RenderOfflineState();
            return;
        }

        await RefreshOnlineSnapshotAsync(autoRoute: false, _pollingCts.Token);
    }

    private async void OnStartMatchClicked()
    {
        if (_isOfflineMode)
        {
            await StartOfflineMatchAsync();
            return;
        }

        await StartOnlineMatchAsync(_pollingCts.Token);
    }

    private async void OnBackClicked()
    {
        if (_isOfflineMode)
        {
            _matchSetupContextService.Clear();
            await _sceneRouter.LoadMainMenuAsync();
            return;
        }

        await BackToLobbyAsync(_pollingCts.Token);
    }

    private void OnNameChanged(string _)
    {
        if (_isOfflineMode)
        {
            RenderOfflineState();
            return;
        }

        if (_hasSnapshot)
        {
            RenderSnapshot(_snapshot);
        }
    }

    private void EnterOfflineMode()
    {
        _offlineConfirmed = false;
        _selectedCharacterId = string.Empty;
        _view.SetStatus("Offline draft mode.");
        RenderOfflineState();
    }

    private async Task PollLoopAsync(CancellationToken cancellationToken)
    {
        float interval = _multiplayerConfig != null ? _multiplayerConfig.LobbyRefreshIntervalSeconds : 2f;
        interval = Math.Max(MinDraftPollingIntervalSeconds, interval);

        while (!cancellationToken.IsCancellationRequested)
        {
            int backoffMultiplier = 1 << Math.Min(_rateLimitBackoffStep, MaxRateLimitBackoffStep);
            int delayMs = (int)Math.Round(interval * 1000f * backoffMultiplier);

            try
            {
                await Task.Delay(delayMs, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (_isBusy || _sceneRouter.IsTransitionInProgress || _draftService.IsOperationInProgress)
            {
                continue;
            }

            SnapshotRefreshOutcome outcome =
                await RefreshOnlineSnapshotAsync(autoRoute: true, cancellationToken, fromPolling: true);

            if (outcome == SnapshotRefreshOutcome.Success)
            {
                _rateLimitBackoffStep = 0;
            }
            else if (outcome == SnapshotRefreshOutcome.RateLimited)
            {
                _rateLimitBackoffStep = Math.Min(MaxRateLimitBackoffStep, _rateLimitBackoffStep + 1);
            }
        }
    }

    private async Task ConfirmOnlineSelectionAsync(CancellationToken cancellationToken)
    {
        if (!TryBeginBusy())
        {
            return;
        }

        try
        {
            string characterId = _selectedCharacterId;
            if (string.IsNullOrWhiteSpace(characterId))
            {
                _view.SetStatus("Select a character card before confirming.");
                return;
            }

            string name = _view.GetCharacterNameInput();
            if (!IsNameValid(name))
            {
                _view.SetStatus("Name must be unique and 3..16 chars (letters, digits, spaces, '-' or '_').");
                return;
            }

            _view.SetStatus("Confirming selection...");
            MultiplayerOperationResult<CharacterDraftSnapshot> result =
                await ExecuteWithRateLimitRetryAsync(
                    () => _draftService.SubmitSelectionAsync(characterId, name, confirmed: true, cancellationToken),
                    "Confirm selection",
                    cancellationToken);

            if (!result.IsSuccess)
            {
                _view.SetStatus(FormatError("Confirm failed", result));
                return;
            }

            UpdateSnapshotState(result.Value);
            _selectedCharacterId = result.Value.SelectedCharacterId;
            RenderSnapshot(result.Value);
            _view.SetStatus("Character confirmed.");
        }
        catch (OperationCanceledException)
        {
            _view.SetStatus("Confirm cancelled.");
        }
        finally
        {
            EndBusy();
        }
    }

    private async Task UnconfirmOnlineSelectionAsync(CancellationToken cancellationToken)
    {
        if (!TryBeginBusy())
        {
            return;
        }

        try
        {
            _view.SetStatus("Removing confirmation...");
            MultiplayerOperationResult<CharacterDraftSnapshot> result =
                await ExecuteWithRateLimitRetryAsync(
                    () => _draftService.ClearSelectionAsync(cancellationToken),
                    "Clear selection",
                    cancellationToken);

            if (!result.IsSuccess)
            {
                _view.SetStatus(FormatError("Unconfirm failed", result));
                return;
            }

            UpdateSnapshotState(result.Value);
            _selectedCharacterId = result.Value.SelectedCharacterId;
            RenderSnapshot(result.Value);
            _view.SetStatus("Selection cleared.");
        }
        catch (OperationCanceledException)
        {
            _view.SetStatus("Unconfirm cancelled.");
        }
        finally
        {
            EndBusy();
        }
    }

    private async Task<SnapshotRefreshOutcome> RefreshOnlineSnapshotAsync(
        bool autoRoute,
        CancellationToken cancellationToken,
        bool fromPolling = false)
    {
        if (_sceneRouter.IsTransitionInProgress)
        {
            return SnapshotRefreshOutcome.Failed;
        }

        MultiplayerOperationResult<CharacterDraftSnapshot> result = await _draftService.GetSnapshotAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            if (IsRateLimitError(result.ErrorCode))
            {
                if (!fromPolling || ShouldShowRateLimitStatus())
                {
                    _view.SetStatus("Draft sync is temporarily rate-limited. Retrying...");
                }

                return SnapshotRefreshOutcome.RateLimited;
            }

            _view.SetStatus(FormatError("Draft refresh failed", result));
            return SnapshotRefreshOutcome.Failed;
        }

        UpdateSnapshotState(result.Value);

        if (autoRoute)
        {
            bool routed = await TryHandlePhaseRoutingAsync(result.Value);
            if (routed)
            {
                return SnapshotRefreshOutcome.Success;
            }
        }

        RenderSnapshot(result.Value);
        return SnapshotRefreshOutcome.Success;
    }

    private async Task<bool> TryHandlePhaseRoutingAsync(CharacterDraftSnapshot snapshot)
    {
        if (string.Equals(snapshot.Phase, MpsCharacterDraftService.PhaseInGame, StringComparison.OrdinalIgnoreCase))
        {
            if (!TrySetMatchSetupFromSnapshot(snapshot))
            {
                _view.SetStatus("Draft data is invalid. Returning to Lobby.");
                await _sceneRouter.LoadLobbyAsync();
                return true;
            }

            await _sceneRouter.LoadGameSceneAsync();
            return true;
        }

        if (string.Equals(snapshot.Phase, MpsCharacterDraftService.PhaseLobby, StringComparison.OrdinalIgnoreCase))
        {
            await _sceneRouter.LoadLobbyAsync();
            return true;
        }

        return false;
    }

    private async Task StartOnlineMatchAsync(CancellationToken cancellationToken)
    {
        if (!TryBeginBusy())
        {
            return;
        }

        try
        {
            MultiplayerOperationResult<CharacterDraftSnapshot> refreshResult =
                await ResolveSnapshotForStartAsync(cancellationToken);
            if (!refreshResult.IsSuccess)
            {
                _view.SetStatus(FormatError("Unable to validate draft", refreshResult));
                return;
            }

            CharacterDraftSnapshot snapshot = refreshResult.Value;
            UpdateSnapshotState(snapshot);
            RenderSnapshot(snapshot);

            if (!snapshot.IsHost)
            {
                _view.SetStatus("Only host can start match.");
                return;
            }

            if (!snapshot.AreAllConfirmed || !snapshot.HasUniqueCharacterPicks || !snapshot.HasUniqueNames)
            {
                _view.SetStatus("All players must confirm unique character picks and unique names.");
                return;
            }

            if (!TrySetMatchSetupFromSnapshot(snapshot))
            {
                _view.SetStatus("Failed to prepare match setup from current draft snapshot.");
                return;
            }

            MultiplayerOperationResult<bool> connectivityResult =
                await _matchNetworkService.WaitForMatchConnectivityAsync(cancellationToken);
            if (!connectivityResult.IsSuccess)
            {
                _view.SetStatus(FormatError("Network is not ready for match start", connectivityResult));
                return;
            }

            MultiplayerOperationResult<CharacterDraftSnapshot> phaseResult =
                await ExecuteWithRateLimitRetryAsync(
                    () => _draftService.SetPhaseAsync(MpsCharacterDraftService.PhaseInGame, cancellationToken),
                    "Start match",
                    cancellationToken);

            if (!phaseResult.IsSuccess)
            {
                _view.SetStatus(FormatError("Failed to set in_game phase", phaseResult));
                return;
            }

            _view.SetStatus("Starting match...");
            bool loaded = await _sceneRouter.LoadGameSceneAsync();
            if (!loaded)
            {
                _view.SetStatus("Failed to load GameScene.");
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

    private async Task BackToLobbyAsync(CancellationToken cancellationToken)
    {
        if (!TryBeginBusy())
        {
            return;
        }

        try
        {
            if (_hasSnapshot && _snapshot.IsHost)
            {
                await _draftService.SetPhaseAsync(MpsCharacterDraftService.PhaseLobby, cancellationToken);
            }

            await _draftService.ClearSelectionAsync(cancellationToken);
            _matchSetupContextService.Clear();
            bool loaded = await _sceneRouter.LoadLobbyAsync();
            if (!loaded)
            {
                _view.SetStatus("Failed to return to Lobby.");
            }
        }
        catch (OperationCanceledException)
        {
            _view.SetStatus("Back cancelled.");
        }
        finally
        {
            EndBusy();
        }
    }

    private void ConfirmOfflineSelection()
    {
        if (string.IsNullOrWhiteSpace(_selectedCharacterId))
        {
            _view.SetStatus("Select a character card before confirming.");
            return;
        }

        string name = _view.GetCharacterNameInput();
        if (!IsNameValid(name))
        {
            _view.SetStatus("Name must be 3..16 chars (letters, digits, spaces, '-' or '_').");
            return;
        }

        _offlineConfirmed = true;
        RenderOfflineState();
        _view.SetStatus("Offline character confirmed.");
    }

    private async Task StartOfflineMatchAsync()
    {
        if (!_offlineConfirmed)
        {
            _view.SetStatus("Confirm your character before starting offline match.");
            return;
        }

        string characterId = _selectedCharacterId;
        if (!_characterCatalog.TryGetById(characterId, out CharacterDefinition definition) || definition == null)
        {
            _view.SetStatus("Selected character is not available in catalog.");
            return;
        }

        string playerName = _view.GetCharacterNameInput().Trim();
        var roster = new List<CharacterSpawnRequest>
        {
            new(OfflinePlayerId, characterId, playerName)
        };

        _matchSetupContextService.SetRoster(
            roster,
            isOnlineMatch: false,
            matchSeed: DateTime.UtcNow.GetHashCode());
        bool loaded = await _sceneRouter.LoadGameSceneAsync();
        if (!loaded)
        {
            _view.SetStatus("Failed to load GameScene.");
        }
    }

    private void RenderOfflineState()
    {
        if (_characterCatalog == null || !_characterCatalog.HasAny)
        {
            _view.ClearRows();
            _view.SetListHeader("Character catalog is empty");
            _view.SetSelectionSummary("Offline: no characters configured");
            _view.SetStatus("CharacterCatalog is empty. Configure characters in Resources/CharacterCatalog.asset.");
            ApplyButtonsState();
            return;
        }

        _view.ClearRows();
        IReadOnlyList<CharacterDefinition> definitions = _characterCatalog.Characters;
        _view.SetListHeader($"Characters available: {definitions.Count}");

        for (int i = 0; i < definitions.Count; i++)
        {
            CharacterDefinition definition = definitions[i];
            if (definition == null || string.IsNullOrWhiteSpace(definition.Id))
            {
                continue;
            }

            CharacterSelectCardState state = CharacterSelectCardState.Available;
            if (string.Equals(definition.Id, _selectedCharacterId, StringComparison.OrdinalIgnoreCase))
            {
                state = _offlineConfirmed ? CharacterSelectCardState.Locked : CharacterSelectCardState.SelectedByYou;
            }
            else if (_offlineConfirmed)
            {
                state = CharacterSelectCardState.Locked;
            }

            CharacterSelectCardItemView row = _view.CreateRow();
            if (row != null)
            {
                row.Bind(definition, state, string.Empty, OnOfflineCardSelected);
            }
        }

        string selectedName = string.IsNullOrWhiteSpace(_selectedCharacterId) ? "none" : _selectedCharacterId;
        _view.SetSelectionSummary($"Mode: Offline\nSelected: {selectedName}\nConfirmed: {(_offlineConfirmed ? "Yes" : "No")}");
        ApplyButtonsState();
    }

    private void OnOfflineCardSelected(CharacterDefinition definition)
    {
        if (_offlineConfirmed || definition == null)
        {
            return;
        }

        _selectedCharacterId = definition.Id;
        RenderOfflineState();
    }

    private void RenderSnapshot(CharacterDraftSnapshot snapshot)
    {
        _view.ClearRows();

        if (_characterCatalog == null || !_characterCatalog.HasAny)
        {
            _view.SetListHeader("Character catalog is empty");
            _view.SetSelectionSummary("Online draft unavailable");
            _view.SetStatus("CharacterCatalog is empty. Configure characters in Resources/CharacterCatalog.asset.");
            ApplyButtonsState();
            return;
        }

        _view.SetListHeader($"Phase: {snapshot.Phase} | Players: {snapshot.Players.Count}");

        IReadOnlyList<CharacterDefinition> definitions = _characterCatalog.Characters;
        for (int i = 0; i < definitions.Count; i++)
        {
            CharacterDefinition definition = definitions[i];
            if (definition == null || string.IsNullOrWhiteSpace(definition.Id))
            {
                continue;
            }

            CharacterSelectCardState state = ResolveCardState(snapshot, definition, out string occupiedByName);
            CharacterSelectCardItemView row = _view.CreateRow();
            if (row != null)
            {
                row.Bind(definition, state, occupiedByName, OnOnlineCardSelected);
            }
        }

        string selected = string.IsNullOrWhiteSpace(snapshot.SelectedCharacterId) ? "none" : snapshot.SelectedCharacterId;
        string role = snapshot.IsHost ? "Host" : "Client";
        _view.SetSelectionSummary(
            $"Role: {role}\nSelected: {selected}\nName: {snapshot.SelectedCharacterName}\nConfirmed: {(snapshot.IsLocalConfirmed ? "Yes" : "No")}");

        if (snapshot.HasConflicts)
        {
            _view.SetStatus("Resolve duplicates: picks and names must be unique.");
        }

        if (!string.IsNullOrWhiteSpace(snapshot.SelectedCharacterName) &&
            string.IsNullOrWhiteSpace(_view.GetCharacterNameInput()))
        {
            _view.SetCharacterNameInput(snapshot.SelectedCharacterName);
        }

        ApplyButtonsState();
    }

    private CharacterSelectCardState ResolveCardState(
        CharacterDraftSnapshot snapshot,
        CharacterDefinition definition,
        out string occupiedByName)
    {
        occupiedByName = string.Empty;
        bool occupiedByLocal = false;
        bool occupiedByOther = false;

        for (int i = 0; i < snapshot.Players.Count; i++)
        {
            CharacterDraftPlayerSnapshot player = snapshot.Players[i];
            if (!string.Equals(player.CharacterId, definition.Id, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            bool isLocal = string.Equals(player.PlayerId, snapshot.LocalPlayerId, StringComparison.Ordinal);
            if (isLocal)
            {
                occupiedByLocal = true;
                continue;
            }

            occupiedByOther = true;
            occupiedByName = string.IsNullOrWhiteSpace(player.CharacterName) ? player.PlayerId : player.CharacterName;
            break;
        }

        if (occupiedByOther)
        {
            return CharacterSelectCardState.Taken;
        }

        if (occupiedByLocal)
        {
            return snapshot.IsLocalConfirmed ? CharacterSelectCardState.Locked : CharacterSelectCardState.SelectedByYou;
        }

        if (snapshot.IsLocalConfirmed)
        {
            return CharacterSelectCardState.Locked;
        }

        return CharacterSelectCardState.Available;
    }

    private void OnOnlineCardSelected(CharacterDefinition definition)
    {
        if (definition == null || _isBusy || !_hasSnapshot || _snapshot.IsLocalConfirmed)
        {
            return;
        }

        _selectedCharacterId = definition.Id;
        RenderSnapshot(_snapshot);
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

            if (!_characterCatalog.TryGetById(player.CharacterId, out CharacterDefinition definition) || definition == null)
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

    private bool TryBeginBusy()
    {
        if (_isBusy || _sceneRouter.IsTransitionInProgress || _draftService.IsOperationInProgress)
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
        if (_isOfflineMode)
        {
            bool hasSelection = !string.IsNullOrWhiteSpace(_selectedCharacterId);
            _view.SetConfirmInteractable(!_isBusy && !_offlineConfirmed && hasSelection);
            _view.SetUnconfirmInteractable(!_isBusy && _offlineConfirmed);
            _view.SetRefreshInteractable(!_isBusy);
            _view.SetStartMatchInteractable(!_isBusy && _offlineConfirmed && hasSelection);
            _view.SetBackInteractable(!_isBusy && !_sceneRouter.IsTransitionInProgress);
            return;
        }

        bool hasSelectionOnline = !string.IsNullOrWhiteSpace(_selectedCharacterId);
        bool localConfirmed = _hasSnapshot && _snapshot.IsLocalConfirmed;
        bool isHost = _hasSnapshot && _snapshot.IsHost;
        bool canStart = _hasSnapshot && isHost && _snapshot.AreAllConfirmed && !_snapshot.HasConflicts;

        _view.SetConfirmInteractable(!_isBusy && !localConfirmed && hasSelectionOnline);
        _view.SetUnconfirmInteractable(!_isBusy && localConfirmed);
        _view.SetRefreshInteractable(!_isBusy);
        _view.SetStartMatchInteractable(!_isBusy && canStart);
        _view.SetBackInteractable(!_isBusy && !_sceneRouter.IsTransitionInProgress);
    }

    private async Task<MultiplayerOperationResult<CharacterDraftSnapshot>> ResolveSnapshotForStartAsync(
        CancellationToken cancellationToken)
    {
        if (_hasSnapshot && DateTime.UtcNow - _lastSnapshotUpdatedUtc <= StartValidationSnapshotMaxAge)
        {
            return MultiplayerOperationResult<CharacterDraftSnapshot>.Success(_snapshot);
        }

        return await ExecuteWithRateLimitRetryAsync(
            () => _draftService.GetSnapshotAsync(cancellationToken),
            "Draft validation",
            cancellationToken);
    }

    private async Task<MultiplayerOperationResult<T>> ExecuteWithRateLimitRetryAsync<T>(
        Func<Task<MultiplayerOperationResult<T>>> operation,
        string operationName,
        CancellationToken cancellationToken)
    {
        MultiplayerOperationResult<T> lastResult = default;

        for (int attempt = 1; attempt <= OperationRetryAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lastResult = await operation();

            if (lastResult.IsSuccess || !IsRateLimitError(lastResult.ErrorCode) || attempt == OperationRetryAttempts)
            {
                return lastResult;
            }

            int delayMs = RateLimitRetryBaseDelayMs * attempt;
            _view.SetStatus($"{operationName}: service is busy, retrying...");
            await Task.Delay(delayMs, cancellationToken);
        }

        return lastResult;
    }

    private void UpdateSnapshotState(CharacterDraftSnapshot snapshot)
    {
        _snapshot = snapshot;
        _hasSnapshot = true;
        _lastSnapshotUpdatedUtc = DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(_selectedCharacterId))
        {
            _selectedCharacterId = snapshot.SelectedCharacterId;
        }
    }

    private bool ShouldShowRateLimitStatus()
    {
        DateTime now = DateTime.UtcNow;
        if (now - _lastRateLimitStatusUtc < RateLimitStatusCooldown)
        {
            return false;
        }

        _lastRateLimitStatusUtc = now;
        return true;
    }

    private static bool IsRateLimitError(string errorCode)
    {
        if (string.IsNullOrWhiteSpace(errorCode))
        {
            return false;
        }

        return errorCode.IndexOf("ratelimit", StringComparison.OrdinalIgnoreCase) >= 0 ||
               errorCode.EndsWith("_429", StringComparison.OrdinalIgnoreCase);
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

    private static string FormatError<T>(string title, MultiplayerOperationResult<T> result)
    {
        return $"{title} ({result.ErrorCode}): {result.ErrorMessage}";
    }
}
