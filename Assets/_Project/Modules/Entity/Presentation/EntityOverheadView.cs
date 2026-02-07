using UnityEngine;

public class EntityOverheadView : MonoBehaviour
{
    [Header("Overhead UI")]
    [SerializeField] private float extraHeightAboveModel = 0.2f;
    [SerializeField] private float fixedUiWidth = 1.2f;
    [SerializeField] private float fixedBarHeight = 0.1f;
    [SerializeField] private float nameOffset = 0.22f;
    [SerializeField] private int fixedNameFontSize = 36;
    [SerializeField] private float fixedNameCharacterSize = 0.05f;
    [SerializeField] private Color backgroundColor = new Color(0.14f, 0.14f, 0.14f, 1f);

    private Entity _model;
    private int _maxHealth;

    private Transform _uiRoot;
    private Transform _fillTransform;
    private Renderer _fillRenderer;
    private TextMesh _nameText;
    private float _barWidth;

    public void Bind(Entity model, string displayName = null)
    {
        if (_model != null)
        {
            _model.StatsChanged -= RefreshHealth;
        }

        _model = model;
        _maxHealth = Mathf.Max(1, model?.Health ?? 1);

        EnsureOverheadUi();
        RefreshFillColor();
        SetEntityName(string.IsNullOrWhiteSpace(displayName) ? _model?.Name : displayName);
        RefreshHealth();

        if (_model != null)
        {
            _model.StatsChanged += RefreshHealth;
        }
    }

    private void LateUpdate()
    {
        if (_uiRoot == null)
        {
            return;
        }

        UpdateOverheadTransform();
    }

    private void OnDestroy()
    {
        if (_model != null)
        {
            _model.StatsChanged -= RefreshHealth;
        }
    }

    private void EnsureOverheadUi()
    {
        if (_uiRoot != null)
        {
            return;
        }

        _barWidth = Mathf.Max(0.1f, fixedUiWidth);

        var root = new GameObject("OverheadUI");
        root.transform.SetParent(transform, false);
        root.transform.localPosition = Vector3.zero;
        _uiRoot = root.transform;
        UpdateOverheadTransform();

        float barHeight = Mathf.Max(0.01f, fixedBarHeight);
        var background = CreateQuad("HealthBarBackground", backgroundColor, _barWidth, barHeight);
        background.transform.SetParent(_uiRoot, false);

        var fillPivot = new GameObject("HealthBarFillPivot");
        fillPivot.transform.SetParent(_uiRoot, false);
        fillPivot.transform.localPosition = new Vector3(-_barWidth * 0.5f, 0f, -0.001f);
        _fillTransform = fillPivot.transform;

        var fill = CreateQuad("HealthBarFill", ResolveFillColor(), _barWidth, barHeight);
        fill.transform.SetParent(_fillTransform, false);
        fill.transform.localPosition = new Vector3(_barWidth * 0.5f, 0f, 0f);
        _fillRenderer = fill.GetComponent<Renderer>();

        var nameObject = new GameObject("Name");
        nameObject.transform.SetParent(_uiRoot, false);
        nameObject.transform.localPosition = new Vector3(0f, nameOffset, -0.001f);
        _nameText = nameObject.AddComponent<TextMesh>();
        _nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _nameText.fontSize = Mathf.Max(1, fixedNameFontSize);
        _nameText.anchor = TextAnchor.MiddleCenter;
        _nameText.alignment = TextAlignment.Center;
        _nameText.color = Color.white;
        _nameText.characterSize = Mathf.Max(0.001f, fixedNameCharacterSize);
    }

    private GameObject CreateQuad(string objectName, Color color, float width, float height)
    {
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = objectName;
        quad.transform.localScale = new Vector3(width, height, 1f);

        var renderer = quad.GetComponent<MeshRenderer>();
        var material = CreateRuntimeMaterial(renderer, color);
        if (material != null)
        {
            renderer.material = material;
        }

        var collider = quad.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        return quad;
    }

    private Material CreateRuntimeMaterial(Renderer renderer, Color color)
    {
        string[] shaderNames =
        {
            "Unlit/Color",
            "Universal Render Pipeline/Unlit",
            "Sprites/Default",
            "Standard"
        };

        for (int i = 0; i < shaderNames.Length; i++)
        {
            Shader shader = Shader.Find(shaderNames[i]);
            if (shader == null)
            {
                continue;
            }

            var material = new Material(shader);
            ApplyMaterialColor(material, color);
            return material;
        }

        if (renderer != null && renderer.sharedMaterial != null)
        {
            var fallbackMaterial = new Material(renderer.sharedMaterial);
            ApplyMaterialColor(fallbackMaterial, color);
            return fallbackMaterial;
        }

        Debug.LogError("[EntityOverheadView] Failed to create runtime material because no compatible shader was found.");
        return null;
    }

    private static void ApplyMaterialColor(Material material, Color color)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        else if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
    }

    private void SetEntityName(string entityName)
    {
        if (_nameText == null)
        {
            return;
        }

        _nameText.text = string.IsNullOrWhiteSpace(entityName) ? "Unknown" : entityName;
    }

    private void RefreshFillColor()
    {
        ApplyMaterialColor(_fillRenderer?.material, ResolveFillColor());
    }

    private void RefreshHealth()
    {
        if (_model == null || _fillTransform == null)
        {
            return;
        }

        _maxHealth = Mathf.Max(_maxHealth, _model.Health, 1);
        float normalized = Mathf.Clamp01((float)_model.Health / _maxHealth);
        _fillTransform.localScale = new Vector3(normalized, 1f, 1f);
    }

    private Color ResolveFillColor()
    {
        return _model?.HealthBarFillColor ?? Color.green;
    }

    private void UpdateOverheadTransform()
    {
        if (_uiRoot == null)
        {
            return;
        }

        float topY = ResolveModelTopY();
        _uiRoot.position = new Vector3(
            transform.position.x,
            topY + Mathf.Max(0f, extraHeightAboveModel),
            transform.position.z
        );

        Camera activeCamera = Camera.main;
        if (activeCamera != null)
        {
            _uiRoot.forward = activeCamera.transform.forward;
        }

        Vector3 parentScale = transform.lossyScale;
        _uiRoot.localScale = new Vector3(
            SafeReciprocal(parentScale.x),
            SafeReciprocal(parentScale.y),
            SafeReciprocal(parentScale.z)
        );
    }

    private static float SafeReciprocal(float value)
    {
        float abs = Mathf.Abs(value);
        if (abs < 0.0001f)
        {
            return 1f;
        }

        return 1f / value;
    }

    private float ResolveModelTopY()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            return transform.position.y;
        }

        bool hasBounds = false;
        float topY = transform.position.y;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            if (_uiRoot != null && renderer.transform.IsChildOf(_uiRoot))
            {
                continue;
            }

            float rendererTopY = renderer.bounds.max.y;
            if (!hasBounds || rendererTopY > topY)
            {
                topY = rendererTopY;
                hasBounds = true;
            }
        }

        return hasBounds ? topY : transform.position.y;
    }
}
