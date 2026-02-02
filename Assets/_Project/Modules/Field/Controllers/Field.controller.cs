using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using System.Linq;

public class FieldController
{
    private readonly ConfigScriptableObject _config;
    private readonly FieldService _fieldService;
    private readonly MazeGenerator _mazeGenerator;

    private readonly CellView _cellPrefab;
    private readonly LinkView _linkView;
    private readonly CellColorTheme _theme;
    private readonly FogOfWarView _fogOfWarView;
    private readonly FieldRootService _fieldRootService;

    private readonly Transform _root;
    private readonly Dictionary<CellModel, CellView> _views = new();

    public CellModel StartCellModel => _fieldService.GetCellsByType(CellModelType.Start).FirstOrDefault();

    public FieldController(
        ConfigScriptableObject config,
        FieldService fieldService,
        MazeGenerator mazeGenerator,
        CellView cellPrefab,
        LinkView linkView,
        CellColorTheme theme,
        FogOfWarView fogOfWarView,
        FieldRootService fieldRootService)
    {
        _config = config;
        _fieldService = fieldService;
        _mazeGenerator = mazeGenerator;

        _cellPrefab = cellPrefab;
        _linkView = linkView;
        _theme = theme;
        _fogOfWarView = fogOfWarView;
        _fieldRootService = fieldRootService;

        _root = _fieldRootService.EnsureRoot();
        _linkView.transform.SetParent(_root, false);
        _fogOfWarView.transform.SetParent(_root, false);
    }
    
    public void CreateField()
    {
        _fieldService.Initialize(_config.FIELD_WIDTH, _config.FIELD_HEIGHT);
        _mazeGenerator.Generate(_fieldService, _config.EXTRA_CONNECTION_CHANCE);
        BuildCells();
        BuildLinks();
    }

    private void BuildCells()
    {
        foreach (var cell in _fieldService.GetAllCells())
        {
            var view = GameObject.Instantiate(_cellPrefab, _root);

            // передаём зависимости вручную
            view.Construct(_theme);
            view.Bind(cell);

            view.transform.localPosition = new Vector3(cell.X * _config.CELL_SIZE, 0, cell.Y * _config.CELL_SIZE);

            _views[cell] = view;
        }
    }

    private void BuildLinks()
    {
        foreach (var link in _fieldService.GetAllLinks())
        {
            var a = _views[link.A];
            var b = _views[link.B];

            _linkView.CreateVisualLink(link, a, b);
        }
    }

    public void Reset()
    {
        // удалить все клетки
        foreach (var view in _views.Values)
            if (view != null) {
                GameObject.Destroy(view.gameObject);
            }
        _views.Clear();
        _linkView.ClearLinks();
        _fieldService.Clear();
    }
}
