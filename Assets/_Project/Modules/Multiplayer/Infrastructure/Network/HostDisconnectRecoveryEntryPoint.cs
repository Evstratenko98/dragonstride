using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.Netcode;
using VContainer.Unity;

public sealed class HostDisconnectRecoveryEntryPoint : IStartable, IDisposable
{
    private readonly IMatchRuntimeRoleService _runtimeRoleService;
    private readonly IMatchNetworkService _matchNetworkService;
    private readonly IMultiplayerSessionService _sessionService;
    private readonly IMatchSetupContextService _matchSetupContextService;
    private readonly ISessionSceneRouter _sceneRouter;

    private int _isRecovering;

    public HostDisconnectRecoveryEntryPoint(
        IMatchRuntimeRoleService runtimeRoleService,
        IMatchNetworkService matchNetworkService,
        IMultiplayerSessionService sessionService,
        IMatchSetupContextService matchSetupContextService,
        ISessionSceneRouter sceneRouter)
    {
        _runtimeRoleService = runtimeRoleService;
        _matchNetworkService = matchNetworkService;
        _sessionService = sessionService;
        _matchSetupContextService = matchSetupContextService;
        _sceneRouter = sceneRouter;
    }

    public void Start()
    {
        if (_runtimeRoleService == null ||
            !_runtimeRoleService.IsOnlineMatch ||
            _runtimeRoleService.IsHostAuthority)
        {
            return;
        }

        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null)
        {
            return;
        }

        networkManager.OnClientDisconnectCallback += OnClientDisconnected;
    }

    public void Dispose()
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager != null)
        {
            networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private async void OnClientDisconnected(ulong clientId)
    {
        if (_runtimeRoleService == null ||
            !_runtimeRoleService.IsOnlineMatch ||
            _runtimeRoleService.IsHostAuthority)
        {
            return;
        }

        if (clientId != NetworkManager.ServerClientId)
        {
            return;
        }

        if (Interlocked.CompareExchange(ref _isRecovering, 1, 0) != 0)
        {
            return;
        }

        await RecoverToMainMenuAsync();
    }

    private async Task RecoverToMainMenuAsync()
    {
        try
        {
            _matchSetupContextService?.Clear();
            if (_matchNetworkService != null)
            {
                await _matchNetworkService.ShutdownAsync();
            }

            if (_sessionService != null && _sessionService.HasActiveSession)
            {
                await _sessionService.LeaveActiveSessionAsync();
            }
        }
        finally
        {
            await _sceneRouter.LoadMainMenuAsync();
            Interlocked.Exchange(ref _isRecovering, 0);
        }
    }
}
