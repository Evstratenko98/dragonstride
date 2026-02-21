using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class GameFlow : IPostInitializable, IDisposable, IStartable
{
    private const int FieldSnapshotTimeoutMs = 20000;

    private readonly IEventBus _eventBus;
    private readonly IRandomSource _randomSource;
    private readonly IMatchRuntimeRoleService _runtimeRoleService;
    private readonly FieldPresenter _fieldPresenter;
    private readonly CharacterMovementDriver _characterDriver;
    private readonly TurnFlow _turnFlow;
    private readonly TurnActorRegistry _turnActorRegistry;
    private readonly IMultiplayerSessionService _sessionService;
    private readonly IMatchSetupContextService _matchSetupContextService;
    private readonly CharacterCatalog _characterCatalog;
    private readonly ISessionSceneRouter _sceneRouter;

    private readonly CancellationTokenSource _lifecycleCts = new();
    private IDisposable _turnEndSub;
    private IDisposable _gameStateSub;
    private IDisposable _resetButtonSub;
    private IDisposable _fieldSnapshotSub;

    private IReadOnlyList<Entity> _turnEntities = Array.Empty<Entity>();
    private ICellLayoutOccupant _currentTurnActor;
    private List<ICellLayoutOccupant> _roundActors = new();
    private int _roundActorIndex;

    private bool _isStarting;
    private bool _hasFieldSnapshot;
    private FieldGridSnapshot _latestFieldSnapshot;
    private TaskCompletionSource<FieldGridSnapshot> _fieldSnapshotTcs;

    public GameState GameState { get; private set; } = GameState.Init;
    public GameTurnState GameTurnState { get; private set; } = GameTurnState.Init;

    public IReadOnlyList<Entity> TurnEntities => _turnEntities;
    public ICellLayoutOccupant CurrentTurnActor => _currentTurnActor;

    public GameFlow(
        IEventBus eventBus,
        IRandomSource randomSource,
        IMatchRuntimeRoleService runtimeRoleService,
        TurnFlow turnFlow,
        FieldPresenter fieldPresenter,
        CharacterMovementDriver characterDriver,
        TurnActorRegistry turnActorRegistry,
        IMultiplayerSessionService sessionService,
        IMatchSetupContextService matchSetupContextService,
        CharacterCatalog characterCatalog,
        ISessionSceneRouter sceneRouter)
    {
        _eventBus = eventBus;
        _randomSource = randomSource;
        _runtimeRoleService = runtimeRoleService;
        _fieldPresenter = fieldPresenter;
        _characterDriver = characterDriver;
        _turnFlow = turnFlow;
        _turnActorRegistry = turnActorRegistry;
        _sessionService = sessionService;
        _matchSetupContextService = matchSetupContextService;
        _characterCatalog = characterCatalog;
        _sceneRouter = sceneRouter;
    }

    public void PostInitialize()
    {
        _turnEndSub = _eventBus.Subscribe<TurnEnded>(OnEndTurn);
        _gameStateSub = _eventBus.Subscribe<GameStateChanged>(OnStateGame);
        _resetButtonSub = _eventBus.Subscribe<ResetRequested>(OnStartGame);
        _fieldSnapshotSub = _eventBus.Subscribe<FieldSnapshotReceived>(OnFieldSnapshotReceived);
    }

    public void Dispose()
    {
        _turnEndSub?.Dispose();
        _gameStateSub?.Dispose();
        _resetButtonSub?.Dispose();
        _fieldSnapshotSub?.Dispose();

        if (!_lifecycleCts.IsCancellationRequested)
        {
            _lifecycleCts.Cancel();
        }

        _lifecycleCts.Dispose();
    }

    public void OnStartGame(ResetRequested msg)
    {
        _ = StartAsync(_lifecycleCts.Token);
    }

    public void Start()
    {
        _ = StartAsync(_lifecycleCts.Token);
    }

    private async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_isStarting)
        {
            return;
        }

        _isStarting = true;
        try
        {
            SetGameState(GameState.Loading);
            _randomSource.ResetSeed(ResolveMatchSeed());

            _characterDriver.Reset();

            bool fieldPrepared = await PrepareFieldAsync(cancellationToken);
            if (!fieldPrepared)
            {
                await AbortToMainMenuAsync("Field initialization failed. Returning to MainMenu.");
                return;
            }

            Cell startCell = _fieldPresenter.StartCell;
            IReadOnlyList<CharacterSpawnRequest> spawnRequests = ResolveSpawnRequests();
            if (spawnRequests == null || spawnRequests.Count == 0)
            {
                Debug.LogError("[GameFlow] Match setup context has no spawn requests. Returning to MainMenu.");
                await AbortToMainMenuAsync("Match setup is empty. Returning to MainMenu.");
                return;
            }

            var characters = _characterDriver.SpawnCharacters(startCell, spawnRequests);
            if (characters == null || characters.Count == 0)
            {
                Debug.LogError("[GameFlow] Character roster is empty after spawn. Returning to MainMenu.");
                _matchSetupContextService.Clear();
                await AbortToMainMenuAsync("Character spawn failed. Returning to MainMenu.");
                return;
            }

            _matchSetupContextService.ClearRoster();
            _eventBus.Publish(new CharacterRosterUpdated(characters));
            _turnEntities = Array.Empty<Entity>();
            _roundActors.Clear();
            _roundActorIndex = 0;

            StartGame();
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _isStarting = false;
        }
    }

    public void StartGame()
    {
        SetGameState(GameState.Playing);

        if (ShouldRunAuthoritativeTurnLoop())
        {
            StartTurnCycle();
        }
    }

    public void StartTurnCycle()
    {
        if (!ShouldRunAuthoritativeTurnLoop())
        {
            return;
        }

        GameTurnState = GameTurnState.BeginTurn;
        _currentTurnActor = null;
        BuildRoundOrder();
        StartTurnFromRoundOrder();
    }

    private void StartTurn()
    {
        if (!ShouldRunAuthoritativeTurnLoop())
        {
            return;
        }

        GameTurnState = GameTurnState.CharacterTurns;

        _turnFlow.StartTurn(_currentTurnActor);
    }

    public void OnEndTurn(TurnEnded msg)
    {
        if (!ShouldRunAuthoritativeTurnLoop() || GameState == GameState.Finished)
        {
            return;
        }

        _roundActorIndex++;
        StartTurnFromRoundOrder();
    }

    private void StartTurnFromRoundOrder()
    {
        if (_roundActors.Count == 0)
        {
            _currentTurnActor = null;
            return;
        }

        while (_roundActorIndex < _roundActors.Count)
        {
            var actor = _roundActors[_roundActorIndex];
            if (IsActorActive(actor))
            {
                _currentTurnActor = actor;
                StartTurn();
                return;
            }

            _roundActorIndex++;
        }

        StartTurnCycle();
    }

    private void OnStateGame(GameStateChanged msg)
    {
        if (msg.State == GameState.Finished)
        {
            FinishGame();
        }
    }

    private void FinishGame()
    {
        GameState = GameState.Finished;
        _characterDriver.Reset();
        _fieldPresenter.Reset();
        _turnEntities = Array.Empty<Entity>();
        _currentTurnActor = null;
        _roundActors.Clear();
        _roundActorIndex = 0;
    }

    private void SetGameState(GameState newState)
    {
        GameState = newState;
        _eventBus.Publish(new GameStateChanged(newState));
    }

    private void BuildRoundOrder()
    {
        var activeActors = _turnActorRegistry.GetActiveActorsSnapshot()
            .Where(IsActorActive)
            .ToList();

        _roundActors = BuildInitiativeOrder(activeActors);
        _roundActorIndex = 0;
        _turnEntities = _roundActors
            .Select(actor => actor.Entity)
            .Where(entity => entity != null)
            .ToArray();
    }

    private List<ICellLayoutOccupant> BuildInitiativeOrder(List<ICellLayoutOccupant> activeActors)
    {
        if (activeActors == null || activeActors.Count == 0)
        {
            return new List<ICellLayoutOccupant>();
        }

        return activeActors
            .GroupBy(actor => actor.Entity.Initiative)
            .OrderByDescending(group => group.Key)
            .SelectMany(group =>
            {
                var shuffled = group.ToList();
                Shuffle(shuffled);
                return shuffled;
            })
            .ToList();
    }

    private void Shuffle(List<ICellLayoutOccupant> actors)
    {
        for (int i = actors.Count - 1; i > 0; i--)
        {
            int j = _randomSource.Range(0, i + 1);
            (actors[i], actors[j]) = (actors[j], actors[i]);
        }
    }

    private static bool IsActorActive(ICellLayoutOccupant actor)
    {
        return actor?.Entity?.CurrentCell != null;
    }

    private IReadOnlyList<CharacterSpawnRequest> ResolveSpawnRequests()
    {
        if (_matchSetupContextService != null && _matchSetupContextService.HasPreparedRoster)
        {
            return _matchSetupContextService.GetSpawnRequests();
        }

        if (_sessionService != null && _sessionService.HasActiveSession)
        {
            return Array.Empty<CharacterSpawnRequest>();
        }

        CharacterDefinition fallbackDefinition = _characterCatalog?.GetFirstOrDefault();
        if (fallbackDefinition == null)
        {
            return Array.Empty<CharacterSpawnRequest>();
        }

        return new[]
        {
            new CharacterSpawnRequest("offline_local", fallbackDefinition.Id, "Player")
        };
    }

    private int ResolveMatchSeed()
    {
        if (_matchSetupContextService != null && _matchSetupContextService.MatchSeed != 0)
        {
            return _matchSetupContextService.MatchSeed;
        }

        if (_sessionService != null && _sessionService.HasActiveSession)
        {
            string sessionId = _sessionService.ActiveSession.SessionId;
            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                return sessionId.GetHashCode();
            }
        }

        return DateTime.UtcNow.GetHashCode();
    }

    private bool ShouldRunAuthoritativeTurnLoop()
    {
        return _runtimeRoleService == null || _runtimeRoleService.CanMutateWorld;
    }

    private async Task<bool> PrepareFieldAsync(CancellationToken cancellationToken)
    {
        if (_runtimeRoleService != null && _runtimeRoleService.IsClientReplica)
        {
            return await WaitForFieldSnapshotAndBuildAsync(cancellationToken);
        }

        _fieldPresenter.CreateField();
        return _fieldPresenter.StartCell != null;
    }

    private async Task<bool> WaitForFieldSnapshotAndBuildAsync(CancellationToken cancellationToken)
    {
        if (_hasFieldSnapshot)
        {
            return _fieldPresenter.CreateFieldFromSnapshot(_latestFieldSnapshot);
        }

        Debug.Log("[GameFlow] Waiting for host field snapshot...");
        _fieldSnapshotTcs = new TaskCompletionSource<FieldGridSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        Task delayTask = Task.Delay(FieldSnapshotTimeoutMs, cancellationToken);
        Task completed = await Task.WhenAny(_fieldSnapshotTcs.Task, delayTask);
        if (completed != _fieldSnapshotTcs.Task)
        {
            Debug.LogWarning("[GameFlow] Timed out while waiting for host field snapshot.");
            return false;
        }

        FieldGridSnapshot snapshot = await _fieldSnapshotTcs.Task;
        if (snapshot.Width <= 0 || snapshot.Height <= 0)
        {
            Debug.LogWarning("[GameFlow] Host field snapshot payload is invalid.");
            return false;
        }

        return _fieldPresenter.CreateFieldFromSnapshot(snapshot);
    }

    private void OnFieldSnapshotReceived(FieldSnapshotReceived message)
    {
        if (message.Snapshot.Width <= 0 || message.Snapshot.Height <= 0)
        {
            return;
        }

        _latestFieldSnapshot = message.Snapshot;
        _hasFieldSnapshot = true;
        Debug.Log($"[GameFlow] Host field snapshot received. Checksum={message.Snapshot.Checksum}");
        _fieldSnapshotTcs?.TrySetResult(message.Snapshot);
    }

    private async Task AbortToMainMenuAsync(string logMessage)
    {
        Debug.LogError($"[GameFlow] {logMessage}");
        _matchSetupContextService.Clear();
        await _sceneRouter.LoadMainMenuAsync();
    }
}
