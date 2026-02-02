using System.Collections.Generic;
using Project.Modules.Field.Application;
using Project.Modules.Field.Domain;
using Project.Modules.Field.Infrastructure;
using UnityEngine;

namespace Project.Modules.Field.Presentation;

public sealed class FieldPresenter
{
    private readonly ConfigScriptableObject _config;
    private readonly FieldGenerator _fieldGenerator;
    private readonly FieldState _fieldState;
    private readonly CellView _cellPrefab;
    private readonly CellColorTheme _theme;
    private readonly FieldRoot _fieldRoot;
    private readonly FieldViewFactory _viewFactory;

    private readonly Dictionary<Cell, CellView> _views = new();

    private LinkView _linkView;
    private Transform _root;

    public FieldPresenter(
        ConfigScriptableObject config,
        FieldGenerator fieldGenerator,
        FieldState fieldState,
        CellView cellPrefab,
        CellColorTheme theme,
        FieldRoot fieldRoot,
        FieldViewFactory viewFactory)
    {
        _config = config;
        _fieldGenerator = fieldGenerator;
        _fieldState = fieldState;
        _cellPrefab = cellPrefab;
        _theme = theme;
        _fieldRoot = fieldRoot;
        _viewFactory = viewFactory;
    }

    public Cell StartCell => _fieldState.StartCell;

    public void CreateField()
    {
        var field = _fieldGenerator.Create(_config.FIELD_WIDTH, _config.FIELD_HEIGHT, _config.EXTRA_CONNECTION_CHANCE);
        _fieldState.SetField(field);

        _root = _fieldRoot.EnsureRoot();
        _linkView = _viewFactory.LinkView;
        _ = _viewFactory.FogOfWarView;

        BuildCells(field);
        BuildLinks(field);
    }

    public void Reset()
    {
        foreach (var view in _views.Values)
        {
            if (view != null)
            {
                Object.Destroy(view.gameObject);
            }
        }

        _views.Clear();
        _linkView?.ClearLinks();
        _fieldState.Clear();
    }

    private void BuildCells(FieldGrid field)
    {
        foreach (var cell in field.GetAllCells())
        {
            var view = Object.Instantiate(_cellPrefab, _root);
            view.Initialize(_theme);
            view.Bind(cell);
            view.transform.localPosition = new Vector3(cell.X * _config.CELL_SIZE, 0f, cell.Y * _config.CELL_SIZE);
            _views[cell] = view;
        }
    }

    private void BuildLinks(FieldGrid field)
    {
        foreach (var link in field.GetAllLinks())
        {
            if (_views.TryGetValue(link.A, out var aView) && _views.TryGetValue(link.B, out var bView))
            {
                _linkView.CreateVisualLink(link, aView, bView);
            }
        }
    }
}
