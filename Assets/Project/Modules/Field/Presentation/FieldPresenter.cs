using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FieldPresenter
{
    private readonly ConfigScriptableObject _config;
    private readonly FieldMap _fieldMap;
    private readonly GenerateFieldUseCase _generateFieldUseCase;

    private readonly CellView _cellPrefab;
    private readonly LinkView _linkView;
    private readonly CellColorTheme _theme;

    private readonly Transform _root;
    private readonly Dictionary<Cell, CellView> _views = new();

    public Cell StartCell => _fieldMap.GetCellsByType(CellType.Start).FirstOrDefault();

    public FieldPresenter(
        ConfigScriptableObject config,
        FieldMap fieldMap,
        GenerateFieldUseCase generateFieldUseCase,
        CellView cellPrefab,
        LinkView linkView,
        CellColorTheme theme)
    {
        _config = config;
        _fieldMap = fieldMap;
        _generateFieldUseCase = generateFieldUseCase;

        _cellPrefab = cellPrefab;
        _linkView = linkView;
        _theme = theme;

        _root = ((MonoBehaviour)linkView).transform;
    }

    public void CreateField()
    {
        _generateFieldUseCase.Execute(_config.FIELD_WIDTH, _config.FIELD_HEIGHT, _config.EXTRA_CONNECTION_CHANCE);
        BuildCells();
        BuildLinks();
    }

    private void BuildCells()
    {
        foreach (var cell in _fieldMap.GetAllCells())
        {
            var view = Object.Instantiate(_cellPrefab, _root);

            view.Construct(_theme);
            view.Bind(cell);

            view.transform.localPosition = new Vector3(cell.X * _config.CELL_SIZE, 0, cell.Y * _config.CELL_SIZE);

            _views[cell] = view;
        }
    }

    private void BuildLinks()
    {
        foreach (var link in _fieldMap.GetAllLinks())
        {
            var a = _views[link.A];
            var b = _views[link.B];

            _linkView.CreateVisualLink(link, a, b);
        }
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
        _linkView.ClearLinks();
        _fieldMap.Clear();
    }
}
