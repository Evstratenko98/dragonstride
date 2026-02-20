using System;
using VContainer.Unity;

public class UIScreenPresenter : IStartable, IDisposable
{
    private readonly IEventBus _eventBus;
    private readonly UIScreenView _view;
    private readonly ISessionSceneRouter _sceneRouter;
    private IDisposable _gameStateSub;

    public UIScreenPresenter(IEventBus eventBus, UIScreenView view, ISessionSceneRouter sceneRouter)
    {
        _eventBus = eventBus;
        _view = view;
        _sceneRouter = sceneRouter;
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
                _ = _sceneRouter.LoadGameOverAsync();
                break;
            case GameState.Playing:
            case GameState.Loading:
                _view.ShowGameScreen();
                break;
        }
    }
}
