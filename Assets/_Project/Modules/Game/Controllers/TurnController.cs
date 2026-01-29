using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class TurnController : IPostInitializable, IDisposable
{
    private readonly IEventBus _eventBus;

    private IDisposable _stepSubscription;
    private IDisposable _endTurnSubscription;
    public TurnState State { get; private set; }

    public int StepsAvailable { get; private set; }
    public int StepsRemaining { get; private set; }

    public CharacterInstance CurrentPlayer { get; private set; }
    private bool _allowEndTurn = false;

    public TurnController(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public void PostInitialize()
    {
        _stepSubscription      = _eventBus.Subscribe<CharacterMovedMessage>(OnCharacterMoved);
        _endTurnSubscription   = _eventBus.Subscribe<EndTurnKeyPressedMessage>(OnEndTurnKeyPressed);
    }

    public void Dispose()
    {
        _stepSubscription?.Dispose();
        _endTurnSubscription?.Dispose();
    }
    // ---------------- PUBLIC API ----------------

    public void StartTurn(CharacterInstance character)
    {
        CurrentPlayer = character;
        StepsAvailable = 0;
        StepsRemaining = 0;

        SetState(TurnState.Start);

        RollDice();
    }

    public void RollDice()
    {
        SetState(TurnState.RollDice);

        StepsAvailable = UnityEngine.Random.Range(1, 7);
        StepsRemaining = StepsAvailable;

        Debug.Log($"[TurnController] Игроку {CurrentPlayer.Name} выпало {StepsAvailable} шагов!");

        _eventBus.Publish(new DiceRolledMessage(CurrentPlayer, StepsAvailable));

        StartMovement();
    }

    public void StartMovement()
    {
        _allowEndTurn = true;
        SetState(TurnState.Movement);
    }

    public void RegisterStep()
    {
        if (State != TurnState.Movement)
            return;

        if (CurrentPlayer == null)
            return;

        if (StepsRemaining <= 0)
        {
            Debug.Log($"[TurnController] У игрока {CurrentPlayer.Name} больше нет шагов, движение заблокировано");
            return;
        }

        if (CurrentPlayer.Model.CurrentCell.Type == CellType.End)
        {
            Debug.Log($"[TurnController] Игрок {CurrentPlayer.Name} достиг финиша и победил!");
            _eventBus.Publish(new GameStateChangedMessage(GameState.Finished));

            return;
        }

        StepsRemaining--;
        Debug.Log($"[TurnController] Игрок {CurrentPlayer.Name} сделал шаг. Осталось шагов: {StepsRemaining}");

        if (StepsRemaining <= 0)
        {
            Debug.Log($"[TurnController] Игрок {CurrentPlayer.Name} израсходовал все шаги. Переход к взаимодействиям.");
            StartInteractions();
        }
    }

    public void StartInteractions()
    {
        if (CurrentPlayer == null)
            return;

        SetState(TurnState.InteractionCells);

        // TODO: логика взаимодействия с клетками

        StartPlayerInteractions();
    }

    private void StartPlayerInteractions()
    {
        if (CurrentPlayer == null)
            return;

        SetState(TurnState.InteractionPlayers);

        Debug.Log($"[TurnController] {CurrentPlayer.Name} нажмите пробел для завершения хода");
    }

    public void EndTurn()
    {
        CurrentPlayer = null;
        StepsAvailable = 0;
        StepsRemaining = 0;

        SetState(TurnState.End);
        _eventBus.Publish(new TurnEndedMessage());
    }

    // ---------------- EVENT HANDLERS ----------------

    private void OnCharacterMoved(CharacterMovedMessage msg)
    {
        if (State != TurnState.Movement)
            return;

        if (msg.Character != CurrentPlayer)
            return;

        RegisterStep();
    }

    private void OnEndTurnKeyPressed(EndTurnKeyPressedMessage msg)
    {
        if (!_allowEndTurn)
        {
            return;
        }
        _allowEndTurn = false;

        if (CurrentPlayer == null)
            return;

        if (State == TurnState.Movement ||
            State == TurnState.InteractionCells ||
            State == TurnState.InteractionPlayers)
        {
            Debug.Log($"[TurnController] Игрок {CurrentPlayer.Name} нажал пробел для завершения хода");

            if (State == TurnState.Movement && StepsRemaining > 0)
            {
                Debug.Log($"[TurnController] Игрок {CurrentPlayer.Name} завершает движение досрочно. Остаток шагов: {StepsRemaining} → 0");
                StepsRemaining = 0;
                StartInteractions();
            }

            EndTurn();
        }
    }

    // ---------------- STATE MACHINE HELPER ----------------

    private void SetState(TurnState newState)
    {
        State = newState;
        
        _eventBus.Publish(new TurnStateChangedMessage(CurrentPlayer, newState));
        Debug.Log($"[TurnController] Состояние хода игрока {CurrentPlayer?.Name}: {newState}");       
    }
}
