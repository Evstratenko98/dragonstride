using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;

public sealed class MultiplayerBootstrapEntryPoint : IStartable, IDisposable
{
    private readonly MultiplayerConfig _config;
    private readonly IMultiplayerBootstrapService _bootstrapService;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private bool _started;

    public MultiplayerBootstrapEntryPoint(MultiplayerConfig config, IMultiplayerBootstrapService bootstrapService)
    {
        _config = config;
        _bootstrapService = bootstrapService;
    }

    public void Start()
    {
        if (_started)
        {
            return;
        }

        _started = true;

        if (_config == null || !_config.EnableMultiplayer)
        {
            Debug.Log("[MPS Bootstrap] Disabled by config.");
            return;
        }

        _ = BootstrapAsync(_cancellationTokenSource.Token);
    }

    public void Dispose()
    {
        if (_cancellationTokenSource.IsCancellationRequested)
        {
            return;
        }

        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
    }

    private async Task BootstrapAsync(CancellationToken cancellationToken)
    {
        try
        {
            MultiplayerBootstrapResult result = await _bootstrapService.InitializeAsync(cancellationToken);
            if (!result.IsSuccess)
            {
                Debug.LogWarning($"[MPS Bootstrap] Bootstrap finished with error: {result.ErrorCode}");
            }
        }
        catch (OperationCanceledException)
        {
            Debug.Log("[MPS Bootstrap] Bootstrap cancelled.");
        }
        catch (Exception exception)
        {
            Debug.LogError($"[MPS Bootstrap] Unexpected failure in entry point: {exception}");
        }
    }
}
