using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;

public sealed class MpsMatchNetworkService : IMatchNetworkService
{
    private static readonly TimeSpan ConnectivityTimeout = TimeSpan.FromSeconds(12);
    private readonly IMultiplayerSessionService _sessionService;

    public bool IsReady
    {
        get
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager == null)
            {
                return false;
            }

            return IsHost ? networkManager.IsListening : networkManager.IsConnectedClient;
        }
    }

    public bool IsHost
    {
        get
        {
            if (_sessionService != null && _sessionService.HasActiveSession)
            {
                return _sessionService.ActiveSession.IsHost;
            }

            return NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
        }
    }

    public bool IsClient
    {
        get
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            return networkManager != null && networkManager.IsClient;
        }
    }

    public string LocalPlayerId
    {
        get
        {
            if (AuthenticationService.Instance != null && AuthenticationService.Instance.IsSignedIn)
            {
                return AuthenticationService.Instance.PlayerId ?? string.Empty;
            }

            return string.Empty;
        }
    }

    public MpsMatchNetworkService(IMultiplayerSessionService sessionService)
    {
        _sessionService = sessionService;
    }

    public async Task<MultiplayerOperationResult<bool>> EnsurePreconnectedAsync(CancellationToken ct = default)
    {
        if (_sessionService == null || !_sessionService.HasActiveSession)
        {
            return MultiplayerOperationResult<bool>.Failure("not_in_session", "Active session is required.");
        }

        return await WaitForMatchConnectivityAsync(ct);
    }

    public async Task<MultiplayerOperationResult<bool>> WaitForMatchConnectivityAsync(CancellationToken ct = default)
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null)
        {
            return MultiplayerOperationResult<bool>.Failure(
                "network_manager_missing",
                "NetworkManager runtime root is not initialized.");
        }

        DateTime startedAt = DateTime.UtcNow;
        while (!ct.IsCancellationRequested)
        {
            bool isConnected = IsHost ? networkManager.IsListening : networkManager.IsConnectedClient;
            if (isConnected)
            {
                return MultiplayerOperationResult<bool>.Success(true);
            }

            if (DateTime.UtcNow - startedAt > ConnectivityTimeout)
            {
                return MultiplayerOperationResult<bool>.Failure(
                    "network_connectivity_timeout",
                    "Timed out while waiting for NGO connectivity.");
            }

            await Task.Delay(100, ct);
        }

        return MultiplayerOperationResult<bool>.Failure("cancelled", "Connectivity wait cancelled.");
    }

    public Task ShutdownAsync(CancellationToken ct = default)
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager != null &&
            (networkManager.IsListening || networkManager.IsClient || networkManager.IsServer || networkManager.IsHost))
        {
            networkManager.Shutdown();
        }

        return Task.CompletedTask;
    }
}
