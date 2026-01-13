using System;

public class FinishGameController
{
    private readonly EventBus _eventBus;
    private readonly FinishGameView _view;
    private IDisposable _gameStateSub;
    
    public FinishGameController(EventBus eventBus, FinishGameView view)
    {
        _eventBus = eventBus;
        _view = view;
    }
    
    public void Start()
    {
        _view.PlayAgainButton.gameObject.SetActive(false); // скрыта по умолчанию
        _view.PlayAgainButton.onClick.AddListener(OnFinishClicked);

        _gameStateSub = _eventBus.Subscribe<GameStateChangedMessage>(OnStateGame);
    }

    public void Dispose()
    {
        _view.PlayAgainButton.onClick.RemoveListener(OnFinishClicked);
        _gameStateSub?.Dispose();
    }

    private void OnStateGame(GameStateChangedMessage msg)
    {
        if(msg.State == GameState.Finished)
        {
            _view.PlayAgainButton.gameObject.SetActive(true);
        }
    }

    private void OnFinishClicked()
    {
        _view.PlayAgainButton.gameObject.SetActive(false);
        // Отправляем событие — кнопка нажата
        _eventBus.Publish(new ResetButtonPressedMessage());
    }
}
