using System;
using System.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;

public sealed class MainMenuPresenter : IStartable, IDisposable
{
    private readonly MainMenuView _view;
    private readonly ISessionSceneRouter _sceneRouter;
    private readonly MultiplayerConfig _multiplayerConfig;
    private readonly IMatchSetupContextService _matchSetupContextService;
    private bool _isNavigating;

    public MainMenuPresenter(
        MainMenuView view,
        ISessionSceneRouter sceneRouter,
        MultiplayerConfig multiplayerConfig,
        IMatchSetupContextService matchSetupContextService)
    {
        _view = view;
        _sceneRouter = sceneRouter;
        _multiplayerConfig = multiplayerConfig;
        _matchSetupContextService = matchSetupContextService;
    }

    public void Start()
    {
        if (_view.PlayOnlineButton != null)
        {
            _view.PlayOnlineButton.onClick.AddListener(OnPlayOnlineClicked);
        }

        if (_view.OfflineTrainingButton != null)
        {
            _view.OfflineTrainingButton.onClick.AddListener(OnOfflineTrainingClicked);
        }

        if (_view.ReconnectButton != null)
        {
            _view.ReconnectButton.onClick.AddListener(OnReconnectClicked);
        }

        if (_view.SettingsButton != null)
        {
            _view.SettingsButton.onClick.AddListener(OnSettingsClicked);
        }

        if (_view.ExitButton != null)
        {
            _view.ExitButton.onClick.AddListener(OnExitClicked);
        }

        ApplyDefaultState();
    }

    public void Dispose()
    {
        if (_view.PlayOnlineButton != null)
        {
            _view.PlayOnlineButton.onClick.RemoveListener(OnPlayOnlineClicked);
        }

        if (_view.OfflineTrainingButton != null)
        {
            _view.OfflineTrainingButton.onClick.RemoveListener(OnOfflineTrainingClicked);
        }

        if (_view.ReconnectButton != null)
        {
            _view.ReconnectButton.onClick.RemoveListener(OnReconnectClicked);
        }

        if (_view.SettingsButton != null)
        {
            _view.SettingsButton.onClick.RemoveListener(OnSettingsClicked);
        }

        if (_view.ExitButton != null)
        {
            _view.ExitButton.onClick.RemoveListener(OnExitClicked);
        }
    }

    private void ApplyDefaultState()
    {
        bool multiplayerEnabled = _multiplayerConfig != null && _multiplayerConfig.EnableMultiplayer;
        _view.SetPlayOnlineInteractable(multiplayerEnabled);
        _view.SetOfflineTrainingInteractable(true);
        _view.SetReconnectInteractable(false);
        _view.SetSettingsInteractable(false);
        _view.SetExitInteractable(true);

        _view.SetStatus(multiplayerEnabled
            ? "Online mode is available. Character draft scene is enabled in Phase 3.1."
            : "Multiplayer disabled in config. Offline training is available.");
    }

    private async void OnPlayOnlineClicked()
    {
        _matchSetupContextService?.Clear();
        await TryNavigateAsync(_sceneRouter.LoadLobbyAsync);
    }

    private async void OnOfflineTrainingClicked()
    {
        _matchSetupContextService?.Clear();
        await TryNavigateAsync(_sceneRouter.LoadCharacterSelectAsync);
    }

    private void OnReconnectClicked()
    {
        _view.SetStatus("Reconnect flow will be implemented in Phase 3.");
    }

    private void OnSettingsClicked()
    {
        _view.SetStatus("Settings flow will be implemented in Phase 2.");
    }

    private void OnExitClicked()
    {
        Application.Quit();
    }

    private async Task TryNavigateAsync(Func<Task<bool>> navigateAction)
    {
        if (_isNavigating || _sceneRouter.IsTransitionInProgress)
        {
            return;
        }

        _isNavigating = true;
        _view.SetPlayOnlineInteractable(false);
        _view.SetOfflineTrainingInteractable(false);
        _view.SetExitInteractable(false);
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
                ApplyDefaultState();
            }
        }
    }
}
