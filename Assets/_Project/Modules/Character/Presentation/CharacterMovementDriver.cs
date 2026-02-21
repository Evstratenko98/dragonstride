using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class CharacterMovementDriver : IPostInitializable, ITickable, IDisposable
{
    private readonly CharacterRoster _characterRoster;
    private readonly IEventBus _eventBus;
    private readonly CharacterInputReader _input;
    private readonly TurnActorRegistry _turnActorRegistry;
    private readonly EnemySpawner _enemySpawner;
    private readonly IMatchRuntimeRoleService _runtimeRoleService;
    private readonly IActorIdentityService _actorIdentityService;

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
        TurnActorRegistry turnActorRegistry,
        EnemySpawner enemySpawner,
        IMatchRuntimeRoleService runtimeRoleService,
        IActorIdentityService actorIdentityService
    )
    {
        _characterRoster = characterRoster;
        _eventBus = eventBus;
        _input = input;
        _turnActorRegistry = turnActorRegistry;
        _enemySpawner = enemySpawner;
        _runtimeRoleService = runtimeRoleService;
        _actorIdentityService = actorIdentityService;
    }

    public void PostInitialize()
    {
        _turnStateSubscription = _eventBus.Subscribe<TurnPhaseChanged>(OnTurnStateChanged);
        _diceRolledSubscription = _eventBus.Subscribe<DiceRolled>(OnDiceRolled);
        _characterMovedSubscription = _eventBus.Subscribe<CharacterMoved>(OnCharacterMoved);
        _input.StartListening();
    }

    public IReadOnlyList<CharacterInstance> SpawnCharacters(Cell startCell, IReadOnlyList<CharacterSpawnRequest> spawnRequests)
    {
        if (spawnRequests == null || spawnRequests.Count == 0)
        {
            return _characterRoster.AllCharacters;
        }

        for (int i = 0; i < spawnRequests.Count; i++)
        {
            CharacterSpawnRequest request = spawnRequests[i];
            CharacterInstance character = _characterRoster.CreateCharacter(startCell, request);
            if (character != null)
            {
                _turnActorRegistry.Register(character);
                _actorIdentityService?.GetOrAssign(character);
            }
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

    }

    public void Dispose()
    {
        _turnStateSubscription?.Dispose();
        _diceRolledSubscription?.Dispose();
        _characterMovedSubscription?.Dispose();
    }

    public async void Tick()
    {
        if (_runtimeRoleService != null && _runtimeRoleService.IsOnlineMatch)
        {
            return;
        }

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

    public async Task<bool> TryExecuteCommandMoveAsync(Vector2Int direction)
    {
        if (_currentCharacter == null)
        {
            return false;
        }

        if (_currentTurnState != TurnState.ActionSelection && _currentTurnState != TurnState.Movement)
        {
            return false;
        }

        if (_movementBlocked || _stepsRemaining <= 0)
        {
            return false;
        }

        if (_characterRoster.IsMoving || direction == Vector2Int.zero)
        {
            return false;
        }

        await _characterRoster.TryMove(_currentCharacter, direction);
        return true;
    }

    public void Reset()
    {
        _currentActor = null;
        _currentCharacter = null;
        _currentTurnState = TurnState.None;
        _stepsRemaining = 0;
        _movementBlocked = false;
        _enemySpawner.Reset();
        _characterRoster.RemoveAllCharacters();
        _turnActorRegistry.Clear();
        _actorIdentityService?.Clear();
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

}
    
