using System;
using VContainer;
using VContainer.Unity;

public class TurnFlow : IPostInitializable, IDisposable
{
    private readonly IEventBus _eventBus;
    private readonly IRandomSource _randomSource;
    private readonly CrownOwnershipService _crownOwnershipService;
    private readonly IMatchRuntimeRoleService _runtimeRoleService;

    private IDisposable _stepSubscription;
    private IDisposable _endTurnSubscription;
    private IDisposable _openCellSubscription;
    public TurnState State { get; private set; }

    public int StepsAvailable { get; private set; }
    public int StepsRemaining { get; private set; }

    public ICellLayoutOccupant CurrentActor { get; private set; }
    public bool HasAttacked => _hasAttacked;
    private bool _allowEndTurn = false;
    private bool _hasAttacked = false;
    private bool _hasOpenedCell = false;

    public TurnFlow(
        IEventBus eventBus,
        IRandomSource randomSource,
        CrownOwnershipService crownOwnershipService,
        IMatchRuntimeRoleService runtimeRoleService)
    {
        _eventBus = eventBus;
        _randomSource = randomSource;
        _crownOwnershipService = crownOwnershipService;
        _runtimeRoleService = runtimeRoleService;
    }

    public void PostInitialize()
    {
        _stepSubscription      = _eventBus.Subscribe<CharacterMoved>(OnCharacterMoved);
        _endTurnSubscription   = _eventBus.Subscribe<EndTurnRequested>(OnEndTurnKeyPressed);
        _openCellSubscription = _eventBus.Subscribe<OpenCellRequested>(OnOpenCellRequested);
    }

    public void Dispose()
    {
        _stepSubscription?.Dispose();
        _endTurnSubscription?.Dispose();
        _openCellSubscription?.Dispose();
    }

    public void StartTurn(ICellLayoutOccupant actor)
    {
        ResetTurn();
        CurrentActor = actor;

        RollDice();
    }

    public void RollDice()
    {
        SetState(TurnState.RollDice);

        int diceRoll = _randomSource.Range(1, 7);
        int steps = CurrentActor?.Entity?.CalculateTurnSteps(diceRoll) ?? diceRoll;
        StepsAvailable = System.Math.Max(0, steps);
        StepsRemaining = StepsAvailable;

        _eventBus.Publish(new DiceRolled(CurrentActor, StepsAvailable));

        _allowEndTurn = true;
        SetState(TurnState.ActionSelection);
    }

    public void RegisterStep()
    {
        if (CurrentActor?.Entity == null)
            return;

        if (!CanMove())
        {
            return;
        }

        if (StepsRemaining <= 0)
        {
            return;
        }

        var currentCell = CurrentActor.Entity.CurrentCell;
        if (currentCell == null)
        {
            return;
        }

        if (CurrentActor is CharacterInstance && _crownOwnershipService.TryFinishGame(CurrentActor))
        {
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

        if (msg.Actor != CurrentActor)
            return;

        if (!CanMove())
        {
            return;
        }

        SetState(TurnState.Movement);
        RegisterStep();

        if (CurrentActor != null && State != TurnState.End)
        {
            SetState(TurnState.ActionSelection);
        }
    }

    private void OnEndTurnKeyPressed(EndTurnRequested msg)
    {
        if (_runtimeRoleService != null && _runtimeRoleService.IsOnlineMatch)
        {
            return;
        }

        if (!_allowEndTurn)
        {
            return;
        }
        _allowEndTurn = false;

        if (CurrentActor == null)
            return;

        if (State == TurnState.ActionSelection ||
            State == TurnState.Movement ||
            State == TurnState.Attack ||
            State == TurnState.OpenCell ||
            State == TurnState.Trade)
        {
            EndTurn();
        }
    }

    private void OnOpenCellRequested(OpenCellRequested msg)
    {
        if (_runtimeRoleService != null && _runtimeRoleService.IsOnlineMatch)
        {
            return;
        }

        TryOpenCell();
    }

    public bool TryEndTurnByAuthority()
    {
        if (!_allowEndTurn || CurrentActor == null)
        {
            return false;
        }

        if (State != TurnState.ActionSelection &&
            State != TurnState.Movement &&
            State != TurnState.Attack &&
            State != TurnState.OpenCell &&
            State != TurnState.Trade)
        {
            return false;
        }

        _allowEndTurn = false;
        EndTurn();
        return true;
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

    public bool TryOpenCell()
    {
        if (!IsOpenCellAvailable())
        {
            return false;
        }

        _hasOpenedCell = true;
        SetState(TurnState.OpenCell);
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
        return StepsRemaining > 0;
    }

    private bool IsActionPhase()
    {
        return State == TurnState.ActionSelection ||
               State == TurnState.Movement ||
               State == TurnState.Attack ||
               State == TurnState.OpenCell ||
               State == TurnState.Trade;
    }

    private bool IsOpenCellAvailable()
    {
        if (!IsActionPhase() || _hasOpenedCell)
        {
            return false;
        }

        var currentCell = CurrentActor?.Entity?.CurrentCell;
        if (currentCell == null)
        {
            return false;
        }

        if (currentCell.Type == CellType.Start)
        {
            return false;
        }

        return !currentCell.IsOpened;
    }

    private void ResetTurn()
    {
        CurrentActor = null;
        StepsAvailable = 0;
        StepsRemaining = 0;
        _allowEndTurn = false;
        _hasAttacked = false;
        _hasOpenedCell = false;
        PublishOpenCellAvailability(false);
    }

    private void SetState(TurnState newState)
    {
        State = newState;

        _eventBus.Publish(new TurnPhaseChanged(CurrentActor, newState));
        PublishOpenCellAvailability(IsOpenCellAvailable());
    }

    private void PublishOpenCellAvailability(bool isAvailable)
    {
        _eventBus.Publish(new OpenCellAvailabilityChanged(isAvailable));
    }
}
