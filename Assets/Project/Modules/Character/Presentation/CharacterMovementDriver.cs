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

    private CharacterInstance _currentCharacter;
    private TurnState _currentTurnState = TurnState.None;

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
        // Логика стадий игрового хода
        _turnStateSubscription = _eventBus.Subscribe<TurnStateChangedMessage>(OnTurnStateChanged);
        _input.StartListening();
    }

    public IReadOnlyList<CharacterInstance> SpawnCharacters(Cell startCell)
    {   
        _characterRoster.CreateCharacter(startCell, "Arnoldo", 0, new SamuraiClass());
        _characterRoster.CreateCharacter(startCell, "Patrick", 1, new RunnerClass());
        _characterRoster.CreateCharacter(startCell, "Jonh", 2, new RunnerClass());

        return _characterRoster.AllCharacters;
    }

    private void OnTurnStateChanged(TurnStateChangedMessage msg)
    {
        _currentCharacter = msg.Character;
        _currentTurnState = msg.State;
    }

    public void Dispose()
    {
        _turnStateSubscription?.Dispose();
    }

    // -----------------------------
    //        TICK (WASD)
    // -----------------------------
    public async void Tick()
    {
        // Никого нет — никто не двигается
        if (_currentCharacter == null)
            return;

        // ВАЖНО: движение только в фазе Movement
        if (_currentTurnState != TurnState.Movement)
            return;

        Vector2Int dir = _input.Dir;
        if (_characterRoster.IsMoving || dir == Vector2Int.zero)
            return;

        
        await _characterRoster.TryMove(_currentCharacter, dir);
    }

    public void Reset()
    {
        _currentCharacter = null;
        _currentTurnState = TurnState.None;
        _characterRoster.RemoveAllCharacters();
    }
}
