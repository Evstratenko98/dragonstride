using System;
using UnityEngine.SceneManagement;
using VContainer.Unity;

public class UIScreenPresenter : IStartable, IDisposable
{
    private readonly IEventBus _eventBus;
    private readonly UIScreenView _view;
    private IDisposable _gameStateSub;

    public UIScreenPresenter(IEventBus eventBus, UIScreenView view)
    {
        _eventBus = eventBus;
        _view = view;
    }

    public void Start()
    {
        _view.ShowGameScreen();
        _gameStateSub = _eventBus.Subscribe<GameStateChanged>(OnStateGame);
    }

    public void Dispose()
    {
        _gameStateSub?.Dispose();
    }

    private void OnStateGame(GameStateChanged msg)
    {
        switch (msg.State)
        {
            case GameState.Finished:
                SceneManager.LoadScene(SessionSceneNames.GameOver);
                break;
            case GameState.Playing:
            case GameState.Loading:
                _view.ShowGameScreen();
                break;
        }
    }
}
