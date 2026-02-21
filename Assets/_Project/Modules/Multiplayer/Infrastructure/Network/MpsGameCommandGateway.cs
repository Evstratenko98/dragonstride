using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using VContainer.Unity;

public sealed class MpsGameCommandGateway : IGameCommandGateway, IStartable, IDisposable
{
    private const string SubmitMessageName = "ds3_cmd_submit";
    private const string ApplyMessageName = "ds3_cmd_apply";
    private const string RejectMessageName = "ds3_cmd_reject";
    private const string IdentityMessageName = "ds3_client_identity";
    private const string SnapshotMessageName = "ds3_match_snapshot";
    private const string FieldSnapshotMessageName = "ds3_field_snapshot";
    private const string ActionBatchMessageName = "ds3_action_batch";
    private const string ActionAckMessageName = "ds3_action_ack";
    private const int ActionAckTimeoutMs = 2500;

    private readonly IMatchNetworkService _matchNetworkService;
    private readonly IMatchRuntimeRoleService _runtimeRoleService;
    private readonly ITurnAuthorityService _turnAuthorityService;
    private readonly IGameCommandExecutionService _commandExecutionService;
    private readonly IGameCommandPolicyService _commandPolicyService;
    private readonly IMatchStatePublisher _matchStatePublisher;
    private readonly IMatchStateApplier _matchStateApplier;
    private readonly IMatchActionTimelineService _matchActionTimelineService;
    private readonly IFieldSnapshotService _fieldSnapshotService;
    private readonly FieldState _fieldState;
    private readonly IEventBus _eventBus;

    private readonly Dictionary<ulong, string> _playerByClientId = new();
    private long _localCommandId;
    private long _serverSequence;
    private long _lastAppliedCommandSequence;
    private bool _isStarted;
    private bool _handlersRegistered;
    private int _snapshotBroadcastInFlight;
    private int _fieldSnapshotBroadcastInFlight;
    private int _turnTimelineBusy;
    private CancellationTokenSource _startCts;
    private readonly Dictionary<long, HashSet<ulong>> _pendingActionAcks = new();

    private IDisposable _turnPhaseChangedSubscription;
    private IDisposable _gameStateChangedSubscription;
    private IDisposable _pauseChangedSubscription;

    public MpsGameCommandGateway(
        IMatchNetworkService matchNetworkService,
        IMatchRuntimeRoleService runtimeRoleService,
        ITurnAuthorityService turnAuthorityService,
        IGameCommandExecutionService commandExecutionService,
        IGameCommandPolicyService commandPolicyService,
        IMatchStatePublisher matchStatePublisher,
        IMatchStateApplier matchStateApplier,
        IMatchActionTimelineService matchActionTimelineService,
        IFieldSnapshotService fieldSnapshotService,
        FieldState fieldState,
        IEventBus eventBus)
    {
        _matchNetworkService = matchNetworkService;
        _runtimeRoleService = runtimeRoleService;
        _turnAuthorityService = turnAuthorityService;
        _commandExecutionService = commandExecutionService;
        _commandPolicyService = commandPolicyService;
        _matchStatePublisher = matchStatePublisher;
        _matchStateApplier = matchStateApplier;
        _matchActionTimelineService = matchActionTimelineService;
        _fieldSnapshotService = fieldSnapshotService;
        _fieldState = fieldState;
        _eventBus = eventBus;
    }

    public void Start()
    {
        if (_isStarted || _runtimeRoleService == null || !_runtimeRoleService.IsOnlineMatch)
        {
            return;
        }

        _isStarted = true;
        _startCts = new CancellationTokenSource();
        _ = StartWhenNetworkReadyAsync(_startCts.Token);
    }

    public void Dispose()
    {
        _startCts?.Cancel();
        _startCts?.Dispose();
        _startCts = null;

        _turnPhaseChangedSubscription?.Dispose();
        _gameStateChangedSubscription?.Dispose();
        _pauseChangedSubscription?.Dispose();

        if (!_handlersRegistered)
        {
            _isStarted = false;
            return;
        }

        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager?.CustomMessagingManager == null)
        {
            _isStarted = false;
            _handlersRegistered = false;
            return;
        }

        networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(ApplyMessageName);
        networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(RejectMessageName);
        networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(SubmitMessageName);
        networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(IdentityMessageName);
        networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(SnapshotMessageName);
        networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(FieldSnapshotMessageName);
        networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(ActionBatchMessageName);
        networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(ActionAckMessageName);
        networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        networkManager.OnClientConnectedCallback -= OnClientConnected;
        _handlersRegistered = false;
        _isStarted = false;
    }

    private async Task StartWhenNetworkReadyAsync(CancellationToken cancellationToken)
    {
        try
        {
            const int maxAttempts = 200;
            for (int i = 0; i < maxAttempts; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                NetworkManager networkManager = NetworkManager.Singleton;
                bool isHost = _matchNetworkService.IsHost;
                bool transportReady = networkManager?.CustomMessagingManager != null;
                bool roleReady = isHost ? networkManager != null && networkManager.IsServer : networkManager != null && networkManager.IsClient;
                if (transportReady && roleReady)
                {
                    RegisterHandlers(networkManager, isHost);
                    return;
                }

                await Task.Delay(100, cancellationToken);
            }

            Debug.LogWarning("[MpsGameCommandGateway] Network messaging was not ready in time; gateway handlers were not registered.");
            _isStarted = false;
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void RegisterHandlers(NetworkManager networkManager, bool isHost)
    {
        if (_handlersRegistered || networkManager?.CustomMessagingManager == null)
        {
            return;
        }

        networkManager.CustomMessagingManager.RegisterNamedMessageHandler(ApplyMessageName, OnApplyMessage);
        networkManager.CustomMessagingManager.RegisterNamedMessageHandler(RejectMessageName, OnRejectMessage);
        networkManager.CustomMessagingManager.RegisterNamedMessageHandler(SnapshotMessageName, OnSnapshotMessage);
        networkManager.CustomMessagingManager.RegisterNamedMessageHandler(FieldSnapshotMessageName, OnFieldSnapshotMessage);
        networkManager.CustomMessagingManager.RegisterNamedMessageHandler(ActionBatchMessageName, OnActionBatchMessage);

        if (isHost)
        {
            networkManager.CustomMessagingManager.RegisterNamedMessageHandler(SubmitMessageName, OnSubmitMessage);
            networkManager.CustomMessagingManager.RegisterNamedMessageHandler(IdentityMessageName, OnIdentityMessage);
            networkManager.CustomMessagingManager.RegisterNamedMessageHandler(ActionAckMessageName, OnActionAckMessage);
            networkManager.OnClientDisconnectCallback += OnClientDisconnected;
            networkManager.OnClientConnectedCallback += OnClientConnected;

            _turnPhaseChangedSubscription = _eventBus.Subscribe<TurnPhaseChanged>(OnTurnPhaseChanged);
            _gameStateChangedSubscription = _eventBus.Subscribe<GameStateChanged>(OnGameStateChanged);
            _pauseChangedSubscription = _eventBus.Subscribe<MatchPauseStateChanged>(OnMatchPauseChanged);
            if (_matchStatePublisher != null && _matchActionTimelineService != null)
            {
                _matchActionTimelineService.PrimeBaseline(_matchStatePublisher.Capture(0));
            }

            RequestSnapshotBroadcast("gateway_started");
            RequestFieldSnapshotBroadcast("gateway_started");
            _ = PushCurrentStateToConnectedClientsAsync("gateway_started");
        }
        else
        {
            _ = SendIdentityToHostAsync();
        }

        _handlersRegistered = true;
    }

    public Task<CommandSubmitResult> SubmitMoveAsync(Vector2Int direction, CancellationToken ct = default)
    {
        return SubmitAsync(GameCommandType.Move, direction, 0, ct);
    }

    public Task<CommandSubmitResult> SubmitAttackAsync(int targetActorId, CancellationToken ct = default)
    {
        return SubmitAsync(GameCommandType.Attack, Vector2Int.zero, targetActorId, ct);
    }

    public Task<CommandSubmitResult> SubmitOpenCellAsync(CancellationToken ct = default)
    {
        return SubmitAsync(GameCommandType.OpenCell, Vector2Int.zero, 0, ct);
    }

    public Task<CommandSubmitResult> SubmitEndTurnAsync(CancellationToken ct = default)
    {
        return SubmitAsync(GameCommandType.EndTurn, Vector2Int.zero, 0, ct);
    }

    public Task<CommandSubmitResult> SubmitTakeLootAsync(CancellationToken ct = default)
    {
        return SubmitAsync(GameCommandType.TakeLoot, Vector2Int.zero, 0, ct);
    }

    private async Task<CommandSubmitResult> SubmitAsync(
        GameCommandType commandType,
        Vector2Int direction,
        int targetActorId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var command = new GameCommandEnvelope(
            Interlocked.Increment(ref _localCommandId),
            commandType,
            _matchNetworkService.LocalPlayerId,
            direction,
            targetActorId,
            Environment.TickCount);

        if (_runtimeRoleService == null || !_runtimeRoleService.IsOnlineMatch)
        {
            return await _commandExecutionService.ExecuteAsync(command, cancellationToken);
        }

        if (_matchNetworkService.IsHost)
        {
            return await ProcessHostCommandAsync(NetworkManager.ServerClientId, command, cancellationToken);
        }

        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager?.CustomMessagingManager == null || !networkManager.IsClient)
        {
            return CommandSubmitResult.Rejected("network_not_ready", "Client network transport is not ready.");
        }

        using (var writer = new FastBufferWriter(256, Allocator.Temp))
        {
            WriteCommand(writer, command);
            networkManager.CustomMessagingManager.SendNamedMessage(
                SubmitMessageName,
                NetworkManager.ServerClientId,
                writer,
                NetworkDelivery.ReliableSequenced);
        }

        return CommandSubmitResult.Accepted();
    }

    private async void OnSubmitMessage(ulong senderClientId, FastBufferReader reader)
    {
        if (!_matchNetworkService.IsHost)
        {
            return;
        }

        GameCommandEnvelope command = ReadCommand(ref reader);
        if (string.IsNullOrWhiteSpace(command.PlayerId))
        {
            await SendRejectAsync(senderClientId, command.CommandId, "missing_player", "PlayerId is required.");
            return;
        }

        if (_playerByClientId.TryGetValue(senderClientId, out string mappedPlayerId))
        {
            if (!string.Equals(mappedPlayerId, command.PlayerId, StringComparison.Ordinal))
            {
                await SendRejectAsync(senderClientId, command.CommandId, "player_id_mismatch", "Sender player id mismatch.");
                return;
            }
        }
        else
        {
            MapClientToPlayer(senderClientId, command.PlayerId);
        }

        CommandSubmitResult result = await ProcessHostCommandAsync(senderClientId, command, CancellationToken.None);
        if (!result.IsAccepted)
        {
            await SendRejectAsync(senderClientId, command.CommandId, result.ErrorCode, result.ErrorMessage);
        }
    }

    private void OnIdentityMessage(ulong senderClientId, FastBufferReader reader)
    {
        if (!_matchNetworkService.IsHost)
        {
            return;
        }

        reader.ReadValueSafe(out FixedString128Bytes playerIdValue);
        string playerId = playerIdValue.ToString();
        if (string.IsNullOrWhiteSpace(playerId))
        {
            return;
        }

        MapClientToPlayer(senderClientId, playerId);
        _ = SendFieldSnapshotToClientAsync(senderClientId);
        _ = SendSnapshotToClientAsync(senderClientId);
    }

    private async Task<CommandSubmitResult> ProcessHostCommandAsync(
        ulong senderClientId,
        GameCommandEnvelope command,
        CancellationToken cancellationToken)
    {
        bool isTurnBoundCommand = IsTurnBoundCommand(command.CommandType);
        if (isTurnBoundCommand && Interlocked.CompareExchange(ref _turnTimelineBusy, 1, 0) != 0)
        {
            return CommandSubmitResult.Rejected("timeline_busy", "Another action batch is still resolving.");
        }

        try
        {
            CommandValidationResult validation = _turnAuthorityService.Validate(command);
            if (!validation.IsValid)
            {
                return CommandSubmitResult.Rejected(validation.ErrorCode, validation.ErrorMessage);
            }

            CommandSubmitResult localExecutionResult = await _commandExecutionService.ExecuteAsync(command, cancellationToken);
            if (!localExecutionResult.IsAccepted)
            {
                return localExecutionResult;
            }

            long snapshotSequence = Interlocked.Increment(ref _serverSequence);
            MatchActionBatch actionBatch = default;
            bool hasActionBatch = false;

            if (_matchActionTimelineService != null)
            {
                MultiplayerOperationResult<MatchActionBatch> timelineResult =
                    await _matchActionTimelineService.BuildBatchForAcceptedCommandAsync(command, cancellationToken);
                if (timelineResult.IsSuccess)
                {
                    actionBatch = timelineResult.Value.WithResultSnapshotSequence(snapshotSequence);
                    hasActionBatch = true;
                }
            }

            if (hasActionBatch)
            {
                await _matchActionTimelineService.PlayBatchLocallyAsync(actionBatch, cancellationToken);
                await BroadcastActionBatchAsync(actionBatch);
                if (isTurnBoundCommand && actionBatch.BlocksTurnInput)
                {
                    await WaitForActionAcksAsync(actionBatch.ActionSequence, cancellationToken);
                }
            }
            else
            {
                await BroadcastAppliedCommandAsync(snapshotSequence, command);
            }

            await BroadcastSnapshotAsync(snapshotSequence);
            return CommandSubmitResult.Accepted(snapshotSequence);
        }
        finally
        {
            if (isTurnBoundCommand)
            {
                Interlocked.Exchange(ref _turnTimelineBusy, 0);
            }
        }
    }

    private async Task BroadcastAppliedCommandAsync(long sequence, GameCommandEnvelope command)
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager?.CustomMessagingManager == null || !networkManager.IsServer)
        {
            return;
        }

        IReadOnlyList<ulong> connectedClientIds = networkManager.ConnectedClientsIds;
        for (int i = 0; i < connectedClientIds.Count; i++)
        {
            ulong clientId = connectedClientIds[i];
            if (clientId == NetworkManager.ServerClientId)
            {
                continue;
            }

            using var writer = new FastBufferWriter(320, Allocator.Temp);
            writer.WriteValueSafe(sequence);
            WriteCommand(writer, command);
            networkManager.CustomMessagingManager.SendNamedMessage(
                ApplyMessageName,
                clientId,
                writer,
                NetworkDelivery.ReliableSequenced);
        }

        await Task.CompletedTask;
    }

    private async Task BroadcastActionBatchAsync(MatchActionBatch batch)
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager?.CustomMessagingManager == null || !networkManager.IsServer)
        {
            return;
        }

        var pendingAcks = new HashSet<ulong>();
        IReadOnlyList<ulong> connectedClientIds = networkManager.ConnectedClientsIds;
        for (int i = 0; i < connectedClientIds.Count; i++)
        {
            ulong clientId = connectedClientIds[i];
            if (clientId == NetworkManager.ServerClientId)
            {
                continue;
            }

            pendingAcks.Add(clientId);
            using var writer = new FastBufferWriter(32768, Allocator.Temp);
            WriteActionBatch(writer, batch);
            networkManager.CustomMessagingManager.SendNamedMessage(
                ActionBatchMessageName,
                clientId,
                writer,
                NetworkDelivery.ReliableSequenced);
        }

        lock (_pendingActionAcks)
        {
            if (pendingAcks.Count == 0)
            {
                _pendingActionAcks.Remove(batch.ActionSequence);
            }
            else
            {
                _pendingActionAcks[batch.ActionSequence] = pendingAcks;
            }
        }

        await Task.CompletedTask;
    }

    private async Task WaitForActionAcksAsync(long actionSequence, CancellationToken cancellationToken)
    {
        DateTime expiresAt = DateTime.UtcNow.AddMilliseconds(ActionAckTimeoutMs);
        while (!cancellationToken.IsCancellationRequested && DateTime.UtcNow < expiresAt)
        {
            bool completed;
            lock (_pendingActionAcks)
            {
                completed = !_pendingActionAcks.TryGetValue(actionSequence, out HashSet<ulong> pending) ||
                            pending == null ||
                            pending.Count == 0;
                if (completed)
                {
                    _pendingActionAcks.Remove(actionSequence);
                }
            }

            if (completed)
            {
                return;
            }

            await Task.Delay(25, cancellationToken);
        }

        lock (_pendingActionAcks)
        {
            _pendingActionAcks.Remove(actionSequence);
        }

        Debug.LogWarning($"[MpsGameCommandGateway] Action ack timeout for sequence {actionSequence}.");
    }

    private async Task BroadcastSnapshotAsync(long sequence)
    {
        if (!_matchNetworkService.IsHost || _matchStatePublisher == null)
        {
            return;
        }

        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager?.CustomMessagingManager == null || !networkManager.IsServer)
        {
            return;
        }

        MatchStateSnapshot snapshot = _matchStatePublisher.Capture(sequence);

        IReadOnlyList<ulong> connectedClientIds = networkManager.ConnectedClientsIds;
        for (int i = 0; i < connectedClientIds.Count; i++)
        {
            ulong clientId = connectedClientIds[i];
            if (clientId == NetworkManager.ServerClientId)
            {
                continue;
            }

            using var writer = new FastBufferWriter(32768, Allocator.Temp);
            WriteSnapshot(writer, snapshot);
            networkManager.CustomMessagingManager.SendNamedMessage(
                SnapshotMessageName,
                clientId,
                writer,
                NetworkDelivery.ReliableSequenced);
        }

        await Task.CompletedTask;
    }

    private async Task BroadcastFieldSnapshotAsync()
    {
        if (!_matchNetworkService.IsHost || _fieldSnapshotService == null || _fieldState?.CurrentField == null)
        {
            return;
        }

        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager?.CustomMessagingManager == null || !networkManager.IsServer)
        {
            return;
        }

        FieldGridSnapshot snapshot = _fieldSnapshotService.Capture(_fieldState.CurrentField);
        if (snapshot.Width <= 0 || snapshot.Height <= 0)
        {
            return;
        }

        IReadOnlyList<ulong> connectedClientIds = networkManager.ConnectedClientsIds;
        for (int i = 0; i < connectedClientIds.Count; i++)
        {
            ulong clientId = connectedClientIds[i];
            if (clientId == NetworkManager.ServerClientId)
            {
                continue;
            }

            using var writer = new FastBufferWriter(32768, Allocator.Temp);
            WriteFieldSnapshot(writer, snapshot);
            networkManager.CustomMessagingManager.SendNamedMessage(
                FieldSnapshotMessageName,
                clientId,
                writer,
                NetworkDelivery.ReliableSequenced);
        }

        await Task.CompletedTask;
    }

    private async Task SendSnapshotToClientAsync(ulong clientId)
    {
        if (!_matchNetworkService.IsHost || _matchStatePublisher == null)
        {
            return;
        }

        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager?.CustomMessagingManager == null || !networkManager.IsServer)
        {
            return;
        }

        long sequence = Interlocked.Increment(ref _serverSequence);
        MatchStateSnapshot snapshot = _matchStatePublisher.Capture(sequence);
        using var writer = new FastBufferWriter(32768, Allocator.Temp);
        WriteSnapshot(writer, snapshot);
        networkManager.CustomMessagingManager.SendNamedMessage(
            SnapshotMessageName,
            clientId,
            writer,
            NetworkDelivery.ReliableSequenced);
        await Task.CompletedTask;
    }

    private async Task SendFieldSnapshotToClientAsync(ulong clientId)
    {
        if (!_matchNetworkService.IsHost || _fieldSnapshotService == null || _fieldState?.CurrentField == null)
        {
            return;
        }

        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager?.CustomMessagingManager == null || !networkManager.IsServer)
        {
            return;
        }

        FieldGridSnapshot snapshot = _fieldSnapshotService.Capture(_fieldState.CurrentField);
        if (snapshot.Width <= 0 || snapshot.Height <= 0)
        {
            return;
        }

        using var writer = new FastBufferWriter(32768, Allocator.Temp);
        WriteFieldSnapshot(writer, snapshot);
        networkManager.CustomMessagingManager.SendNamedMessage(
            FieldSnapshotMessageName,
            clientId,
            writer,
            NetworkDelivery.ReliableSequenced);
        await Task.CompletedTask;
    }

    private async Task SendRejectAsync(ulong senderClientId, long commandId, string errorCode, string errorMessage)
    {
        if (senderClientId == NetworkManager.ServerClientId)
        {
            return;
        }

        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager?.CustomMessagingManager == null || !networkManager.IsServer)
        {
            return;
        }

        using var writer = new FastBufferWriter(320, Allocator.Temp);
        writer.WriteValueSafe(commandId);
        writer.WriteValueSafe(new FixedString128Bytes(errorCode ?? string.Empty));
        writer.WriteValueSafe(new FixedString512Bytes(errorMessage ?? string.Empty));
        networkManager.CustomMessagingManager.SendNamedMessage(
            RejectMessageName,
            senderClientId,
            writer,
            NetworkDelivery.ReliableSequenced);
        await Task.CompletedTask;
    }

    private void OnApplyMessage(ulong senderClientId, FastBufferReader reader)
    {
        if (_matchNetworkService.IsHost)
        {
            return;
        }

        reader.ReadValueSafe(out long sequence);
        if (sequence <= _lastAppliedCommandSequence)
        {
            return;
        }

        _lastAppliedCommandSequence = sequence;
        _ = ReadCommand(ref reader);
    }

    private async void OnActionBatchMessage(ulong senderClientId, FastBufferReader reader)
    {
        if (_matchNetworkService.IsHost)
        {
            return;
        }

        MatchActionBatch batch = ReadActionBatch(ref reader);
        if (batch.ActionSequence <= 0)
        {
            return;
        }

        try
        {
            if (_matchActionTimelineService != null)
            {
                await _matchActionTimelineService.PlayBatchLocallyAsync(batch);
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[MpsGameCommandGateway] Failed to apply action batch {batch.ActionSequence}: {exception.Message}");
        }
        await SendActionAckToHostAsync(batch.ActionSequence);
    }

    private void OnActionAckMessage(ulong senderClientId, FastBufferReader reader)
    {
        if (!_matchNetworkService.IsHost)
        {
            return;
        }

        reader.ReadValueSafe(out long actionSequence);
        lock (_pendingActionAcks)
        {
            if (_pendingActionAcks.TryGetValue(actionSequence, out HashSet<ulong> pending))
            {
                pending.Remove(senderClientId);
                if (pending.Count == 0)
                {
                    _pendingActionAcks.Remove(actionSequence);
                }
            }
        }
    }

    private void OnFieldSnapshotMessage(ulong senderClientId, FastBufferReader reader)
    {
        if (_matchNetworkService.IsHost)
        {
            return;
        }

        FieldGridSnapshot snapshot = ReadFieldSnapshot(ref reader);
        if (snapshot.Width <= 0 || snapshot.Height <= 0)
        {
            return;
        }

        _eventBus.Publish(new FieldSnapshotReceived(snapshot));
    }

    private void OnSnapshotMessage(ulong senderClientId, FastBufferReader reader)
    {
        if (_matchNetworkService.IsHost || _matchStateApplier == null)
        {
            return;
        }

        MatchStateSnapshot snapshot = ReadSnapshot(ref reader);
        _matchStateApplier.TryApply(snapshot);
    }

    private void OnRejectMessage(ulong senderClientId, FastBufferReader reader)
    {
        reader.ReadValueSafe(out long commandId);
        reader.ReadValueSafe(out FixedString128Bytes errorCode);
        reader.ReadValueSafe(out FixedString512Bytes errorMessage);
        Debug.LogWarning($"[MpsGameCommandGateway] Command {commandId} rejected ({errorCode}): {errorMessage}");
    }

    private async Task SendIdentityToHostAsync()
    {
        string localPlayerId = _matchNetworkService.LocalPlayerId;
        if (string.IsNullOrWhiteSpace(localPlayerId))
        {
            return;
        }

        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null)
        {
            return;
        }

        const int maxWaitIterations = 20;
        int iteration = 0;
        while (iteration < maxWaitIterations &&
               (networkManager.CustomMessagingManager == null || !networkManager.IsClient))
        {
            iteration++;
            await Task.Delay(100);
        }

        if (networkManager.CustomMessagingManager == null || !networkManager.IsClient)
        {
            return;
        }

        using var writer = new FastBufferWriter(160, Allocator.Temp);
        writer.WriteValueSafe(new FixedString128Bytes(localPlayerId));
        networkManager.CustomMessagingManager.SendNamedMessage(
            IdentityMessageName,
            NetworkManager.ServerClientId,
            writer,
            NetworkDelivery.ReliableSequenced);
    }

    private async Task SendActionAckToHostAsync(long actionSequence)
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager?.CustomMessagingManager == null || !networkManager.IsClient)
        {
            return;
        }

        await Task.Yield();
        using var writer = new FastBufferWriter(16, Allocator.Temp);
        writer.WriteValueSafe(actionSequence);
        networkManager.CustomMessagingManager.SendNamedMessage(
            ActionAckMessageName,
            NetworkManager.ServerClientId,
            writer,
            NetworkDelivery.ReliableSequenced);
    }

    private void OnTurnPhaseChanged(TurnPhaseChanged _)
    {
        RequestSnapshotBroadcast("turn_phase");
    }

    private void OnGameStateChanged(GameStateChanged _)
    {
        RequestSnapshotBroadcast("game_state");
        RequestFieldSnapshotBroadcast("game_state");
    }

    private void OnMatchPauseChanged(MatchPauseStateChanged _)
    {
        RequestSnapshotBroadcast("pause_state");
    }

    private void RequestSnapshotBroadcast(string reason)
    {
        if (!_matchNetworkService.IsHost)
        {
            return;
        }

        if (Interlocked.CompareExchange(ref _snapshotBroadcastInFlight, 1, 0) != 0)
        {
            return;
        }

        _ = BroadcastSnapshotDeferredAsync(reason);
    }

    private void RequestFieldSnapshotBroadcast(string reason)
    {
        if (!_matchNetworkService.IsHost || _fieldState?.CurrentField == null)
        {
            return;
        }

        if (Interlocked.CompareExchange(ref _fieldSnapshotBroadcastInFlight, 1, 0) != 0)
        {
            return;
        }

        _ = BroadcastFieldSnapshotDeferredAsync(reason);
    }

    private async Task BroadcastSnapshotDeferredAsync(string reason)
    {
        try
        {
            await Task.Delay(60);
            long sequence = Interlocked.Increment(ref _serverSequence);
            await BroadcastSystemActionBatchIfNeededAsync(sequence);
            await BroadcastSnapshotAsync(sequence);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[MpsGameCommandGateway] Failed to broadcast snapshot ({reason}): {exception.Message}");
        }
        finally
        {
            Interlocked.Exchange(ref _snapshotBroadcastInFlight, 0);
        }
    }

    private async Task BroadcastSystemActionBatchIfNeededAsync(long snapshotSequence)
    {
        if (!_matchNetworkService.IsHost || _matchActionTimelineService == null)
        {
            return;
        }

        var systemCommand = new GameCommandEnvelope(
            0,
            GameCommandType.None,
            string.Empty,
            Vector2Int.zero,
            0,
            Environment.TickCount);

        MultiplayerOperationResult<MatchActionBatch> batchResult =
            await _matchActionTimelineService.BuildBatchForAcceptedCommandAsync(systemCommand);
        if (!batchResult.IsSuccess)
        {
            return;
        }

        MatchActionBatch batch = batchResult.Value.WithResultSnapshotSequence(snapshotSequence);
        await _matchActionTimelineService.PlayBatchLocallyAsync(batch);
        await BroadcastActionBatchAsync(batch);
    }

    private async Task BroadcastFieldSnapshotDeferredAsync(string reason)
    {
        try
        {
            await Task.Delay(60);
            await BroadcastFieldSnapshotAsync();
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[MpsGameCommandGateway] Failed to broadcast field snapshot ({reason}): {exception.Message}");
        }
        finally
        {
            Interlocked.Exchange(ref _fieldSnapshotBroadcastInFlight, 0);
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!_matchNetworkService.IsHost || clientId == NetworkManager.ServerClientId)
        {
            return;
        }

        _ = SendFieldSnapshotToClientAsync(clientId);
        _ = SendSnapshotToClientAsync(clientId);
    }

    private async Task PushCurrentStateToConnectedClientsAsync(string reason)
    {
        try
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (!_matchNetworkService.IsHost || networkManager == null || !networkManager.IsServer)
            {
                return;
            }

            IReadOnlyList<ulong> connectedClientIds = networkManager.ConnectedClientsIds;
            for (int i = 0; i < connectedClientIds.Count; i++)
            {
                ulong clientId = connectedClientIds[i];
                if (clientId == NetworkManager.ServerClientId)
                {
                    continue;
                }

                await SendFieldSnapshotToClientAsync(clientId);
                await SendSnapshotToClientAsync(clientId);
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[MpsGameCommandGateway] Failed to push state to connected clients ({reason}): {exception.Message}");
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!_matchNetworkService.IsHost || clientId == NetworkManager.ServerClientId)
        {
            return;
        }

        if (_playerByClientId.TryGetValue(clientId, out string playerId) && !string.IsNullOrWhiteSpace(playerId))
        {
            _eventBus.Publish(new OnlinePlayerConnectionChanged(playerId, false));
            _playerByClientId.Remove(clientId);
        }

        lock (_pendingActionAcks)
        {
            foreach (KeyValuePair<long, HashSet<ulong>> kv in _pendingActionAcks)
            {
                kv.Value?.Remove(clientId);
            }
        }

        RequestSnapshotBroadcast("client_disconnected");
    }

    private void MapClientToPlayer(ulong clientId, string playerId)
    {
        if (string.IsNullOrWhiteSpace(playerId))
        {
            return;
        }

        if (_playerByClientId.TryGetValue(clientId, out string existingPlayerId) &&
            string.Equals(existingPlayerId, playerId, StringComparison.Ordinal))
        {
            return;
        }

        _playerByClientId[clientId] = playerId;
        _eventBus.Publish(new OnlinePlayerConnectionChanged(playerId, true));
    }

    private bool IsTurnBoundCommand(GameCommandType commandType)
    {
        CommandTiming timing = _commandPolicyService?.GetTiming(commandType) ?? CommandTiming.TurnBound;
        return timing == CommandTiming.TurnBound;
    }

    private static void WriteCommand(FastBufferWriter writer, GameCommandEnvelope command)
    {
        writer.WriteValueSafe(command.CommandId);
        writer.WriteValueSafe((int)command.CommandType);
        writer.WriteValueSafe(new FixedString128Bytes(command.PlayerId ?? string.Empty));
        writer.WriteValueSafe(command.Direction.x);
        writer.WriteValueSafe(command.Direction.y);
        writer.WriteValueSafe(command.TargetActorId);
        writer.WriteValueSafe(command.ClientTick);
    }

    private static GameCommandEnvelope ReadCommand(ref FastBufferReader reader)
    {
        reader.ReadValueSafe(out long commandId);
        reader.ReadValueSafe(out int commandTypeValue);
        reader.ReadValueSafe(out FixedString128Bytes playerId);
        reader.ReadValueSafe(out int dirX);
        reader.ReadValueSafe(out int dirY);
        reader.ReadValueSafe(out int targetActorId);
        reader.ReadValueSafe(out int clientTick);

        return new GameCommandEnvelope(
            commandId,
            (GameCommandType)commandTypeValue,
            playerId.ToString(),
            new Vector2Int(dirX, dirY),
            targetActorId,
            clientTick);
    }

    private static void WriteActionBatch(FastBufferWriter writer, MatchActionBatch batch)
    {
        writer.WriteValueSafe(batch.ActionSequence);
        writer.WriteValueSafe(batch.ResultSnapshotSequence);
        writer.WriteValueSafe(new FixedString128Bytes(batch.SourcePlayerId ?? string.Empty));
        writer.WriteValueSafe(batch.BlocksTurnInput);

        int eventCount = batch.Events?.Count ?? 0;
        writer.WriteValueSafe(eventCount);
        for (int i = 0; i < eventCount; i++)
        {
            ActionEventEnvelope actionEvent = batch.Events[i];
            writer.WriteValueSafe((int)actionEvent.Type);
            writer.WriteValueSafe(actionEvent.ActorId);
            writer.WriteValueSafe(actionEvent.TargetActorId);
            writer.WriteValueSafe(actionEvent.FromX);
            writer.WriteValueSafe(actionEvent.FromY);
            writer.WriteValueSafe(actionEvent.ToX);
            writer.WriteValueSafe(actionEvent.ToY);
            writer.WriteValueSafe(actionEvent.IntValue1);
            writer.WriteValueSafe(actionEvent.IntValue2);
            writer.WriteValueSafe(actionEvent.BoolValue1);
            writer.WriteValueSafe(actionEvent.BoolValue2);
            writer.WriteValueSafe(new FixedString4096Bytes(actionEvent.StrValue1 ?? string.Empty));
            writer.WriteValueSafe(new FixedString4096Bytes(actionEvent.StrValue2 ?? string.Empty));
            writer.WriteValueSafe(actionEvent.DurationMs);
        }
    }

    private static MatchActionBatch ReadActionBatch(ref FastBufferReader reader)
    {
        reader.ReadValueSafe(out long actionSequence);
        reader.ReadValueSafe(out long resultSnapshotSequence);
        reader.ReadValueSafe(out FixedString128Bytes sourcePlayerId);
        reader.ReadValueSafe(out bool blocksTurnInput);

        reader.ReadValueSafe(out int eventCount);
        var events = new List<ActionEventEnvelope>(Math.Max(0, eventCount));
        for (int i = 0; i < eventCount; i++)
        {
            reader.ReadValueSafe(out int eventTypeValue);
            reader.ReadValueSafe(out int actorId);
            reader.ReadValueSafe(out int targetActorId);
            reader.ReadValueSafe(out int fromX);
            reader.ReadValueSafe(out int fromY);
            reader.ReadValueSafe(out int toX);
            reader.ReadValueSafe(out int toY);
            reader.ReadValueSafe(out int intValue1);
            reader.ReadValueSafe(out int intValue2);
            reader.ReadValueSafe(out bool boolValue1);
            reader.ReadValueSafe(out bool boolValue2);
            reader.ReadValueSafe(out FixedString4096Bytes strValue1);
            reader.ReadValueSafe(out FixedString4096Bytes strValue2);
            reader.ReadValueSafe(out int durationMs);

            events.Add(new ActionEventEnvelope(
                (ActionEventType)eventTypeValue,
                actorId,
                targetActorId,
                fromX,
                fromY,
                toX,
                toY,
                intValue1,
                intValue2,
                boolValue1,
                boolValue2,
                strValue1.ToString(),
                strValue2.ToString(),
                durationMs));
        }

        return new MatchActionBatch(
            actionSequence,
            resultSnapshotSequence,
            sourcePlayerId.ToString(),
            events,
            blocksTurnInput);
    }

    private static void WriteFieldSnapshot(FastBufferWriter writer, FieldGridSnapshot snapshot)
    {
        writer.WriteValueSafe(snapshot.Width);
        writer.WriteValueSafe(snapshot.Height);
        writer.WriteValueSafe(snapshot.StartX);
        writer.WriteValueSafe(snapshot.StartY);
        writer.WriteValueSafe(new FixedString128Bytes(snapshot.Checksum ?? string.Empty));

        int cellCount = snapshot.Cells?.Count ?? 0;
        writer.WriteValueSafe(cellCount);
        for (int i = 0; i < cellCount; i++)
        {
            FieldCellSnapshot cell = snapshot.Cells[i];
            writer.WriteValueSafe(cell.X);
            writer.WriteValueSafe(cell.Y);
            writer.WriteValueSafe((int)cell.Type);
            writer.WriteValueSafe(cell.IsOpened);
            writer.WriteValueSafe(cell.IsTypeRevealed);
        }

        int linkCount = snapshot.Links?.Count ?? 0;
        writer.WriteValueSafe(linkCount);
        for (int i = 0; i < linkCount; i++)
        {
            FieldLinkSnapshot link = snapshot.Links[i];
            writer.WriteValueSafe(link.AX);
            writer.WriteValueSafe(link.AY);
            writer.WriteValueSafe(link.BX);
            writer.WriteValueSafe(link.BY);
        }
    }

    private static FieldGridSnapshot ReadFieldSnapshot(ref FastBufferReader reader)
    {
        reader.ReadValueSafe(out int width);
        reader.ReadValueSafe(out int height);
        reader.ReadValueSafe(out int startX);
        reader.ReadValueSafe(out int startY);
        reader.ReadValueSafe(out FixedString128Bytes checksum);

        reader.ReadValueSafe(out int cellCount);
        var cells = new List<FieldCellSnapshot>(Math.Max(0, cellCount));
        for (int i = 0; i < cellCount; i++)
        {
            reader.ReadValueSafe(out int x);
            reader.ReadValueSafe(out int y);
            reader.ReadValueSafe(out int typeValue);
            reader.ReadValueSafe(out bool isOpened);
            reader.ReadValueSafe(out bool isTypeRevealed);
            cells.Add(new FieldCellSnapshot(x, y, (CellType)typeValue, isOpened, isTypeRevealed));
        }

        reader.ReadValueSafe(out int linkCount);
        var links = new List<FieldLinkSnapshot>(Math.Max(0, linkCount));
        for (int i = 0; i < linkCount; i++)
        {
            reader.ReadValueSafe(out int ax);
            reader.ReadValueSafe(out int ay);
            reader.ReadValueSafe(out int bx);
            reader.ReadValueSafe(out int by);
            links.Add(new FieldLinkSnapshot(ax, ay, bx, by));
        }

        return new FieldGridSnapshot(
            width,
            height,
            cells,
            links,
            startX,
            startY,
            checksum.ToString());
    }

    private static void WriteSnapshot(FastBufferWriter writer, MatchStateSnapshot snapshot)
    {
        writer.WriteValueSafe(snapshot.Sequence);
        writer.WriteValueSafe((int)snapshot.GameState);
        writer.WriteValueSafe((int)snapshot.TurnState);
        writer.WriteValueSafe(snapshot.CurrentActorId);
        writer.WriteValueSafe(snapshot.StepsTotal);
        writer.WriteValueSafe(snapshot.StepsRemaining);
        writer.WriteValueSafe(snapshot.IsPaused);
        writer.WriteValueSafe(new FixedString512Bytes(snapshot.PauseReason ?? string.Empty));
        writer.WriteValueSafe(new FixedString128Bytes(snapshot.Phase ?? string.Empty));

        int actorCount = snapshot.Actors?.Count ?? 0;
        writer.WriteValueSafe(actorCount);
        for (int i = 0; i < actorCount; i++)
        {
            ActorStateSnapshot actor = snapshot.Actors[i];
            writer.WriteValueSafe(actor.ActorId);
            writer.WriteValueSafe(new FixedString64Bytes(actor.ActorType ?? string.Empty));
            writer.WriteValueSafe(new FixedString128Bytes(actor.OwnerPlayerId ?? string.Empty));
            writer.WriteValueSafe(new FixedString128Bytes(actor.CharacterId ?? string.Empty));
            writer.WriteValueSafe(new FixedString128Bytes(actor.DisplayName ?? string.Empty));
            writer.WriteValueSafe(actor.CellX);
            writer.WriteValueSafe(actor.CellY);
            writer.WriteValueSafe(actor.Health);
            writer.WriteValueSafe(actor.Level);
            writer.WriteValueSafe(actor.HasCrown);
            writer.WriteValueSafe(actor.IsAlive);
        }

        int openedCount = snapshot.OpenedCells?.Count ?? 0;
        writer.WriteValueSafe(openedCount);
        for (int i = 0; i < openedCount; i++)
        {
            OpenedCellSnapshot cell = snapshot.OpenedCells[i];
            writer.WriteValueSafe(cell.X);
            writer.WriteValueSafe(cell.Y);
            writer.WriteValueSafe(new FixedString64Bytes(cell.CellType ?? string.Empty));
        }

        int inventoryCount = snapshot.Inventories?.Count ?? 0;
        writer.WriteValueSafe(inventoryCount);
        for (int i = 0; i < inventoryCount; i++)
        {
            CharacterInventorySnapshot inventory = snapshot.Inventories[i];
            writer.WriteValueSafe(inventory.ActorId);

            int slotCount = inventory.Slots?.Count ?? 0;
            writer.WriteValueSafe(slotCount);
            for (int j = 0; j < slotCount; j++)
            {
                InventorySlotSnapshot slot = inventory.Slots[j];
                writer.WriteValueSafe(slot.SlotIndex);
                writer.WriteValueSafe(new FixedString128Bytes(slot.ItemId ?? string.Empty));
                writer.WriteValueSafe(slot.Count);
            }
        }
    }

    private static MatchStateSnapshot ReadSnapshot(ref FastBufferReader reader)
    {
        reader.ReadValueSafe(out long sequence);
        reader.ReadValueSafe(out int gameStateValue);
        reader.ReadValueSafe(out int turnStateValue);
        reader.ReadValueSafe(out int currentActorId);
        reader.ReadValueSafe(out int stepsTotal);
        reader.ReadValueSafe(out int stepsRemaining);
        reader.ReadValueSafe(out bool isPaused);
        reader.ReadValueSafe(out FixedString512Bytes pauseReason);
        reader.ReadValueSafe(out FixedString128Bytes phase);

        reader.ReadValueSafe(out int actorCount);
        var actors = new List<ActorStateSnapshot>(Math.Max(0, actorCount));
        for (int i = 0; i < actorCount; i++)
        {
            reader.ReadValueSafe(out int actorId);
            reader.ReadValueSafe(out FixedString64Bytes actorType);
            reader.ReadValueSafe(out FixedString128Bytes ownerPlayerId);
            reader.ReadValueSafe(out FixedString128Bytes characterId);
            reader.ReadValueSafe(out FixedString128Bytes displayName);
            reader.ReadValueSafe(out int cellX);
            reader.ReadValueSafe(out int cellY);
            reader.ReadValueSafe(out int health);
            reader.ReadValueSafe(out int level);
            reader.ReadValueSafe(out bool hasCrown);
            reader.ReadValueSafe(out bool isAlive);

            actors.Add(new ActorStateSnapshot(
                actorId,
                actorType.ToString(),
                ownerPlayerId.ToString(),
                characterId.ToString(),
                displayName.ToString(),
                cellX,
                cellY,
                health,
                level,
                hasCrown,
                isAlive));
        }

        reader.ReadValueSafe(out int openedCount);
        var openedCells = new List<OpenedCellSnapshot>(Math.Max(0, openedCount));
        for (int i = 0; i < openedCount; i++)
        {
            reader.ReadValueSafe(out int x);
            reader.ReadValueSafe(out int y);
            reader.ReadValueSafe(out FixedString64Bytes cellType);
            openedCells.Add(new OpenedCellSnapshot(x, y, cellType.ToString()));
        }

        reader.ReadValueSafe(out int inventoryCount);
        var inventories = new List<CharacterInventorySnapshot>(Math.Max(0, inventoryCount));
        for (int i = 0; i < inventoryCount; i++)
        {
            reader.ReadValueSafe(out int actorId);
            reader.ReadValueSafe(out int slotCount);
            var slots = new List<InventorySlotSnapshot>(Math.Max(0, slotCount));
            for (int j = 0; j < slotCount; j++)
            {
                reader.ReadValueSafe(out int slotIndex);
                reader.ReadValueSafe(out FixedString128Bytes itemId);
                reader.ReadValueSafe(out int count);
                slots.Add(new InventorySlotSnapshot(slotIndex, itemId.ToString(), count));
            }

            inventories.Add(new CharacterInventorySnapshot(actorId, slots));
        }

        return new MatchStateSnapshot(
            sequence,
            (GameState)gameStateValue,
            (TurnState)turnStateValue,
            currentActorId,
            stepsTotal,
            stepsRemaining,
            isPaused,
            pauseReason.ToString(),
            phase.ToString(),
            actors,
            openedCells,
            inventories);
    }
}
