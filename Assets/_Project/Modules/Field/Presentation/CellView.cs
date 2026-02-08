using UnityEngine;

public class CellView : MonoBehaviour
{
    private Renderer _renderer;
    private Cell _model;
    private CellColorTheme _theme;
    private Material _openedLootMaterial;
    private Material _openedFightMaterial;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
    }

    public void Initialize(CellColorTheme theme)
    {
        _theme = theme;
        _openedLootMaterial = CreateOpenedMaterial(_theme?.lootMaterial);
        _openedFightMaterial = CreateOpenedMaterial(_theme?.fightMaterial);
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
                _renderer.material = _model != null && _model.IsOpened
                    ? _openedLootMaterial ?? _theme.lootMaterial
                    : _theme.lootMaterial;
                break;
            case CellType.Fight:
                _renderer.material = _model != null && _model.IsOpened
                    ? _openedFightMaterial ?? _theme.fightMaterial
                    : _theme.fightMaterial;
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
        if (_openedLootMaterial != null)
        {
            Destroy(_openedLootMaterial);
        }

        if (_openedFightMaterial != null)
        {
            Destroy(_openedFightMaterial);
        }
    }

    private static Material CreateOpenedMaterial(Material source)
    {
        if (source == null)
        {
            return null;
        }

        var material = new Material(source);

        if (material.HasProperty("_BaseColor"))
        {
            var color = material.GetColor("_BaseColor");
            material.SetColor("_BaseColor", ToOpenedColor(color));
        }

        if (material.HasProperty("_Color"))
        {
            var color = material.GetColor("_Color");
            material.SetColor("_Color", ToOpenedColor(color));
        }

        return material;
    }

    private static Color ToOpenedColor(Color color)
    {
        float grayscale = (color.r + color.g + color.b) / 3f;
        var muted = Color.Lerp(color, new Color(grayscale, grayscale, grayscale, color.a), 0.45f);
        var darkened = muted * 0.6f;
        darkened.a = color.a;
        return darkened;
    }
}
