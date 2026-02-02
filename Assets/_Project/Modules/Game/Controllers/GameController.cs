using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class GameController : IPostInitializable, IDisposable, IStartable
{
    private readonly IEventBus _eventBus;
    private IDisposable _turnEndSub;
    private IDisposable _gameStateSub;
    private IDisposable _resetButtonSub;

    // Controllers
    private FieldPresenter _fieldController;
    private CharacterMovementDriver _characterController;
    private TurnController _turnController;

    private IReadOnlyList<CharacterInstance> _players;
    private int _currentPlayerIndex = -1;

    public GameState GameState { get; private set; } = GameState.Init;
    public GameTurnState GameTurnState { get; private set; } = GameTurnState.Init;

    public IReadOnlyList<CharacterInstance> Players => _players;
    public CharacterInstance CurrentPlayer =>
        (_currentPlayerIndex < 0 || _currentPlayerIndex >= _players.Count)
        ? null
        : _players[_currentPlayerIndex];

    public GameController(
        IEventBus eventBus, 
        TurnController turnController,
        FieldPresenter fieldController,
        CharacterMovementDriver characterController    
    )
    {
        _eventBus = eventBus;
        _fieldController = fieldController;
        _characterController = characterController;
        _turnController = turnController;
    }

    // ==========================================================
    // INIT
    // ==========================================================
    public void PostInitialize()
    {
        // стадии игрового хода
        _turnEndSub = _eventBus.Subscribe<TurnEnded>(OnEndTurn);
        // стадии игры
        _gameStateSub = _eventBus.Subscribe<GameStateChanged>(OnStateGame);
        // нажатие кнопки рестарта
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
        Debug.Log("[GameController] Игра загружается...");
        SetGameState(GameState.Loading);

        _fieldController.CreateField();
        Cell startCell = _fieldController.StartCell;

        _players = _characterController.SpawnCharacters(startCell);

        StartGame();
    }

    public void StartGame()
    {
        Debug.Log("[GameController] Игра начинается!");
        SetGameState(GameState.Playing);

        StartTurnCycle();
    }

    // ==========================================================
    // TURN CYCLE
    // ==========================================================
    public void StartTurnCycle()
    {
        //TODO: Тут будет обработка нового глобального хода
        GameTurnState = GameTurnState.BeginTurn;
        // начинаем с первого игрока
        _currentPlayerIndex = 0;

        StartTurn();
    }

    private void StartTurn()
    {
        GameTurnState = GameTurnState.CharacterTurns;
        Debug.Log($"[GameController] Ход игрока: {CurrentPlayer.Name}");

       _turnController.StartTurn(CurrentPlayer);
    }

    public void OnEndTurn(TurnEnded msg)
    {
        if (GameState == GameState.Finished || _players == null || _players.Count == 0)
        {
            return;
        }
        
        NextPlayer();
    }

    // ==========================================================
    // NEXT PLAYER / NEXT ROUND
    // ==========================================================
    public void NextPlayer()
    {
        _currentPlayerIndex++;

        if (_currentPlayerIndex >= _players.Count)
        {
            Debug.Log("[GameController] Все игроки походили — новый раунд");
            StartTurnCycle();   // начало нового раунда
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
        _characterController.Reset();
        _fieldController.Reset();
    }

    private void SetGameState(GameState newState)
    {
        GameState = newState;
        
        _eventBus.Publish(new GameStateChanged(newState));
    }
}
