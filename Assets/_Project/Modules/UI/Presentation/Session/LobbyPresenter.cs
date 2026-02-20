using System;
using System.Threading.Tasks;
using VContainer.Unity;

public sealed class LobbyPresenter : IStartable, IDisposable
{
    private readonly LobbyView _view;
    private readonly ISessionSceneRouter _sceneRouter;
    private bool _isNavigating;

    public LobbyPresenter(LobbyView view, ISessionSceneRouter sceneRouter)
    {
        _view = view;
        _sceneRouter = sceneRouter;
    }

    public void Start()
    {
        _view.SetLobbyPlaceholderText("Lobby browser placeholder.\nReal MPS session listing will be added in Phase 2.");
        _view.SetStatus("Phase 1 mode: local placeholder lobby.");

        SetButtonInteractable(_view.CreateLobbyButton, false);
        SetButtonInteractable(_view.RefreshButton, false);
        SetButtonInteractable(_view.JoinByCodeButton, false);
        SetButtonInteractable(_view.StartMatchButton, true);
        SetButtonInteractable(_view.BackToMenuButton, true);

        if (_view.StartMatchButton != null)
        {
            _view.StartMatchButton.onClick.AddListener(OnStartMatchClicked);
        }

        if (_view.BackToMenuButton != null)
        {
            _view.BackToMenuButton.onClick.AddListener(OnBackToMenuClicked);
        }
    }

    public void Dispose()
    {
        if (_view.StartMatchButton != null)
        {
            _view.StartMatchButton.onClick.RemoveListener(OnStartMatchClicked);
        }

        if (_view.BackToMenuButton != null)
        {
            _view.BackToMenuButton.onClick.RemoveListener(OnBackToMenuClicked);
        }
    }

    private async void OnStartMatchClicked()
    {
        _view.SetStatus("Starting local placeholder match...");
        await TryNavigateAsync(_sceneRouter.LoadGameSceneAsync);
    }

    private async void OnBackToMenuClicked()
    {
        await TryNavigateAsync(_sceneRouter.LoadMainMenuAsync);
    }

    private async Task TryNavigateAsync(Func<Task<bool>> navigateAction)
    {
        if (_isNavigating || _sceneRouter.IsTransitionInProgress)
        {
            return;
        }

        _isNavigating = true;
        SetButtonInteractable(_view.StartMatchButton, false);
        SetButtonInteractable(_view.BackToMenuButton, false);
        try
        {
            bool success = await navigateAction();
            if (!success)
            {
                _view.SetStatus("Navigation failed. Check logs and try again.");
            }
        }
        finally
        {
            _isNavigating = false;
            if (_view != null)
            {
                SetButtonInteractable(_view.StartMatchButton, true);
                SetButtonInteractable(_view.BackToMenuButton, true);
            }
        }
    }

    private static void SetButtonInteractable(UnityEngine.UI.Button button, bool isInteractable)
    {
        if (button != null)
        {
            button.interactable = isInteractable;
        }
    }
}
