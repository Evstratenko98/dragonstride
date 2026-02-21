using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;

public sealed class DisconnectGraceDriver : IPostInitializable, IDisposable
{
    private readonly IEventBus _eventBus;
    private readonly IMatchRuntimeRoleService _runtimeRoleService;
    private readonly IMatchPauseService _matchPauseService;
    private readonly MultiplayerConfig _multiplayerConfig;
    private readonly CharacterRoster _characterRoster;
    private readonly TurnActorRegistry _turnActorRegistry;
    private readonly TurnFlow _turnFlow;
    private readonly IActorIdentityService _actorIdentityService;

    private readonly Dictionary<string, CancellationTokenSource> _pendingGrace = new(StringComparer.Ordinal);
    private IDisposable _connectionChangedSubscription;

    public DisconnectGraceDriver(
        IEventBus eventBus,
        IMatchRuntimeRoleService runtimeRoleService,
        IMatchPauseService matchPauseService,
        MultiplayerConfig multiplayerConfig,
        CharacterRoster characterRoster,
        TurnActorRegistry turnActorRegistry,
        TurnFlow turnFlow,
        IActorIdentityService actorIdentityService)
    {
        _eventBus = eventBus;
        _runtimeRoleService = runtimeRoleService;
        _matchPauseService = matchPauseService;
        _multiplayerConfig = multiplayerConfig;
        _characterRoster = characterRoster;
        _turnActorRegistry = turnActorRegistry;
        _turnFlow = turnFlow;
        _actorIdentityService = actorIdentityService;
    }

    public void PostInitialize()
    {
        _connectionChangedSubscription = _eventBus.Subscribe<OnlinePlayerConnectionChanged>(OnConnectionChanged);
    }

    public void Dispose()
    {
        _connectionChangedSubscription?.Dispose();
        foreach (KeyValuePair<string, CancellationTokenSource> pair in _pendingGrace)
        {
            pair.Value.Cancel();
            pair.Value.Dispose();
        }

        _pendingGrace.Clear();
    }

    private void OnConnectionChanged(OnlinePlayerConnectionChanged message)
    {
        if (_runtimeRoleService == null ||
            !_runtimeRoleService.IsOnlineMatch ||
            !_runtimeRoleService.IsHostAuthority ||
            string.IsNullOrWhiteSpace(message.PlayerId))
        {
            return;
        }

        if (message.IsConnected)
        {
            HandleReconnected(message.PlayerId);
            return;
        }

        if (_pendingGrace.ContainsKey(message.PlayerId))
        {
            return;
        }

        _matchPauseService.Pause($"Player disconnected: {message.PlayerId}. Waiting for reconnect...");

        var cts = new CancellationTokenSource();
        _pendingGrace[message.PlayerId] = cts;
        _ = RunGraceWindowAsync(message.PlayerId, cts.Token);
    }

    private void HandleReconnected(string playerId)
    {
        if (!_pendingGrace.TryGetValue(playerId, out CancellationTokenSource cts))
        {
            return;
        }

        cts.Cancel();
        cts.Dispose();
        _pendingGrace.Remove(playerId);
        if (_pendingGrace.Count == 0)
        {
            _matchPauseService.Resume();
        }
    }

    private async Task RunGraceWindowAsync(string playerId, CancellationToken cancellationToken)
    {
        int graceSeconds = _multiplayerConfig != null ? _multiplayerConfig.DisconnectGraceSeconds : 45;
        graceSeconds = Math.Max(1, graceSeconds);

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(graceSeconds), cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (!_pendingGrace.TryGetValue(playerId, out CancellationTokenSource currentCts) ||
            currentCts.IsCancellationRequested)
        {
            return;
        }

        Debug.LogWarning($"[DisconnectGraceDriver] Grace timeout reached for player '{playerId}'. Forfeit is applied.");
        ApplyForfeit(playerId);

        currentCts.Dispose();
        _pendingGrace.Remove(playerId);
        if (_pendingGrace.Count == 0)
        {
            _matchPauseService.Resume();
        }
    }

    private void ApplyForfeit(string playerId)
    {
        if (!_characterRoster.TryGetCharacterByPlayerId(playerId, out CharacterInstance character) || character == null)
        {
            return;
        }

        bool wasCurrentActor = ReferenceEquals(_turnFlow.CurrentActor, character);
        _turnActorRegistry.Unregister(character);
        _actorIdentityService.Remove(character);
        _characterRoster.RemoveCharacter(character);

        if (wasCurrentActor)
        {
            if (!_turnFlow.TryEndTurnByAuthority())
            {
                _turnFlow.EndTurn();
            }
        }
    }
}
