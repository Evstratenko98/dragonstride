using System;
using System.Collections.Generic;
using VContainer.Unity;

public class FogOfWarController : IPostInitializable, IDisposable
{
    private const int VisionRadius = 2;

    private readonly IEventBus _eventBus;
    private readonly FieldService _fieldService;
    private readonly CharacterService _characterService;
    private readonly ConfigScriptableObject _config;
    private readonly FogOfWarView _view;

    private IDisposable _moveSubscription;
    private IDisposable _gameStateSubscription;

    public FogOfWarController(
        IEventBus eventBus,
        FieldService fieldService,
        CharacterService characterService,
        ConfigScriptableObject config,
        FogOfWarView view)
    {
        _eventBus = eventBus;
        _fieldService = fieldService;
        _characterService = characterService;
        _config = config;
        _view = view;
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

    private void OnGameStateChanged(GameStateChangedMessage message)
    {
        if (message.State == GameState.Playing)
        {
            InitializeFog();
        }

        if (message.State == GameState.Finished)
        {
            _view.Clear();
        }
    }

    private void InitializeFog()
    {
        if (_fieldService.Grid == null)
            return;

        foreach (var cell in _fieldService.GetAllCells())
        {
            cell.SetVisibility(CellVisibilityState.Unseen);
        }

        _view.Build(_fieldService, _config.CELL_SIZE);
        UpdateVisibility();
    }

    private void OnCharacterMoved(CharacterMovedMessage message)
    {
        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        if (_fieldService.Grid == null)
            return;

        var currentlyVisible = new HashSet<CellModel>();

        foreach (var character in _characterService.AllCharacters)
        {
            var cell = character?.Model?.CurrentCell;
            if (cell == null)
                continue;

            foreach (var visible in CollectVisibleCells(cell, VisionRadius))
            {
                currentlyVisible.Add(visible);
            }
        }

        foreach (var cell in _fieldService.GetAllCells())
        {
            if (currentlyVisible.Contains(cell))
            {
                cell.SetVisibility(CellVisibilityState.Visible);
            }
            else if (cell.VisibilityState == CellVisibilityState.Visible)
            {
                cell.SetVisibility(CellVisibilityState.Seen);
            }

            _view.ApplyVisibility(cell);
        }
    }

    private IEnumerable<CellModel> CollectVisibleCells(CellModel origin, int radius)
    {
        var result = new HashSet<CellModel>();
        if (origin == null || radius < 0)
            return result;

        var queue = new Queue<(CellModel cell, int depth)>();
        queue.Enqueue((origin, 0));
        result.Add(origin);

        while (queue.Count > 0)
        {
            var (cell, depth) = queue.Dequeue();
            if (depth >= radius)
                continue;

            foreach (var neighbor in cell.Neighbors)
            {
                if (result.Add(neighbor))
                {
                    queue.Enqueue((neighbor, depth + 1));
                }
            }
        }

        return result;
    }
}
