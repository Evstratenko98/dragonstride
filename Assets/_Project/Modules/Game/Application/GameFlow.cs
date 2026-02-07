using System;
using System.Collections.Generic;
using System.Linq;
using VContainer;
using VContainer.Unity;

public class GameFlow : IPostInitializable, IDisposable, IStartable
{
    private readonly IEventBus _eventBus;
    private IDisposable _turnEndSub;
    private IDisposable _gameStateSub;
    private IDisposable _resetButtonSub;

    private readonly FieldPresenter _fieldPresenter;
    private readonly CharacterMovementDriver _characterDriver;
    private readonly TurnFlow _turnFlow;
    private readonly TurnActorRegistry _turnActorRegistry;

    private IReadOnlyList<Entity> _turnEntities = Array.Empty<Entity>();
    private ICellLayoutOccupant _currentTurnActor;

    public GameState GameState { get; private set; } = GameState.Init;
    public GameTurnState GameTurnState { get; private set; } = GameTurnState.Init;

    public IReadOnlyList<Entity> TurnEntities => _turnEntities;
    public ICellLayoutOccupant CurrentTurnActor => _currentTurnActor;

    public GameFlow(
        IEventBus eventBus,
        TurnFlow turnFlow,
        FieldPresenter fieldPresenter,
        CharacterMovementDriver characterDriver,
        TurnActorRegistry turnActorRegistry
    )
    {
        _eventBus = eventBus;
        _fieldPresenter = fieldPresenter;
        _characterDriver = characterDriver;
        _turnFlow = turnFlow;
        _turnActorRegistry = turnActorRegistry;
    }

    public void PostInitialize()
    {
        _turnEndSub = _eventBus.Subscribe<TurnEnded>(OnEndTurn);
        _gameStateSub = _eventBus.Subscribe<GameStateChanged>(OnStateGame);
        _resetButtonSub = _eventBus.Subscribe<ResetRequested>(OnStartGame);
    }

    public void Dispose()
    {
        _turnEndSub?.Dispose();
        _gameStateSub?.Dispose();
        _resetButtonSub?.Dispose();
    }

    public void OnStartGame(ResetRequested msg)
    {
        Start();
    }

    public void Start()
    {
        SetGameState(GameState.Loading);

        _characterDriver.Reset();
        _fieldPresenter.CreateField();
        Cell startCell = _fieldPresenter.StartCell;

        var characters = _characterDriver.SpawnCharacters(startCell);
        _eventBus.Publish(new CharacterRosterUpdated(characters));
        UpdateTurnEntities();

        StartGame();
    }

    public void StartGame()
    {
        SetGameState(GameState.Playing);

        StartTurnCycle();
    }

    public void StartTurnCycle()
    {
        GameTurnState = GameTurnState.BeginTurn;
        _currentTurnActor = null;

        NextTurnActor();
    }

    private void StartTurn()
    {
        GameTurnState = GameTurnState.CharacterTurns;

        _turnFlow.StartTurn(_currentTurnActor);
    }

    public void OnEndTurn(TurnEnded msg)
    {
        if (GameState == GameState.Finished)
        {
            return;
        }
        
        NextTurnActor();
    }

    public void NextTurnActor()
    {
        var activeActors = _turnActorRegistry.GetActiveActorsSnapshot();
        _turnEntities = activeActors
            .Select(actor => actor.Entity)
            .Where(entity => entity != null)
            .ToArray();

        if (activeActors.Count == 0)
        {
            _currentTurnActor = null;
            return;
        }

        int nextIndex = 0;
        if (_currentTurnActor != null)
        {
            int currentIndex = activeActors.IndexOf(_currentTurnActor);
            nextIndex = currentIndex >= 0 ? currentIndex + 1 : 0;
            if (nextIndex >= activeActors.Count)
            {
                GameTurnState = GameTurnState.BeginTurn;
                nextIndex = 0;
            }
        }

        _currentTurnActor = activeActors[nextIndex];
        StartTurn();
    }

    private void OnStateGame(GameStateChanged msg)
    {
        if(msg.State == GameState.Finished)
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
    }

    private void SetGameState(GameState newState)
    {
        GameState = newState;
        
        _eventBus.Publish(new GameStateChanged(newState));
    }

    private void UpdateTurnEntities()
    {
        _turnEntities = _turnActorRegistry.GetActiveActorsSnapshot()
            .Select(actor => actor.Entity)
            .Where(entity => entity != null)
            .ToArray();
    }
}
