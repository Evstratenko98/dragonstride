using System;
using VContainer.Unity;

public class GameScreenController : IPostInitializable, IDisposable
{
    private const string FallbackValue = "—";
    private readonly GameScreenView _view;
    private readonly IEventBus _eventBus;
    private IDisposable _turnStateSubscription;
    private IDisposable _diceRolledSubscription;
    private IDisposable _characterMovedSubscription;

    private CharacterInstance _currentCharacter;
    private TurnState _currentTurnState = TurnState.None;
    private int _stepsTotal;
    private int _stepsRemaining;

    public GameScreenController(IEventBus eventBus, GameScreenView view)
    {
        _eventBus = eventBus;
        _view = view;
    }

    public void PostInitialize()
    {
        _turnStateSubscription = _eventBus.Subscribe<TurnStateChangedMessage>(OnTurnStateChanged);
        _diceRolledSubscription = _eventBus.Subscribe<DiceRolledMessage>(OnDiceRolled);
        _characterMovedSubscription = _eventBus.Subscribe<CharacterMovedMessage>(OnCharacterMoved);

        if (_view.CharacaterButton != null)
        {
            _view.CharacaterButton.onClick.AddListener(OnCharacterButtonClicked);
        }

        if (_view.FollowPlayerToggle != null)
        {
            _view.FollowPlayerToggle.onValueChanged.AddListener(OnFollowToggleChanged);
            OnFollowToggleChanged(_view.FollowPlayerToggle.isOn);
        }
        
        UpdatePlayerText();
        UpdateTurnStateText();
        UpdateStepsText();
    }

    public void Dispose()
    {
        _turnStateSubscription?.Dispose();
        _diceRolledSubscription?.Dispose();
        _characterMovedSubscription?.Dispose();

        if (_view.CharacaterButton != null)
        {
            _view.CharacaterButton.onClick.RemoveListener(OnCharacterButtonClicked);
        }

        if (_view.FollowPlayerToggle != null)
        {
            _view.FollowPlayerToggle.onValueChanged.RemoveListener(OnFollowToggleChanged);
        }
    }

    private void OnTurnStateChanged(TurnStateChangedMessage msg)
    {
        if (msg.Character != null)
        {
            _currentCharacter = msg.Character;
        }

        _currentTurnState = msg.State;
        UpdatePlayerText();
        UpdateTurnStateText();

        if (msg.State == TurnState.Start || msg.State == TurnState.End || msg.State == TurnState.None)
        {
            _stepsTotal = 0;
            _stepsRemaining = 0;
        }

        if (msg.State == TurnState.InteractionCells || msg.State == TurnState.InteractionPlayers)
        {
            _stepsRemaining = 0;
        }

        UpdateStepsText();
    }

    private void OnDiceRolled(DiceRolledMessage msg)
    {
        if (_currentCharacter != null && msg.Character != _currentCharacter)
        {
            return;
        }

        _currentCharacter = msg.Character;
        _stepsTotal = msg.Steps;
        _stepsRemaining = msg.Steps;

        UpdatePlayerText();
        UpdateStepsText();
    }

    private void OnCharacterMoved(CharacterMovedMessage msg)
    {
        if (_currentCharacter == null || msg.Character != _currentCharacter)
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
        var playerName = _currentCharacter != null ? _currentCharacter.Name : FallbackValue;
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
        _eventBus.Publish(new CharacterButtonPressedMessage());
    }

    private void OnFollowToggleChanged(bool isEnabled)
    {
        _eventBus.Publish(new CameraFollowToggled(isEnabled));
    }
}
