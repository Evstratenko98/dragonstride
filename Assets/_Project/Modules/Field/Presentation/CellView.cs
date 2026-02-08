using UnityEngine;

public class CellView : MonoBehaviour
{
    [SerializeField] private Renderer hiddenOverlayRenderer;

    private Renderer _renderer;
    private Cell _model;
    private CellColorTheme _theme;
    private Material _openedLootMaterial;
    private Material _openedFightMaterial;
    private bool _isHiddenOverlayEnabled = true;
    private static Mesh _cachedCylinderMesh;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
    }

    public void Initialize(CellColorTheme theme)
    {
        _theme = theme;
        EnsureHiddenVisuals();
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

    public void SetHiddenOverlayEnabled(bool isEnabled)
    {
        _isHiddenOverlayEnabled = isEnabled;
        if (_model == null)
        {
            SetHiddenVisualsVisible(_isHiddenOverlayEnabled);
            return;
        }

        if (_model != null)
        {
            UpdateMaterial(_model.Type);
        }
    }

    private void UpdateMaterial(CellType type)
    {
        if (_renderer == null || _theme == null)
        {
            return;
        }

        bool isRevealed = _model != null && _model.IsTypeRevealed;
        bool shouldHideType = _isHiddenOverlayEnabled && !isRevealed;
        SetHiddenVisualsVisible(_isHiddenOverlayEnabled);
        if (shouldHideType)
        {
            _renderer.material = _theme.hiddenMaterial ?? _theme.commonMaterial;
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

    private void EnsureHiddenVisuals()
    {
        if (hiddenOverlayRenderer == null)
        {
            var overlay = transform.Find("HiddenOverlay");
            if (overlay == null)
            {
                overlay = CreateHiddenOverlay().transform;
            }

            hiddenOverlayRenderer = overlay.GetComponent<Renderer>();
            if (hiddenOverlayRenderer == null)
            {
                hiddenOverlayRenderer = overlay.gameObject.AddComponent<MeshRenderer>();
            }

            var meshFilter = overlay.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = overlay.gameObject.AddComponent<MeshFilter>();
            }

            meshFilter.sharedMesh = GetBuiltInCylinderMesh();
            overlay.localPosition = new Vector3(0f, 0.51f, 0f);
            overlay.localRotation = Quaternion.Euler(0f, 0f, 0f);
            overlay.localScale = new Vector3(1f, 0.04f, 1f);
        }

        if (hiddenOverlayRenderer != null)
        {
            hiddenOverlayRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            hiddenOverlayRenderer.receiveShadows = false;
            hiddenOverlayRenderer.material = _theme?.hiddenMaterial ?? _theme?.commonMaterial;
        }

    }

    private void SetHiddenVisualsVisible(bool isVisible)
    {
        if (hiddenOverlayRenderer != null)
        {
            hiddenOverlayRenderer.enabled = isVisible;
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

    private static Mesh GetBuiltInCylinderMesh()
    {
        if (_cachedCylinderMesh != null)
        {
            return _cachedCylinderMesh;
        }

        var primitive = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        _cachedCylinderMesh = primitive.GetComponent<MeshFilter>()?.sharedMesh;
        if (primitive.TryGetComponent<Collider>(out var collider))
        {
            Destroy(collider);
        }

        primitive.hideFlags = HideFlags.HideAndDontSave;
        if (Application.isPlaying)
        {
            Destroy(primitive);
        }
        else
        {
            DestroyImmediate(primitive);
        }

        return _cachedCylinderMesh;
    }

    private GameObject CreateHiddenOverlay()
    {
        var overlay = new GameObject("HiddenOverlay");
        overlay.transform.SetParent(transform, false);
        overlay.transform.localPosition = new Vector3(0f, 0.51f, 0f);
        overlay.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        overlay.transform.localScale = new Vector3(1f, 0.04f, 1f);
        return overlay;
    }

}
