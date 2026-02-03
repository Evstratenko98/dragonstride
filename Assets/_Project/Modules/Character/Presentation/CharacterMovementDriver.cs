using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class CharacterMovementDriver : IPostInitializable, ITickable, IDisposable
{
    private readonly CharacterRoster _characterRoster;
    private readonly IEventBus _eventBus;
    private readonly CharacterInputReader _input;

    private IDisposable _turnStateSubscription;
    private IDisposable _diceRolledSubscription;
    private IDisposable _characterMovedSubscription;

    private CharacterInstance _currentCharacter;
    private TurnState _currentTurnState = TurnState.None;
    private int _stepsRemaining = 0;
    private bool _movementBlocked = false;

    public CharacterMovementDriver(
        CharacterRoster characterRoster,
        IEventBus eventBus,
        CharacterInputReader input
    )
    {
        _characterRoster = characterRoster;
        _eventBus = eventBus;
        _input = input;
    }

    public void PostInitialize()
    {
        _turnStateSubscription = _eventBus.Subscribe<TurnPhaseChanged>(OnTurnStateChanged);
        _diceRolledSubscription = _eventBus.Subscribe<DiceRolled>(OnDiceRolled);
        _characterMovedSubscription = _eventBus.Subscribe<CharacterMoved>(OnCharacterMoved);
        _input.StartListening();
    }

    public IReadOnlyList<CharacterInstance> SpawnCharacters(Cell startCell)
    {   
        _characterRoster.CreateCharacter(startCell, "Arnoldo", 0, new SamuraiClass());
        _characterRoster.CreateCharacter(startCell, "Patrick", 1, new RunnerClass());
        _characterRoster.CreateCharacter(startCell, "Jonh", 2, new RunnerClass());

        return _characterRoster.AllCharacters;
    }

    private void OnTurnStateChanged(TurnPhaseChanged msg)
    {
        _currentCharacter = msg.Character;
        _currentTurnState = msg.State;

        if (msg.State == TurnState.RollDice || msg.State == TurnState.End || msg.State == TurnState.None)
        {
            _stepsRemaining = 0;
            _movementBlocked = false;
        }

        if (msg.State == TurnState.InteractionCell)
        {
            HandleCellInteraction(msg.Character);
            _movementBlocked = true;
            _stepsRemaining = 0;
        }
    }

    public void Dispose()
    {
        _turnStateSubscription?.Dispose();
        _diceRolledSubscription?.Dispose();
        _characterMovedSubscription?.Dispose();
    }

    public async void Tick()
    {
        if (_currentCharacter == null)
            return;

        if (_currentTurnState != TurnState.ActionSelection && _currentTurnState != TurnState.Movement)
            return;

        if (_movementBlocked || _stepsRemaining <= 0)
        {
            return;
        }

        Vector2Int dir = _input.Dir;
        if (_characterRoster.IsMoving || dir == Vector2Int.zero)
            return;

        
        await _characterRoster.TryMove(_currentCharacter, dir);
    }

    public void Reset()
    {
        _currentCharacter = null;
        _currentTurnState = TurnState.None;
        _stepsRemaining = 0;
        _movementBlocked = false;
        _characterRoster.RemoveAllCharacters();
    }

    private void OnDiceRolled(DiceRolled msg)
    {
        if (msg.Character != _currentCharacter)
        {
            return;
        }

        _stepsRemaining = msg.Steps;
        _movementBlocked = false;
    }

    private void OnCharacterMoved(CharacterMoved msg)
    {
        if (msg.Character != _currentCharacter)
        {
            return;
        }

        if (_stepsRemaining <= 0)
        {
            return;
        }

        _stepsRemaining--;
    }

    private void HandleCellInteraction(CharacterInstance character)
    {
        if (character == null)
        {
            return;
        }

        var currentCell = character.Model.CurrentCell;
        if (currentCell == null)
        {
            return;
        }

        if (currentCell.Type == CellType.Common)
        {
            return;
        }

        switch (currentCell.Type)
        {
            case CellType.Start:
            case CellType.Loot:
            case CellType.Fight:
            case CellType.Teleport:
            case CellType.End:
                // TODO: implement interaction logic for non-common cell types.
                break;
            default:
                return;
        }
    }
}
