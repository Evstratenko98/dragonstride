using System;
using System.Threading.Tasks;
using VContainer.Unity;

public sealed class GameOverScenePresenter : IStartable, IDisposable
{
    private readonly GameOverSceneView _view;
    private readonly ISessionSceneRouter _sceneRouter;
    private readonly IMultiplayerSessionService _sessionService;
    private readonly IMatchNetworkService _matchNetworkService;
    private bool _isNavigating;

    public GameOverScenePresenter(
        GameOverSceneView view,
        ISessionSceneRouter sceneRouter,
        IMultiplayerSessionService sessionService,
        IMatchNetworkService matchNetworkService)
    {
        _view = view;
        _sceneRouter = sceneRouter;
        _sessionService = sessionService;
        _matchNetworkService = matchNetworkService;
    }

    public void Start()
    {
        _view.SetResultText("Match finished.\nDetailed results will be added in Phase 2+.");

        if (_view.ReturnToMainMenuButton != null)
        {
            _view.ReturnToMainMenuButton.onClick.AddListener(OnReturnToMainMenuClicked);
            _view.ReturnToMainMenuButton.interactable = true;
        }
    }

    public void Dispose()
    {
        if (_view.ReturnToMainMenuButton != null)
        {
            _view.ReturnToMainMenuButton.onClick.RemoveListener(OnReturnToMainMenuClicked);
        }
    }

    private async void OnReturnToMainMenuClicked()
    {
        if (_isNavigating || _sceneRouter.IsTransitionInProgress)
        {
            return;
        }

        _isNavigating = true;
        if (_view.ReturnToMainMenuButton != null)
        {
            _view.ReturnToMainMenuButton.interactable = false;
        }

        try
        {
            await _matchNetworkService.ShutdownAsync();
            await _sessionService.LeaveActiveSessionAsync();
            await _sceneRouter.LoadMainMenuAsync();
        }
        finally
        {
            _isNavigating = false;
            if (_view != null && _view.ReturnToMainMenuButton != null)
            {
                _view.ReturnToMainMenuButton.interactable = true;
            }
        }
    }
}
