using System;
using UnityEngine;

public class CellView : MonoBehaviour
{
    private Renderer _renderer;
    private IDisposable _subscription;
    private Cell _model;
    private CellColorTheme _theme;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
    }

    public void Construct(CellColorTheme theme)
    {
        _theme = theme;
    }

    public void Bind(Cell model)
    {
        _model = model;
        UpdateMaterial(model.Type);
    }

    private void UpdateMaterial(CellType type)
    {
        switch (type)
        {
            case CellType.Start:
                _renderer.material = _theme.startMaterial;
                break;
            case CellType.Common:
                _renderer.material = _theme.commonMaterial;
                break;
            case CellType.Loot:
                _renderer.material = _theme.lootMaterial;
                break;
            case CellType.Fight:
                _renderer.material = _theme.fightMaterial;
                break;
            case CellType.Teleport:
                _renderer.material = _theme.teleportMaterial;
                break;
            case CellType.End:
                _renderer.material = _theme.endMaterial;
                break;
        }
    }

    private void OnDestroy()
    {
        _subscription?.Dispose();
    }
}
