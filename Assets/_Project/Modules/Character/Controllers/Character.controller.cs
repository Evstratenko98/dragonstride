using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class CharacterController : IPostInitializable, ITickable, IDisposable
{
    private readonly CharacterService _characterService;
    private readonly IEventBus _eventBus;
    private readonly CharacterInput _input;

    private IDisposable _turnStateSubscription;

    private CharacterInstance _currentCharacter;
    private TurnState _currentTurnState = TurnState.None;

    public CharacterController(
        CharacterService characterService,
        IEventBus eventBus,
        CharacterInput input
    )
    {
        _characterService = characterService;
        _eventBus = eventBus;
        _input = input;
    }

    public void PostInitialize()
    {
        // Логика стадий игрового хода
        _turnStateSubscription = _eventBus.Subscribe<TurnStateChangedMessage>(OnTurnStateChanged);
    }

    public IReadOnlyList<CharacterInstance> SpawnCharacters(Cell startCell)
    {   
        _characterService.CreateCharacter(startCell, "Arnoldo", 0, new SamuraiClass());
        _characterService.CreateCharacter(startCell, "Patrick", 1, new RunnerClass());
        _characterService.CreateCharacter(startCell, "Jonh", 2, new RunnerClass());

        return _characterService.AllCharacters;
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
        if (_characterService.IsMoving || dir == Vector2Int.zero)
            return;

        
        await _characterService.TryMove(_currentCharacter, dir);
    }

    public void Reset()
    {
        _currentCharacter = null;
        _currentTurnState = TurnState.None;
        _characterService.RemoveAllCharacters();
    }
}
