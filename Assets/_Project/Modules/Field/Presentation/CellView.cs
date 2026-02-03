using UnityEngine;

public class CellView : MonoBehaviour
{
    private Renderer _renderer;
    private Cell _model;
    private CellColorTheme _theme;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
    }

    public void Initialize(CellColorTheme theme)
    {
        _theme = theme;
    }

    public void Bind(Cell model)
    {
        _model = model;
        UpdateMaterial(model.Type);
    }

    public void Refresh()
    {
        if (_model == null)
        {
            return;
        }

        UpdateMaterial(_model.Type);
    }

    private void UpdateMaterial(CellType type)
    {
        if (_renderer == null || _theme == null)
        {
            return;
        }

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
}
