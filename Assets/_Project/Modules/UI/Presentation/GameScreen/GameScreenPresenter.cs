using System;
using System.Linq;
using VContainer.Unity;

public class GameScreenPresenter : IPostInitializable, IDisposable
{
    private const string FallbackValue = "—";
    private readonly GameScreenView _view;
    private readonly IEventBus _eventBus;
    private readonly TurnFlow _turnFlow;
    private readonly CharacterRoster _characterRoster;
    private IDisposable _turnStateSubscription;
    private IDisposable _diceRolledSubscription;
    private IDisposable _characterMovedSubscription;

    private CharacterInstance _currentCharacter;
    private TurnState _currentTurnState = TurnState.None;
    private int _stepsTotal;
    private int _stepsRemaining;

    public GameScreenPresenter(
        IEventBus eventBus,
        TurnFlow turnFlow,
        CharacterRoster characterRoster,
        GameScreenView view)
    {
        _eventBus = eventBus;
        _turnFlow = turnFlow;
        _characterRoster = characterRoster;
        _view = view;
    }

    public void PostInitialize()
    {
        _turnStateSubscription = _eventBus.Subscribe<TurnPhaseChanged>(OnTurnStateChanged);
        _diceRolledSubscription = _eventBus.Subscribe<DiceRolled>(OnDiceRolled);
        _characterMovedSubscription = _eventBus.Subscribe<CharacterMoved>(OnCharacterMoved);

        if (_view.CharacaterButton != null)
        {
            _view.CharacaterButton.onClick.AddListener(OnCharacterButtonClicked);
        }

        if (_view.InteractCellButton != null)
        {
            _view.InteractCellButton.onClick.AddListener(OnInteractCellClicked);
        }

        if (_view.EndTurnButton != null)
        {
            _view.EndTurnButton.onClick.AddListener(OnEndTurnClicked);
        }

        if (_view.FollowPlayerToggle != null)
        {
            _view.FollowPlayerToggle.onValueChanged.AddListener(OnFollowToggleChanged);
            OnFollowToggleChanged(_view.FollowPlayerToggle.isOn);
        }
        
        UpdatePlayerText();
        UpdateTurnStateText();
        UpdateStepsText();
        UpdateAttackButtonState();
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

        if (_view.InteractCellButton != null)
        {
            _view.InteractCellButton.onClick.RemoveListener(OnInteractCellClicked);
        }

        if (_view.EndTurnButton != null)
        {
            _view.EndTurnButton.onClick.RemoveListener(OnEndTurnClicked);
        }

        if (_view.FollowPlayerToggle != null)
        {
            _view.FollowPlayerToggle.onValueChanged.RemoveListener(OnFollowToggleChanged);
        }
    }

    private void OnTurnStateChanged(TurnPhaseChanged msg)
    {
        if (msg.Character != null)
        {
            _currentCharacter = msg.Character;
        }

        _currentTurnState = msg.State;
        UpdatePlayerText();
        UpdateTurnStateText();

        if (msg.State == TurnState.End || msg.State == TurnState.None)
        {
            _stepsTotal = 0;
            _stepsRemaining = 0;
        }

        if (msg.State == TurnState.InteractionCell)
        {
            _stepsRemaining = 0;
        }

        UpdateStepsText();
        UpdateAttackButtonState();
    }

    private void OnDiceRolled(DiceRolled msg)
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
        UpdateAttackButtonState();
    }

    private void OnCharacterMoved(CharacterMoved msg)
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
        UpdateAttackButtonState();
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
        _eventBus.Publish(new CharacterScreenRequested());
    }

    private void OnInteractCellClicked()
    {
        _turnFlow.TryInteractWithCell();
    }

    private void OnEndTurnClicked()
    {
        _eventBus.Publish(new EndTurnRequested());
    }

    private void OnFollowToggleChanged(bool isEnabled)
    {
        _eventBus.Publish(new CameraFollowToggled(isEnabled));
    }

    private void UpdateAttackButtonState()
    {
        if (_view.AttackButton == null)
        {
            return;
        }

        if (_currentCharacter?.Model?.CurrentCell == null)
        {
            _view.AttackButton.interactable = false;
            return;
        }

        var currentCell = _currentCharacter.Model.CurrentCell;
        if (currentCell.Type == CellType.Start)
        {
            _view.AttackButton.interactable = false;
            return;
        }

        bool hasTarget = _characterRoster.AllCharacters
            .Any(character =>
                character != null &&
                character != _currentCharacter &&
                character.Model?.CurrentCell == currentCell);

        _view.AttackButton.interactable = hasTarget;
    }
}
