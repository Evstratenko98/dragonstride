using System;
using System.Collections.Generic;
using VContainer.Unity;

public sealed class FogOfWarPresenter : IPostInitializable, IDisposable
{
    private const int VisionRadius = 2;

    private readonly IEventBus _eventBus;
    private readonly FieldState _fieldState;
    private readonly CharacterRoster _characterRoster;
    private readonly ConfigScriptableObject _config;
    private readonly FogOfWarView _view;

    private IDisposable _moveSubscription;
    private IDisposable _gameStateSubscription;
    private IDisposable _fogToggleSubscription;
    private bool _isFogEnabled = true;

    public FogOfWarPresenter(
        IEventBus eventBus,
        FieldState fieldState,
        CharacterRoster characterRoster,
        ConfigScriptableObject config,
        FieldViewFactory viewFactory)
    {
        _eventBus = eventBus;
        _fieldState = fieldState;
        _characterRoster = characterRoster;
        _config = config;
        _view = viewFactory.FogOfWarView;
    }

    public void PostInitialize()
    {
        _moveSubscription = _eventBus.Subscribe<CharacterMoved>(OnCharacterMoved);
        _gameStateSubscription = _eventBus.Subscribe<GameStateChanged>(OnGameStateChanged);
        _fogToggleSubscription = _eventBus.Subscribe<FogOfWarToggled>(OnFogToggleChanged);
    }

    public void Dispose()
    {
        _moveSubscription?.Dispose();
        _gameStateSubscription?.Dispose();
        _fogToggleSubscription?.Dispose();
    }

    private void OnGameStateChanged(GameStateChanged message)
    {
        if (message.State == GameState.Playing)
        {
            InitializeFog();
            ApplyFogEnabledState();
        }

        if (message.State == GameState.Finished)
        {
            _view.Clear();
        }
    }

    private void InitializeFog()
    {
        var field = _fieldState.CurrentField;
        if (field == null)
        {
            return;
        }

        foreach (var cell in field.GetAllCells())
        {
            cell.SetVisibility(CellVisibility.Unseen);
        }

        _view.Build(field, _config.CELL_SIZE);
        UpdateVisibility();
    }

    private void OnCharacterMoved(CharacterMoved message)
    {
        if (!_isFogEnabled)
        {
            return;
        }

        UpdateVisibility();
    }

    private void OnFogToggleChanged(FogOfWarToggled message)
    {
        _isFogEnabled = message.IsEnabled;
        ApplyFogEnabledState();
    }

    private void ApplyFogEnabledState()
    {
        if (_view != null)
        {
            _view.gameObject.SetActive(_isFogEnabled);
        }

        if (_isFogEnabled)
        {
            UpdateVisibility();
            return;
        }

        ShowEverything();
    }

    private void UpdateVisibility()
    {
        var field = _fieldState.CurrentField;
        if (field == null)
        {
            return;
        }

        var currentlyVisible = new HashSet<Cell>();

        foreach (var character in _characterRoster.AllCharacters)
        {
            var currentCell = character?.Model?.CurrentCell;
            if (currentCell == null)
            {
                continue;
            }

            var fieldCell = field.GetCell(currentCell.X, currentCell.Y);
            if (fieldCell == null)
            {
                continue;
            }

            foreach (var visible in CollectVisibleCells(fieldCell, VisionRadius))
            {
                currentlyVisible.Add(visible);
            }
        }

        foreach (var cell in field.GetAllCells())
        {
            if (currentlyVisible.Contains(cell))
            {
                cell.SetVisibility(CellVisibility.Visible);
            }
            else if (cell.VisibilityState == CellVisibility.Visible)
            {
                cell.SetVisibility(CellVisibility.Seen);
            }

            _view.ApplyVisibility(cell);
        }

        ApplyOccupantsVisibility();
    }

    private void ShowEverything()
    {
        var field = _fieldState.CurrentField;
        if (field != null)
        {
            foreach (var cell in field.GetAllCells())
            {
                cell.SetVisibility(CellVisibility.Visible);
            }
        }

        var actors = _characterRoster.GetAllOccupants();
        for (int i = 0; i < actors.Count; i++)
        {
            var occupant = actors[i];
            if (occupant == null)
            {
                continue;
            }

            occupant.SetWorldVisible(true);
        }
    }

    private void ApplyOccupantsVisibility()
    {
        var actors = _characterRoster.GetAllOccupants();
        for (int i = 0; i < actors.Count; i++)
        {
            var occupant = actors[i];
            if (occupant == null || occupant is CharacterInstance)
            {
                continue;
            }

            var cell = occupant.Entity?.CurrentCell;
            bool isVisible = cell != null && cell.VisibilityState == CellVisibility.Visible;
            occupant.SetWorldVisible(isVisible);
        }
    }

    private IEnumerable<Cell> CollectVisibleCells(Cell origin, int radius)
    {
        var result = new HashSet<Cell>();
        if (origin == null || radius < 0)
        {
            return result;
        }

        var queue = new Queue<(Cell cell, int depth)>();
        queue.Enqueue((origin, 0));
        result.Add(origin);

        while (queue.Count > 0)
        {
            var (cell, depth) = queue.Dequeue();
            if (depth >= radius)
            {
                continue;
            }

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
