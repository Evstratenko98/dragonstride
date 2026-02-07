using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class CharacterMovementDriver : IPostInitializable, ITickable, IDisposable
{
    private readonly CharacterRoster _characterRoster;
    private readonly IEventBus _eventBus;
    private readonly CharacterInputReader _input;
    private readonly ItemFactory _itemFactory;
    private readonly FieldPresenter _fieldPresenter;
    private readonly TurnActorRegistry _turnActorRegistry;

    private IDisposable _turnStateSubscription;
    private IDisposable _diceRolledSubscription;
    private IDisposable _characterMovedSubscription;

    private ICellLayoutOccupant _currentActor;
    private CharacterInstance _currentCharacter;
    private TurnState _currentTurnState = TurnState.None;
    private int _stepsRemaining = 0;
    private bool _movementBlocked = false;

    public CharacterMovementDriver(
        CharacterRoster characterRoster,
        IEventBus eventBus,
        CharacterInputReader input,
        ItemFactory itemFactory,
        FieldPresenter fieldPresenter,
        TurnActorRegistry turnActorRegistry
    )
    {
        _characterRoster = characterRoster;
        _eventBus = eventBus;
        _input = input;
        _itemFactory = itemFactory;
        _fieldPresenter = fieldPresenter;
        _turnActorRegistry = turnActorRegistry;
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

        foreach (var character in _characterRoster.AllCharacters)
        {
            _turnActorRegistry.Register(character);
        }

        return _characterRoster.AllCharacters;
    }

    private void OnTurnStateChanged(TurnPhaseChanged msg)
    {
        _currentActor = msg.Actor;
        _currentCharacter = msg.Actor as CharacterInstance;
        _currentTurnState = msg.State;

        if (msg.State == TurnState.RollDice || msg.State == TurnState.End || msg.State == TurnState.None)
        {
            _stepsRemaining = 0;
            _movementBlocked = false;
        }

        if (msg.State == TurnState.InteractionCell)
        {
            HandleCellInteraction(msg.Actor);
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
        _currentActor = null;
        _currentCharacter = null;
        _currentTurnState = TurnState.None;
        _stepsRemaining = 0;
        _movementBlocked = false;
        _characterRoster.RemoveAllCharacters();
        _turnActorRegistry.Clear();
    }

    private void OnDiceRolled(DiceRolled msg)
    {
        if (msg.Actor != _currentActor)
        {
            return;
        }

        _stepsRemaining = msg.Steps;
        _movementBlocked = false;
    }

    private void OnCharacterMoved(CharacterMoved msg)
    {
        if (msg.Actor != _currentActor)
        {
            return;
        }

        if (_stepsRemaining <= 0)
        {
            return;
        }

        _stepsRemaining--;
    }

    private void HandleCellInteraction(ICellLayoutOccupant actor)
    {
        var entity = actor?.Entity;
        if (entity == null)
        {
            return;
        }

        var currentCell = entity.CurrentCell;
        if (currentCell == null)
        {
            return;
        }

        if (currentCell.Type == CellType.Common)
        {
            return;
        }

        bool shouldConsumeCell = false;

        switch (currentCell.Type)
        {
            case CellType.Start:
            case CellType.End:
                return;
            case CellType.Loot:
                if (actor is CharacterInstance character)
                {
                    int lootCount = UnityEngine.Random.Range(1, 4);
                    var loot = new List<ItemDefinition>(lootCount);
                    for (int i = 0; i < lootCount; i++)
                    {
                        var item = _itemFactory.CreateRandomChestLoot();
                        if (item?.Definition != null)
                        {
                            loot.Add(item.Definition);
                        }
                    }

                    if (loot.Count > 0)
                    {
                        _eventBus.Publish(new ChestLootOpened(character, loot));
                    }
                }
                shouldConsumeCell = true;
                break;
            case CellType.Fight:
            case CellType.Teleport:
                // TODO: implement interaction logic for non-common cell types.
                shouldConsumeCell = true;
                break;
            default:
                return;
        }

        if (shouldConsumeCell)
        {
            currentCell.SetType(CellType.Common);
            _fieldPresenter.RefreshCell(currentCell);
        }
    }
}
