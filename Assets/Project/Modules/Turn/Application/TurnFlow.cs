using System;
using VContainer;
using VContainer.Unity;

public class TurnFlow : IPostInitializable, IDisposable
{
    private readonly IEventBus _eventBus;
    private readonly IRandomSource _randomSource;

    private IDisposable _stepSubscription;
    private IDisposable _endTurnSubscription;
    public TurnState State { get; private set; }

    public int StepsAvailable { get; private set; }
    public int StepsRemaining { get; private set; }

    public CharacterInstance CurrentPlayer { get; private set; }
    private bool _allowEndTurn = false;

    public TurnFlow(IEventBus eventBus, IRandomSource randomSource)
    {
        _eventBus = eventBus;
        _randomSource = randomSource;
    }

    public void PostInitialize()
    {
        _stepSubscription      = _eventBus.Subscribe<CharacterMoved>(OnCharacterMoved);
        _endTurnSubscription   = _eventBus.Subscribe<EndTurnRequested>(OnEndTurnKeyPressed);
    }

    public void Dispose()
    {
        _stepSubscription?.Dispose();
        _endTurnSubscription?.Dispose();
    }

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

        StepsAvailable = _randomSource.Range(1, 7);
        StepsRemaining = StepsAvailable;

        _eventBus.Publish(new DiceRolled(CurrentPlayer, StepsAvailable));

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
            return;
        }

        if (CurrentPlayer.Model.CurrentCell.Type == CellType.End)
        {
            _eventBus.Publish(new GameStateChanged(GameState.Finished));

            return;
        }

        StepsRemaining--;

        if (StepsRemaining <= 0)
        {
            StartInteractions();
        }
    }

    public void StartInteractions()
    {
        if (CurrentPlayer == null)
            return;

        SetState(TurnState.InteractionCells);

        StartPlayerInteractions();
    }

    private void StartPlayerInteractions()
    {
        if (CurrentPlayer == null)
            return;

        SetState(TurnState.InteractionPlayers);
    }

    public void EndTurn()
    {
        CurrentPlayer = null;
        StepsAvailable = 0;
        StepsRemaining = 0;

        SetState(TurnState.End);
        _eventBus.Publish(new TurnEnded());
    }

    private void OnCharacterMoved(CharacterMoved msg)
    {
        if (State != TurnState.Movement)
            return;

        if (msg.Character != CurrentPlayer)
            return;

        RegisterStep();
    }

    private void OnEndTurnKeyPressed(EndTurnRequested msg)
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
            if (State == TurnState.Movement && StepsRemaining > 0)
            {
                StepsRemaining = 0;
                StartInteractions();
            }

            EndTurn();
        }
    }

    private void SetState(TurnState newState)
    {
        State = newState;

        _eventBus.Publish(new TurnPhaseChanged(CurrentPlayer, newState));
    }
}
