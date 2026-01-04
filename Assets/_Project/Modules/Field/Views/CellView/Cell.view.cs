using System;
using UnityEngine;

public class CellView : MonoBehaviour, ICellView
{
    private Renderer _renderer;
    private IDisposable _subscription;
    private ICellModel _model;
    private CellColorTheme _theme;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
    }

    public void Construct(CellColorTheme theme)
    {
        _theme = theme;
    }

    public void Bind(ICellModel model)
    {
        _model = model;
        UpdateMaterial(model.Type);
    }

    private void UpdateMaterial(CellModelType type)
    {
        switch (type)
        {
            case CellModelType.Start:
                _renderer.material = _theme.startMaterial;
                break;
            case CellModelType.Common:
                _renderer.material = _theme.commonMaterial;
                break;
            case CellModelType.End:
                _renderer.material = _theme.endMaterial;
                break;
        }
    }

    private void OnDestroy()
    {
        _subscription?.Dispose();
    }
}
