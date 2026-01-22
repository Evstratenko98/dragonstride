using System;
using UnityEngine;
using VContainer.Unity;

public class FogOfWarController : IPostInitializable, IDisposable
{
    private readonly IEventBus _eventBus;
    private readonly FieldService _fieldService;
    private readonly CharacterService _characterService;
    private readonly FogOfWarService _fogOfWarService;
    private readonly FogOfWarView _fogOfWarView;
    private readonly ConfigScriptableObject _config;

    private IDisposable _moveSubscription;
    private IDisposable _gameStateSubscription;

    public FogOfWarController(
        IEventBus eventBus,
        FieldService fieldService,
        CharacterService characterService,
        FogOfWarService fogOfWarService,
        FogOfWarView fogOfWarView,
        ConfigScriptableObject config)
    {
        _eventBus = eventBus;
        _fieldService = fieldService;
        _characterService = characterService;
        _fogOfWarService = fogOfWarService;
        _fogOfWarView = fogOfWarView;
        _config = config;
    }

    public void PostInitialize()
    {
        _moveSubscription = _eventBus.Subscribe<CharacterMovedMessage>(OnCharacterMoved);
        _gameStateSubscription = _eventBus.Subscribe<GameStateChangedMessage>(OnGameStateChanged);
    }

    public void Dispose()
    {
        _moveSubscription?.Dispose();
        _gameStateSubscription?.Dispose();
    }

    private void OnGameStateChanged(GameStateChangedMessage msg)
    {
        if (msg.State != GameState.Playing)
            return;

        InitializeFog();
    }

    private void InitializeFog()
    {
        if (_fogOfWarView == null)
        {
            Debug.LogWarning("[FogOfWarController] FogOfWarView is missing.");
            return;
        }

        _fogOfWarService.Initialize(_fieldService);
        _fogOfWarView.Initialize(_fieldService, _config.CELL_SIZE);
        RefreshFog();
    }

    private void OnCharacterMoved(CharacterMovedMessage msg)
    {
        RefreshFog();
    }

    private void RefreshFog()
    {
        if (_characterService.AllCharacters == null || _fogOfWarView == null)
            return;

        _fogOfWarService.RevealFromCharacters(_characterService.AllCharacters, _config.CHARACTER_VISION_RANGE);
        _fogOfWarView.Render(_fieldService);
    }
}
