using UnityEngine;

public class CharacterView : MonoBehaviour
{
    [Header("Overhead UI")]
    [SerializeField] private float uiHeightOffset = 1.8f;
    [SerializeField] private float widthMultiplier = 1.5f;
    [SerializeField] private float fallbackModelWidth = 0.8f;
    [SerializeField] private float barHeight = 0.1f;
    [SerializeField] private float nameOffset = 0.22f;
    [SerializeField] private Color fillColor = new Color(0.85f, 0.08f, 0.08f, 1f);
    [SerializeField] private Color backgroundColor = new Color(0.14f, 0.14f, 0.14f, 1f);

    private Coroutine _moveRoutine;
    private Character _model;
    private int _maxHealth;

    private Transform _uiRoot;
    private Transform _fillTransform;
    private TextMesh _nameText;
    private float _barWidth;

    public void SetPosition(Vector3 cellPos)
    {
        transform.position = cellPos;
    }

    public void Bind(Character model, string characterName)
    {
        if (_model != null)
        {
            _model.StatsChanged -= RefreshHealth;
        }

        _model = model;
        _maxHealth = Mathf.Max(1, model?.Health ?? 1);

        EnsureOverheadUi();
        SetCharacterName(characterName);
        RefreshHealth();

        if (_model != null)
        {
            _model.StatsChanged += RefreshHealth;
        }
    }

    public void MoveToPosition(Vector3 targetPosition, float speed)
    {
        if (_moveRoutine != null)
        {
            StopCoroutine(_moveRoutine);
        }

        _moveRoutine = StartCoroutine(MoveRoutine(targetPosition, speed));
    }

    private System.Collections.IEnumerator MoveRoutine(Vector3 targetPosition, float speed)
    {
        Vector3 start = transform.position;
        float distance = Vector3.Distance(start, targetPosition);
        float duration = speed > 0f ? distance / speed : 0f;

        if (duration <= 0f)
        {
            transform.position = targetPosition;
            _moveRoutine = null;
            yield break;
        }

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            transform.position = Vector3.Lerp(start, targetPosition, t);
            yield return null;
        }

        transform.position = targetPosition;
        _moveRoutine = null;
    }

    private void LateUpdate()
    {
        if (_uiRoot == null)
        {
            return;
        }

        Camera activeCamera = Camera.main;
        if (activeCamera == null)
        {
            return;
        }

        _uiRoot.forward = activeCamera.transform.forward;
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

        _barWidth = ResolveModelWidth() * widthMultiplier;

        var root = new GameObject("OverheadUI");
        root.transform.SetParent(transform, false);
        root.transform.localPosition = new Vector3(0f, uiHeightOffset, 0f);
        _uiRoot = root.transform;

        var background = CreateQuad("HealthBarBackground", backgroundColor, _barWidth, barHeight);
        background.transform.SetParent(_uiRoot, false);

        var fillPivot = new GameObject("HealthBarFillPivot");
        fillPivot.transform.SetParent(_uiRoot, false);
        fillPivot.transform.localPosition = new Vector3(-_barWidth * 0.5f, 0f, -0.001f);
        _fillTransform = fillPivot.transform;

        var fill = CreateQuad("HealthBarFill", fillColor, _barWidth, barHeight);
        fill.transform.SetParent(_fillTransform, false);
        fill.transform.localPosition = new Vector3(_barWidth * 0.5f, 0f, 0f);

        var nameObject = new GameObject("Name");
        nameObject.transform.SetParent(_uiRoot, false);
        nameObject.transform.localPosition = new Vector3(0f, nameOffset, -0.001f);
        _nameText = nameObject.AddComponent<TextMesh>();
        _nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _nameText.fontSize = 36;
        _nameText.anchor = TextAnchor.MiddleCenter;
        _nameText.alignment = TextAlignment.Center;
        _nameText.color = Color.white;
        _nameText.characterSize = 0.05f;
    }

    private GameObject CreateQuad(string objectName, Color color, float width, float height)
    {
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = objectName;
        quad.transform.localScale = new Vector3(width, height, 1f);

        var renderer = quad.GetComponent<MeshRenderer>();
        renderer.material = new Material(Shader.Find("Unlit/Color"));
        renderer.material.color = color;

        var collider = quad.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        return quad;
    }

    private void SetCharacterName(string characterName)
    {
        if (_nameText == null)
        {
            return;
        }

        _nameText.text = string.IsNullOrWhiteSpace(characterName) ? "Unknown" : characterName;
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

    private float ResolveModelWidth()
    {
        var renderers = GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0)
        {
            return fallbackModelWidth;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            if (_uiRoot != null && renderers[i].transform.IsChildOf(_uiRoot))
            {
                continue;
            }

            bounds.Encapsulate(renderers[i].bounds);
        }

        float width = Mathf.Max(bounds.size.x, bounds.size.z);
        return width > 0.01f ? width : fallbackModelWidth;
    }
}
