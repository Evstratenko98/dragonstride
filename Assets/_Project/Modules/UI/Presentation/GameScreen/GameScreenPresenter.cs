using System;
using VContainer.Unity;

public class GameScreenPresenter : IPostInitializable, IDisposable
{
    private const string FallbackValue = "—";
    private readonly GameScreenView _view;
    private readonly IEventBus _eventBus;
    private IDisposable _turnStateSubscription;
    private IDisposable _diceRolledSubscription;
    private IDisposable _characterMovedSubscription;
    private IDisposable _attackAvailabilitySubscription;
    private IDisposable _openCellAvailabilitySubscription;

    private ICellLayoutOccupant _currentActor;
    private TurnState _currentTurnState = TurnState.None;
    private int _stepsTotal;
    private int _stepsRemaining;

    public GameScreenPresenter(
        IEventBus eventBus,
        GameScreenView view)
    {
        _eventBus = eventBus;
        _view = view;
    }

    public void PostInitialize()
    {
        _turnStateSubscription = _eventBus.Subscribe<TurnPhaseChanged>(OnTurnStateChanged);
        _diceRolledSubscription = _eventBus.Subscribe<DiceRolled>(OnDiceRolled);
        _characterMovedSubscription = _eventBus.Subscribe<CharacterMoved>(OnCharacterMoved);
        _attackAvailabilitySubscription = _eventBus.Subscribe<AttackAvailabilityChanged>(OnAttackAvailabilityChanged);
        _openCellAvailabilitySubscription = _eventBus.Subscribe<OpenCellAvailabilityChanged>(OnOpenCellAvailabilityChanged);

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
        
        UpdatePlayerText();
        UpdateTurnStateText();
        UpdateStepsText();
        SetCharacterButtonInteractable(false);
        SetAttackButtonInteractable(false);
        SetOpenCellButtonInteractable(false);
    }

    public void Dispose()
    {
        _turnStateSubscription?.Dispose();
        _diceRolledSubscription?.Dispose();
        _characterMovedSubscription?.Dispose();
        _attackAvailabilitySubscription?.Dispose();
        _openCellAvailabilitySubscription?.Dispose();

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
    }

    private void OnTurnStateChanged(TurnPhaseChanged msg)
    {
        _currentActor = msg.Actor;

        _currentTurnState = msg.State;
        UpdatePlayerText();
        UpdateTurnStateText();
        SetCharacterButtonInteractable(_currentActor is CharacterInstance);

        if (msg.State == TurnState.End || msg.State == TurnState.None)
        {
            _stepsTotal = 0;
            _stepsRemaining = 0;
        }

        UpdateStepsText();
    }

    private void OnDiceRolled(DiceRolled msg)
    {
        if (_currentActor != null && msg.Actor != _currentActor)
        {
            return;
        }

        _currentActor = msg.Actor;
        _stepsTotal = msg.Steps;
        _stepsRemaining = msg.Steps;

        UpdatePlayerText();
        UpdateStepsText();
        SetCharacterButtonInteractable(_currentActor is CharacterInstance);
    }

    private void OnCharacterMoved(CharacterMoved msg)
    {
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
        if (_currentActor is not CharacterInstance)
        {
            return;
        }

        _eventBus.Publish(new CharacterScreenRequested());
    }

    private void OnOpenCellClicked()
    {
        _eventBus.Publish(new OpenCellRequested());
    }

    private void OnEndTurnClicked()
    {
        _eventBus.Publish(new EndTurnRequested());
    }

    private void OnAttackClicked()
    {
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

    private void OnAttackAvailabilityChanged(AttackAvailabilityChanged msg)
    {
        SetAttackButtonInteractable(msg.IsAvailable);
    }

    private void OnOpenCellAvailabilityChanged(OpenCellAvailabilityChanged msg)
    {
        SetOpenCellButtonInteractable(msg.IsAvailable);
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
}
