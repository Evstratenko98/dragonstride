using System;
using VContainer.Unity;

public class GameScreenController : IStartable, IDisposable
{
    private const string DefaultDiceLabel = "бросить кубик";

    private readonly IEventBus _eventBus;
    private readonly GameScreenView _view;
    private IDisposable _turnStateSubscription;
    private IDisposable _diceRolledSubscription;
    private CharacterInstance _activeCharacter;

    public GameScreenController(IEventBus eventBus, GameScreenView view)
    {
        _eventBus = eventBus;
        _view = view;
    }

    public void Start()
    {
        _view.DiceButton.onClick.AddListener(OnDiceButtonClicked);
        _turnStateSubscription = _eventBus.Subscribe<TurnStateChangedMessage>(OnTurnStateChanged);
        _diceRolledSubscription = _eventBus.Subscribe<DiceRolledMessage>(OnDiceRolled);
        ResetDiceButton();
    }

    public void Dispose()
    {
        _turnStateSubscription?.Dispose();
        _diceRolledSubscription?.Dispose();
        _view.DiceButton.onClick.RemoveListener(OnDiceButtonClicked);
    }

    private void OnDiceButtonClicked()
    {
        _eventBus.Publish(new DiceButtonPressedMessage());
    }

    private void OnTurnStateChanged(TurnStateChangedMessage msg)
    {
        _activeCharacter = msg.Character;

        if (msg.State == TurnState.Start)
        {
            ResetDiceButton();
            return;
        }

        _view.DiceButton.interactable = false;
    }

    private void OnDiceRolled(DiceRolledMessage msg)
    {
        if (_activeCharacter != null && msg.Character != _activeCharacter)
            return;

        _view.DiceButtonLabel.text = msg.Steps.ToString();
        _view.DiceButton.interactable = false;
    }

    private void ResetDiceButton()
    {
        _view.DiceButtonLabel.text = DefaultDiceLabel;
        _view.DiceButton.interactable = true;
    }
}
