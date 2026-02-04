using System;
using System.Collections.Generic;
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

    private IReadOnlyList<CharacterInstance> _players = Array.Empty<CharacterInstance>();
    private int _currentPlayerIndex = -1;

    public GameState GameState { get; private set; } = GameState.Init;
    public GameTurnState GameTurnState { get; private set; } = GameTurnState.Init;

    public IReadOnlyList<CharacterInstance> Players => _players;
    public CharacterInstance CurrentPlayer =>
        (_currentPlayerIndex < 0 || _currentPlayerIndex >= _players.Count)
        ? null
        : _players[_currentPlayerIndex];

    public GameFlow(
        IEventBus eventBus,
        TurnFlow turnFlow,
        FieldPresenter fieldPresenter,
        CharacterMovementDriver characterDriver
    )
    {
        _eventBus = eventBus;
        _fieldPresenter = fieldPresenter;
        _characterDriver = characterDriver;
        _turnFlow = turnFlow;
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

        _fieldPresenter.CreateField();
        Cell startCell = _fieldPresenter.StartCell;

        _players = _characterDriver.SpawnCharacters(startCell);
        _eventBus.Publish(new CharacterRosterUpdated(_players));

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
        _currentPlayerIndex = 0;

        StartTurn();
    }

    private void StartTurn()
    {
        GameTurnState = GameTurnState.CharacterTurns;

        _turnFlow.StartTurn(CurrentPlayer);
    }

    public void OnEndTurn(TurnEnded msg)
    {
        if (GameState == GameState.Finished || _players == null || _players.Count == 0)
        {
            return;
        }
        
        NextPlayer();
    }

    public void NextPlayer()
    {
        _currentPlayerIndex++;

        if (_currentPlayerIndex >= _players.Count)
        {
            StartTurnCycle();
            return;
        }

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
    }

    private void SetGameState(GameState newState)
    {
        GameState = newState;
        
        _eventBus.Publish(new GameStateChanged(newState));
    }
}
