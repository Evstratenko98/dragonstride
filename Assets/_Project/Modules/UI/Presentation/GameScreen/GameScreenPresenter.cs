using System;
using VContainer.Unity;

public class GameScreenPresenter : IPostInitializable, IDisposable
{
    private const string FallbackValue = "—";
    private const string WaitingForHostStateLabel = "Waiting for host state...";
    private readonly GameScreenView _view;
    private readonly IEventBus _eventBus;
    private readonly IMatchSetupContextService _matchSetupContextService;
    private readonly IMatchNetworkService _matchNetworkService;
    private IDisposable _turnStateSubscription;
    private IDisposable _diceRolledSubscription;
    private IDisposable _characterMovedSubscription;
    private IDisposable _attackAvailabilitySubscription;
    private IDisposable _openCellAvailabilitySubscription;
    private IDisposable _snapshotAppliedSubscription;

    private ICellLayoutOccupant _currentActor;
    private TurnState _currentTurnState = TurnState.None;
    private int _stepsTotal;
    private int _stepsRemaining;
    private bool _isWaitingForInitialSnapshot;

    public GameScreenPresenter(
        IEventBus eventBus,
        GameScreenView view,
        IMatchSetupContextService matchSetupContextService,
        IMatchNetworkService matchNetworkService)
    {
        _eventBus = eventBus;
        _view = view;
        _matchSetupContextService = matchSetupContextService;
        _matchNetworkService = matchNetworkService;
    }

    public void PostInitialize()
    {
        _turnStateSubscription = _eventBus.Subscribe<TurnPhaseChanged>(OnTurnStateChanged);
        _diceRolledSubscription = _eventBus.Subscribe<DiceRolled>(OnDiceRolled);
        _characterMovedSubscription = _eventBus.Subscribe<CharacterMoved>(OnCharacterMoved);
        _attackAvailabilitySubscription = _eventBus.Subscribe<AttackAvailabilityChanged>(OnAttackAvailabilityChanged);
        _openCellAvailabilitySubscription = _eventBus.Subscribe<OpenCellAvailabilityChanged>(OnOpenCellAvailabilityChanged);
        _snapshotAppliedSubscription = _eventBus.Subscribe<MatchSnapshotApplied>(OnMatchSnapshotApplied);

        if (_view.CharacaterButton != null)
        {
            _view.CharacaterButton.onClick.AddListener(OnCharacterButtonClicked);
        }

        if (_view.OpenCellButton != null)
        {
            _view.OpenCellButton.onClick.AddListener(OnOpenCellClicked);
        }

        if (_view.EndTurnButton != null)
        {
            _view.EndTurnButton.onClick.AddListener(OnEndTurnClicked);
        }

        if (_view.AttackButton != null)
        {
            _view.AttackButton.onClick.AddListener(OnAttackClicked);
        }

        if (_view.FollowPlayerToggle != null)
        {
            _view.FollowPlayerToggle.onValueChanged.AddListener(OnFollowToggleChanged);
            OnFollowToggleChanged(_view.FollowPlayerToggle.isOn);
        }

        if (_view.FogToggle != null)
        {
            _view.FogToggle.onValueChanged.AddListener(OnFogToggleChanged);
            OnFogToggleChanged(_view.FogToggle.isOn);
        }

        if (_view.HiddenCellsToggle != null)
        {
            _view.HiddenCellsToggle.onValueChanged.AddListener(OnHiddenCellsToggleChanged);
            OnHiddenCellsToggleChanged(_view.HiddenCellsToggle.isOn);
        }
        
        UpdatePlayerText();
        UpdateTurnStateText();
        UpdateStepsText();
        SetCharacterButtonInteractable(false);
        SetEndTurnButtonInteractable(false);
        SetAttackButtonInteractable(false);
        SetOpenCellButtonInteractable(false);
        ApplyInitialSnapshotWaitingState();
    }

    public void Dispose()
    {
        _turnStateSubscription?.Dispose();
        _diceRolledSubscription?.Dispose();
        _characterMovedSubscription?.Dispose();
        _attackAvailabilitySubscription?.Dispose();
        _openCellAvailabilitySubscription?.Dispose();
        _snapshotAppliedSubscription?.Dispose();

        if (_view.CharacaterButton != null)
        {
            _view.CharacaterButton.onClick.RemoveListener(OnCharacterButtonClicked);
        }

        if (_view.OpenCellButton != null)
        {
            _view.OpenCellButton.onClick.RemoveListener(OnOpenCellClicked);
        }

        if (_view.EndTurnButton != null)
        {
            _view.EndTurnButton.onClick.RemoveListener(OnEndTurnClicked);
        }

        if (_view.AttackButton != null)
        {
            _view.AttackButton.onClick.RemoveListener(OnAttackClicked);
        }

        if (_view.FollowPlayerToggle != null)
        {
            _view.FollowPlayerToggle.onValueChanged.RemoveListener(OnFollowToggleChanged);
        }

        if (_view.FogToggle != null)
        {
            _view.FogToggle.onValueChanged.RemoveListener(OnFogToggleChanged);
        }

        if (_view.HiddenCellsToggle != null)
        {
            _view.HiddenCellsToggle.onValueChanged.RemoveListener(OnHiddenCellsToggleChanged);
        }
    }

    private void OnTurnStateChanged(TurnPhaseChanged msg)
    {
        _currentActor = msg.Actor;

        _currentTurnState = msg.State;
        if (_isWaitingForInitialSnapshot)
        {
            return;
        }

        UpdatePlayerText();
        UpdateTurnStateText();
        bool isCharacterTurn = _currentActor is CharacterInstance;
        SetCharacterButtonInteractable(isCharacterTurn);
        SetEndTurnButtonInteractable(isCharacterTurn && CanUseTurnActions());

        if (msg.State == TurnState.End || msg.State == TurnState.None)
        {
            _stepsTotal = 0;
            _stepsRemaining = 0;
        }

        UpdateStepsText();
    }

    private void OnDiceRolled(DiceRolled msg)
    {
        if (_isWaitingForInitialSnapshot)
        {
            return;
        }

        if (_currentActor != null && msg.Actor != _currentActor)
        {
            return;
        }

        _currentActor = msg.Actor;
        _stepsTotal = msg.Steps;
        _stepsRemaining = msg.Steps;

        UpdatePlayerText();
        UpdateStepsText();
        bool isCharacterTurn = _currentActor is CharacterInstance;
        SetCharacterButtonInteractable(isCharacterTurn);
        SetEndTurnButtonInteractable(isCharacterTurn && CanUseTurnActions());
    }

    private void OnCharacterMoved(CharacterMoved msg)
    {
        if (_isWaitingForInitialSnapshot)
        {
            return;
        }

        if (_currentActor == null || msg.Actor != _currentActor)
        {
            return;
        }

        if (_stepsRemaining <= 0)
        {
            return;
        }

        _stepsRemaining--;
        UpdateStepsText();
    }

    private void UpdatePlayerText()
    {
        var playerName = _currentActor?.Entity?.Name ?? FallbackValue;
        _view.SetCurrentPlayer(playerName);
    }

    private void UpdateTurnStateText()
    {
        var stateLabel = _currentTurnState == TurnState.None
            ? FallbackValue
            : _currentTurnState.ToString();
        _view.SetTurnState(stateLabel);
    }

    private void UpdateStepsText()
    {
        _view.SetSteps(_stepsRemaining, _stepsTotal);
    }

    private void OnCharacterButtonClicked()
    {
        if (_isWaitingForInitialSnapshot)
        {
            return;
        }

        if (_currentActor is not CharacterInstance)
        {
            return;
        }

        _eventBus.Publish(new CharacterScreenRequested());
    }

    private void OnOpenCellClicked()
    {
        if (_isWaitingForInitialSnapshot)
        {
            return;
        }

        _eventBus.Publish(new OpenCellRequested());
    }

    private void OnEndTurnClicked()
    {
        if (_isWaitingForInitialSnapshot)
        {
            return;
        }

        _eventBus.Publish(new EndTurnRequested());
    }

    private void OnAttackClicked()
    {
        if (_isWaitingForInitialSnapshot)
        {
            return;
        }

        _eventBus.Publish(new AttackRequested());
    }

    private void OnFollowToggleChanged(bool isEnabled)
    {
        _eventBus.Publish(new CameraFollowToggled(isEnabled));
    }

    private void OnFogToggleChanged(bool isEnabled)
    {
        _eventBus.Publish(new FogOfWarToggled(isEnabled));
    }

    private void OnHiddenCellsToggleChanged(bool isEnabled)
    {
        _eventBus.Publish(new HiddenCellsToggled(isEnabled));
    }

    private void OnAttackAvailabilityChanged(AttackAvailabilityChanged msg)
    {
        SetAttackButtonInteractable(!_isWaitingForInitialSnapshot && msg.IsAvailable);
    }

    private void OnOpenCellAvailabilityChanged(OpenCellAvailabilityChanged msg)
    {
        SetOpenCellButtonInteractable(!_isWaitingForInitialSnapshot && msg.IsAvailable);
    }

    private void OnMatchSnapshotApplied(MatchSnapshotApplied msg)
    {
        if (!_isWaitingForInitialSnapshot || !msg.IsInitial)
        {
            return;
        }

        _isWaitingForInitialSnapshot = false;
        UpdatePlayerText();
        UpdateTurnStateText();
        UpdateStepsText();

        bool isCharacterTurn = _currentActor is CharacterInstance;
        SetCharacterButtonInteractable(isCharacterTurn);
        SetEndTurnButtonInteractable(isCharacterTurn && CanUseTurnActions());
        SetAttackButtonInteractable(false);
        SetOpenCellButtonInteractable(false);
    }

    private void SetAttackButtonInteractable(bool isInteractable)
    {
        if (_view.AttackButton == null)
        {
            return;
        }

        _view.AttackButton.interactable = isInteractable;
    }

    private void SetOpenCellButtonInteractable(bool isInteractable)
    {
        if (_view.OpenCellButton == null)
        {
            return;
        }

        _view.OpenCellButton.interactable = isInteractable;
    }

    private void SetCharacterButtonInteractable(bool isInteractable)
    {
        if (_view.CharacaterButton == null)
        {
            return;
        }

        _view.CharacaterButton.interactable = isInteractable;
    }

    private void SetEndTurnButtonInteractable(bool isInteractable)
    {
        if (_view.EndTurnButton == null)
        {
            return;
        }

        _view.EndTurnButton.interactable = isInteractable;
    }

    private bool CanUseTurnActions()
    {
        return _currentTurnState == TurnState.ActionSelection ||
               _currentTurnState == TurnState.Movement ||
               _currentTurnState == TurnState.Attack ||
               _currentTurnState == TurnState.OpenCell ||
               _currentTurnState == TurnState.Trade;
    }

    private void ApplyInitialSnapshotWaitingState()
    {
        bool isOnlineMatch = _matchSetupContextService != null && _matchSetupContextService.IsOnlineMatch;
        bool isHost = _matchNetworkService != null && _matchNetworkService.IsHost;
        _isWaitingForInitialSnapshot = isOnlineMatch && !isHost;

        if (!_isWaitingForInitialSnapshot)
        {
            return;
        }

        _view.SetCurrentPlayer(FallbackValue);
        _view.SetTurnState(WaitingForHostStateLabel);
        _view.SetSteps(0, 0);
        SetCharacterButtonInteractable(false);
        SetEndTurnButtonInteractable(false);
        SetAttackButtonInteractable(false);
        SetOpenCellButtonInteractable(false);
    }
}
