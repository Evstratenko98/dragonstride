using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public sealed class MpsBootstrapService : IMultiplayerBootstrapService
{
    private const string UnknownErrorCode = "unknown_error";
    private readonly IEventBus _eventBus;

    public MpsBootstrapService(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public async Task<MultiplayerBootstrapResult> InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                Debug.Log("[MPS Bootstrap] Initializing Unity Services.");
                await UnityServices.InitializeAsync();
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                Debug.Log("[MPS Bootstrap] Signing in anonymously.");
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            cancellationToken.ThrowIfCancellationRequested();

            string playerId = AuthenticationService.Instance.PlayerId;
            if (string.IsNullOrWhiteSpace(playerId))
            {
                return PublishFailure("missing_player_id", "Authentication completed but PlayerId is empty.");
            }

            var success = MultiplayerBootstrapResult.Success(playerId);
            _eventBus.Publish(new MultiplayerBootstrapSucceeded(playerId));
            Debug.Log($"[MPS Bootstrap] Success. PlayerId: {playerId}");
            return success;
        }
        catch (OperationCanceledException exception)
        {
            return PublishFailure("cancelled", exception.Message);
        }
        catch (AuthenticationException exception)
        {
            return PublishFailure("auth_failed", exception.Message);
        }
        catch (RequestFailedException exception)
        {
            return PublishFailure($"request_failed_{exception.ErrorCode}", exception.Message);
        }
        catch (Exception exception)
        {
            return PublishFailure(UnknownErrorCode, exception.Message);
        }
    }

    private MultiplayerBootstrapResult PublishFailure(string errorCode, string errorMessage)
    {
        var normalizedCode = string.IsNullOrWhiteSpace(errorCode) ? UnknownErrorCode : errorCode;
        var normalizedMessage = string.IsNullOrWhiteSpace(errorMessage) ? "Unknown error." : errorMessage;
        var failed = MultiplayerBootstrapResult.Failure(normalizedCode, normalizedMessage);

        _eventBus.Publish(new MultiplayerBootstrapFailed(failed.ErrorCode, failed.ErrorMessage));
        Debug.LogWarning($"[MPS Bootstrap] Failed ({failed.ErrorCode}): {failed.ErrorMessage}");

        return failed;
    }
}
