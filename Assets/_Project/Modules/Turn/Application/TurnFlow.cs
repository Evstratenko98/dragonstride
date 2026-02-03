using System;
using VContainer;
using VContainer.Unity;

public class TurnFlow : IPostInitializable, IDisposable
{
    private readonly IEventBus _eventBus;
    private readonly IRandomSource _randomSource;

    private IDisposable _stepSubscription;
    private IDisposable _endTurnSubscription;
    private IDisposable _interactCellSubscription;
    public TurnState State { get; private set; }

    public int StepsAvailable { get; private set; }
    public int StepsRemaining { get; private set; }

    public CharacterInstance CurrentPlayer { get; private set; }
    private bool _allowEndTurn = false;
    private bool _hasAttacked = false;
    private bool _hasInteractedCell = false;

    public TurnFlow(IEventBus eventBus, IRandomSource randomSource)
    {
        _eventBus = eventBus;
        _randomSource = randomSource;
    }

    public void PostInitialize()
    {
        _stepSubscription      = _eventBus.Subscribe<CharacterMoved>(OnCharacterMoved);
        _endTurnSubscription   = _eventBus.Subscribe<EndTurnRequested>(OnEndTurnKeyPressed);
        _interactCellSubscription = _eventBus.Subscribe<InteractWithCellRequested>(OnInteractCellRequested);
    }

    public void Dispose()
    {
        _stepSubscription?.Dispose();
        _endTurnSubscription?.Dispose();
        _interactCellSubscription?.Dispose();
    }

    public void StartTurn(CharacterInstance character)
    {
        ResetTurn();
        CurrentPlayer = character;

        RollDice();
    }

    public void RollDice()
    {
        SetState(TurnState.RollDice);

        StepsAvailable = _randomSource.Range(1, 7);
        StepsRemaining = StepsAvailable;

        _eventBus.Publish(new DiceRolled(CurrentPlayer, StepsAvailable));

        _allowEndTurn = true;
        SetState(TurnState.ActionSelection);
    }

    public void RegisterStep()
    {
        if (CurrentPlayer == null)
            return;

        if (!CanMove())
        {
            return;
        }

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
    }

    public void EndTurn()
    {
        ResetTurn();

        SetState(TurnState.End);
        _eventBus.Publish(new TurnEnded());
    }

    private void OnCharacterMoved(CharacterMoved msg)
    {
        if (State != TurnState.ActionSelection && State != TurnState.Movement)
            return;

        if (msg.Character != CurrentPlayer)
            return;

        if (!CanMove())
        {
            return;
        }

        SetState(TurnState.Movement);
        RegisterStep();

        if (CurrentPlayer != null && State != TurnState.End)
        {
            SetState(TurnState.ActionSelection);
        }
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

        if (State == TurnState.ActionSelection ||
            State == TurnState.Movement ||
            State == TurnState.Attack ||
            State == TurnState.InteractionCell ||
            State == TurnState.Trade)
        {
            EndTurn();
        }
    }

    private void OnInteractCellRequested(InteractWithCellRequested msg)
    {
        TryInteractWithCell();
    }

    public bool TryAttack()
    {
        if (!IsActionPhase())
        {
            return false;
        }

        if (_hasAttacked)
        {
            return false;
        }

        _hasAttacked = true;
        SetState(TurnState.Attack);
        SetState(TurnState.ActionSelection);
        return true;
    }

    public bool TryInteractWithCell()
    {
        if (!IsActionPhase())
        {
            return false;
        }

        if (_hasInteractedCell)
        {
            return false;
        }

        _hasInteractedCell = true;
        SetState(TurnState.InteractionCell);
        SetState(TurnState.ActionSelection);
        return true;
    }

    public bool TryTrade()
    {
        if (!IsActionPhase())
        {
            return false;
        }

        SetState(TurnState.Trade);
        SetState(TurnState.ActionSelection);
        return true;
    }

    private bool CanMove()
    {
        return StepsRemaining > 0 && !_hasInteractedCell;
    }

    private bool IsActionPhase()
    {
        return State == TurnState.ActionSelection ||
               State == TurnState.Movement ||
               State == TurnState.Attack ||
               State == TurnState.InteractionCell ||
               State == TurnState.Trade;
    }

    private void ResetTurn()
    {
        CurrentPlayer = null;
        StepsAvailable = 0;
        StepsRemaining = 0;
        _allowEndTurn = false;
        _hasAttacked = false;
        _hasInteractedCell = false;
    }

    private void SetState(TurnState newState)
    {
        State = newState;

        _eventBus.Publish(new TurnPhaseChanged(CurrentPlayer, newState));
    }
}
