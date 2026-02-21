using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public sealed class MatchActionTimelineService : IMatchActionTimelineService
{
    private const int DefaultMoveDurationMs = 220;
    private const int DefaultAttackDurationMs = 260;
    private const int DefaultCellOpenDurationMs = 120;

    private readonly IMatchStatePublisher _matchStatePublisher;
    private readonly IClientActionPlaybackService _clientActionPlaybackService;
    private readonly ILootSyncService _lootSyncService;

    private long _nextActionSequence = 1;
    private bool _hasBaseline;
    private MatchStateSnapshot _baseline;

    public bool IsPlaybackInProgress => _clientActionPlaybackService != null && _clientActionPlaybackService.IsBusy;

    public MatchActionTimelineService(
        IMatchStatePublisher matchStatePublisher,
        IClientActionPlaybackService clientActionPlaybackService,
        ILootSyncService lootSyncService)
    {
        _matchStatePublisher = matchStatePublisher;
        _clientActionPlaybackService = clientActionPlaybackService;
        _lootSyncService = lootSyncService;
    }

    public void PrimeBaseline(MatchStateSnapshot snapshot)
    {
        if (!_hasBaseline)
        {
            _baseline = snapshot;
            _hasBaseline = true;
        }
    }

    public Task<MultiplayerOperationResult<MatchActionBatch>> BuildBatchForAcceptedCommandAsync(
        GameCommandEnvelope command,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (_matchStatePublisher == null)
        {
            return Task.FromResult(MultiplayerOperationResult<MatchActionBatch>.Failure(
                "timeline_unavailable",
                "MatchStatePublisher is not available."));
        }

        MatchStateSnapshot current = _matchStatePublisher.Capture(0);
        if (!_hasBaseline)
        {
            _baseline = current;
            _hasBaseline = true;

            if (command.CommandType == GameCommandType.None)
            {
                return Task.FromResult(MultiplayerOperationResult<MatchActionBatch>.Failure(
                    "no_events",
                    "No baseline diff yet."));
            }
        }

        var events = new List<ActionEventEnvelope>();
        BuildTurnStateEvent(_baseline, current, events);
        BuildMovedEvents(_baseline, current, events);
        BuildOpenedCellEvents(_baseline, current, events);
        BuildEnemySpawnDespawnEvents(_baseline, current, events);
        BuildAttackEvent(command, _baseline, current, events);

        if (_lootSyncService != null)
        {
            IReadOnlyList<ActionEventEnvelope> lootEvents = _lootSyncService.DrainPendingTimelineEvents();
            if (lootEvents != null)
            {
                for (int i = 0; i < lootEvents.Count; i++)
                {
                    events.Add(lootEvents[i]);
                }
            }
        }

        _baseline = current;

        if (events.Count == 0)
        {
            return Task.FromResult(MultiplayerOperationResult<MatchActionBatch>.Failure(
                "no_events",
                "No timeline events were produced."));
        }

        bool blocksTurnInput = command.CommandType != GameCommandType.None || HasBlockingEvents(events);
        var batch = new MatchActionBatch(
            _nextActionSequence++,
            0,
            command.PlayerId ?? string.Empty,
            events,
            blocksTurnInput);

        return Task.FromResult(MultiplayerOperationResult<MatchActionBatch>.Success(batch));
    }

    public Task PlayBatchLocallyAsync(MatchActionBatch batch, CancellationToken ct = default)
    {
        if (_clientActionPlaybackService == null)
        {
            return Task.CompletedTask;
        }

        return _clientActionPlaybackService.ApplyBatchAsync(batch, ct);
    }

    private static bool HasBlockingEvents(IReadOnlyList<ActionEventEnvelope> events)
    {
        for (int i = 0; i < events.Count; i++)
        {
            ActionEventType type = events[i].Type;
            if (type == ActionEventType.ActorMoved ||
                type == ActionEventType.AttackResolved ||
                type == ActionEventType.CellOpened ||
                type == ActionEventType.EnemySpawned ||
                type == ActionEventType.EnemyDespawned ||
                type == ActionEventType.TurnStateChanged)
            {
                return true;
            }
        }

        return false;
    }

    private static void BuildTurnStateEvent(
        MatchStateSnapshot before,
        MatchStateSnapshot after,
        List<ActionEventEnvelope> events)
    {
        bool changed = before.TurnState != after.TurnState ||
                       before.CurrentActorId != after.CurrentActorId ||
                       before.StepsRemaining != after.StepsRemaining ||
                       before.StepsTotal != after.StepsTotal ||
                       before.IsPaused != after.IsPaused;
        if (!changed)
        {
            return;
        }

        string ownerPlayerId = ResolveOwnerPlayerId(after, after.CurrentActorId);
        events.Add(new ActionEventEnvelope(
            ActionEventType.TurnStateChanged,
            after.CurrentActorId,
            0,
            after.StepsTotal,
            0,
            0,
            0,
            (int)after.TurnState,
            after.StepsRemaining,
            after.IsPaused,
            false,
            ownerPlayerId,
            after.PauseReason,
            0));
    }

    private static void BuildMovedEvents(
        MatchStateSnapshot before,
        MatchStateSnapshot after,
        List<ActionEventEnvelope> events)
    {
        Dictionary<int, ActorStateSnapshot> beforeById = BuildActorMap(before);
        Dictionary<int, ActorStateSnapshot> afterById = BuildActorMap(after);

        foreach (var kv in afterById)
        {
            if (!beforeById.TryGetValue(kv.Key, out ActorStateSnapshot beforeActor))
            {
                continue;
            }

            ActorStateSnapshot afterActor = kv.Value;
            if (!beforeActor.IsAlive || !afterActor.IsAlive)
            {
                continue;
            }

            if (beforeActor.CellX == afterActor.CellX && beforeActor.CellY == afterActor.CellY)
            {
                continue;
            }

            events.Add(new ActionEventEnvelope(
                ActionEventType.ActorMoved,
                afterActor.ActorId,
                0,
                beforeActor.CellX,
                beforeActor.CellY,
                afterActor.CellX,
                afterActor.CellY,
                0,
                0,
                false,
                false,
                afterActor.ActorType ?? string.Empty,
                afterActor.OwnerPlayerId ?? string.Empty,
                DefaultMoveDurationMs));
        }
    }

    private static void BuildOpenedCellEvents(
        MatchStateSnapshot before,
        MatchStateSnapshot after,
        List<ActionEventEnvelope> events)
    {
        var openedBefore = new HashSet<string>();
        if (before.OpenedCells != null)
        {
            for (int i = 0; i < before.OpenedCells.Count; i++)
            {
                OpenedCellSnapshot cell = before.OpenedCells[i];
                openedBefore.Add($"{cell.X}:{cell.Y}");
            }
        }

        if (after.OpenedCells == null)
        {
            return;
        }

        for (int i = 0; i < after.OpenedCells.Count; i++)
        {
            OpenedCellSnapshot cell = after.OpenedCells[i];
            string key = $"{cell.X}:{cell.Y}";
            if (openedBefore.Contains(key))
            {
                continue;
            }

            int typeValue = Enum.TryParse(cell.CellType, true, out CellType parsedType)
                ? (int)parsedType
                : (int)CellType.Common;
            events.Add(new ActionEventEnvelope(
                ActionEventType.CellOpened,
                0,
                0,
                cell.X,
                cell.Y,
                cell.X,
                cell.Y,
                typeValue,
                0,
                true,
                true,
                string.Empty,
                cell.CellType ?? string.Empty,
                DefaultCellOpenDurationMs));
        }
    }

    private static void BuildEnemySpawnDespawnEvents(
        MatchStateSnapshot before,
        MatchStateSnapshot after,
        List<ActionEventEnvelope> events)
    {
        Dictionary<int, ActorStateSnapshot> beforeById = BuildActorMap(before);
        Dictionary<int, ActorStateSnapshot> afterById = BuildActorMap(after);

        foreach (var kv in afterById)
        {
            ActorStateSnapshot actor = kv.Value;
            if (!string.Equals(actor.ActorType, "enemy", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!beforeById.ContainsKey(actor.ActorId) && actor.IsAlive)
            {
                events.Add(new ActionEventEnvelope(
                    ActionEventType.EnemySpawned,
                    actor.ActorId,
                    0,
                    actor.CellX,
                    actor.CellY,
                    actor.CellX,
                    actor.CellY,
                    actor.Level,
                    actor.Health,
                    actor.IsAlive,
                    false,
                    actor.DisplayName ?? string.Empty,
                    actor.CharacterId ?? string.Empty,
                    0));
            }
        }

        foreach (var kv in beforeById)
        {
            ActorStateSnapshot actor = kv.Value;
            if (!string.Equals(actor.ActorType, "enemy", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (afterById.ContainsKey(actor.ActorId))
            {
                continue;
            }

            events.Add(new ActionEventEnvelope(
                ActionEventType.EnemyDespawned,
                actor.ActorId,
                0,
                actor.CellX,
                actor.CellY,
                actor.CellX,
                actor.CellY,
                actor.Level,
                actor.Health,
                false,
                false,
                actor.DisplayName ?? string.Empty,
                actor.CharacterId ?? string.Empty,
                0));
        }
    }

    private static void BuildAttackEvent(
        GameCommandEnvelope command,
        MatchStateSnapshot before,
        MatchStateSnapshot after,
        List<ActionEventEnvelope> events)
    {
        if (command.CommandType != GameCommandType.Attack || command.TargetActorId <= 0)
        {
            return;
        }

        Dictionary<int, ActorStateSnapshot> beforeById = BuildActorMap(before);
        Dictionary<int, ActorStateSnapshot> afterById = BuildActorMap(after);
        if (!beforeById.TryGetValue(command.TargetActorId, out ActorStateSnapshot beforeTarget))
        {
            return;
        }

        if (!afterById.TryGetValue(command.TargetActorId, out ActorStateSnapshot afterTarget))
        {
            afterTarget = beforeTarget;
        }

        int hpBefore = Math.Max(0, beforeTarget.Health);
        int hpAfter = Math.Max(0, afterTarget.Health);
        int damage = Math.Max(0, hpBefore - hpAfter);
        bool dodged = damage == 0;
        bool killed = !afterTarget.IsAlive;
        int sourceActorId = ResolveSourceActorId(before, command.PlayerId);

        events.Add(new ActionEventEnvelope(
            ActionEventType.AttackResolved,
            sourceActorId,
            command.TargetActorId,
            beforeTarget.CellX,
            beforeTarget.CellY,
            afterTarget.CellX,
            afterTarget.CellY,
            hpAfter,
            damage,
            dodged,
            killed,
            beforeTarget.DisplayName ?? string.Empty,
            afterTarget.DisplayName ?? string.Empty,
            DefaultAttackDurationMs));
    }

    private static Dictionary<int, ActorStateSnapshot> BuildActorMap(MatchStateSnapshot snapshot)
    {
        var map = new Dictionary<int, ActorStateSnapshot>();
        if (snapshot.Actors == null)
        {
            return map;
        }

        for (int i = 0; i < snapshot.Actors.Count; i++)
        {
            ActorStateSnapshot actor = snapshot.Actors[i];
            if (actor.ActorId <= 0)
            {
                continue;
            }

            map[actor.ActorId] = actor;
        }

        return map;
    }

    private static int ResolveSourceActorId(MatchStateSnapshot snapshot, string playerId)
    {
        if (snapshot.Actors == null || string.IsNullOrWhiteSpace(playerId))
        {
            return 0;
        }

        for (int i = 0; i < snapshot.Actors.Count; i++)
        {
            ActorStateSnapshot actor = snapshot.Actors[i];
            if (!string.Equals(actor.ActorType, "character", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (string.Equals(actor.OwnerPlayerId, playerId, StringComparison.Ordinal))
            {
                return actor.ActorId;
            }
        }

        return 0;
    }

    private static string ResolveOwnerPlayerId(MatchStateSnapshot snapshot, int actorId)
    {
        if (snapshot.Actors == null || actorId <= 0)
        {
            return string.Empty;
        }

        for (int i = 0; i < snapshot.Actors.Count; i++)
        {
            ActorStateSnapshot actor = snapshot.Actors[i];
            if (actor.ActorId == actorId)
            {
                return actor.OwnerPlayerId ?? string.Empty;
            }
        }

        return string.Empty;
    }
}
