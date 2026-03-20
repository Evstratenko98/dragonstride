using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterRosterPanelView : MonoBehaviour
{
    private const float PanelWidth = 376f;
    private const float HeaderHeight = 34f;
    private const float TopInset = 70f;
    private const float ExpandedRightInset = 10f;
    private const float CollapsedVisibleWidth = 84f;

    [SerializeField] private CharacterCardView cardTemplate;
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private float cardSpacing = 8f;

    private readonly List<CharacterCardView> _cards = new();
    private CharacterStatTooltipView _tooltipView;
    private RectTransform _panelRect;
    private RectTransform _toggleRect;
    private Button _toggleButton;
    private TMP_Text _toggleText;
    private float _targetX;
    private float _expandedX;
    private float _collapsedX;
    private bool _isExpanded;
    private bool _isConfigured;

    private void Awake()
    {
        if (!TryCacheSceneLayout())
        {
            Debug.LogWarning("[CharacterRosterPanelView] Scene layout is missing. Rebuild the panel from the editor menu.");
            return;
        }

        if (_panelRect != null)
        {
            _tooltipView = CreateTooltipView(_panelRect);
        }

        cardTemplate.Prepare(this);
        InitializeDrawerState();
        RefreshPanelHeight();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying || !gameObject.scene.IsValid())
        {
            return;
        }

        UnityEditor.EditorApplication.delayCall += SyncSceneLayoutIfAlive;
    }
#endif

    private void Update()
    {
        if (!_isConfigured || _panelRect == null)
        {
            return;
        }

        Vector2 anchoredPosition = _panelRect.anchoredPosition;
        float nextX = Mathf.MoveTowards(anchoredPosition.x, _targetX, 1200f * Time.unscaledDeltaTime);
        if (!Mathf.Approximately(nextX, anchoredPosition.x))
        {
            _panelRect.anchoredPosition = new Vector2(nextX, anchoredPosition.y);
        }
    }

    private void OnDestroy()
    {
        if (_toggleButton != null)
        {
            _toggleButton.onClick.RemoveListener(ToggleDrawer);
        }
    }

#if UNITY_EDITOR
    private bool NeedsSceneLayoutRebuild()
    {
        return transform.Find("RuntimeHeader") == null ||
               transform.Find("DrawerToggle") == null ||
               transform.Find("RuntimeTooltip") == null ||
               transform.Find("CharacterRosterContent") == null ||
               contentRoot == null ||
               cardTemplate == null ||
               Mathf.Abs(GetComponent<RectTransform>().sizeDelta.x - PanelWidth) > 0.5f ||
               (cardTemplate != null && Mathf.Abs(cardTemplate.GetComponent<RectTransform>().sizeDelta.y - CharacterCardView.PreferredHeight) > 0.5f);
    }

    private void SyncSceneLayoutIfAlive()
    {
        if (this == null || Application.isPlaying || !gameObject.scene.IsValid())
        {
            return;
        }

        if (NeedsSceneLayoutRebuild())
        {
            RebuildSceneLayout();
            return;
        }

        if (!TryCacheSceneLayout())
        {
            return;
        }

        if (_panelRect != null)
        {
            _tooltipView = CreateTooltipView(_panelRect);
        }

        cardTemplate.Prepare(this);
        InitializeDrawerState();
        RefreshPanelHeight();
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }
#endif

    public void SetCharacters(IReadOnlyList<CharacterInstance> characters)
    {
        ClearCards();
        HideTooltip();

        if (cardTemplate == null || contentRoot == null)
        {
            return;
        }

        cardTemplate.Prepare(this);
        SetLayoutElement(cardTemplate.gameObject, preferredHeight: CharacterCardView.PreferredHeight, flexibleWidth: 1f);
        cardTemplate.gameObject.SetActive(true);

        if (characters == null || characters.Count == 0)
        {
            cardTemplate.SetPreviewData("Самурай", 1, 90, 90, 55, 0, 0.20f, 0, 0, 0.10f);
            RefreshPanelHeight();
            return;
        }

        cardTemplate.SetCharacter(characters[0]);

        for (int i = 1; i < characters.Count; i++)
        {
            CharacterCardView card = Instantiate(cardTemplate, contentRoot);
            card.Prepare(this);
            card.gameObject.SetActive(true);
            card.SetCharacter(characters[i]);
            SetLayoutElement(card.gameObject, preferredHeight: CharacterCardView.PreferredHeight, flexibleWidth: 1f);
            _cards.Add(card);
        }

        RefreshPanelHeight();
    }

    public void ShowTooltip(RectTransform target, string title, string description)
    {
        _tooltipView?.Show(target, title, description);
    }

    public void HideTooltip()
    {
        _tooltipView?.Hide();
    }

    private bool TryCacheSceneLayout()
    {
        _panelRect = GetComponent<RectTransform>();
        contentRoot ??= transform.Find("CharacterRosterContent") as RectTransform;

        if (contentRoot != null && cardTemplate == null)
        {
            cardTemplate = contentRoot.Find("CharacterCardTemplate")?.GetComponent<CharacterCardView>();
        }

        if (_tooltipView == null)
        {
            _tooltipView = transform.Find("RuntimeTooltip")?.GetComponent<CharacterStatTooltipView>();
        }

        Transform toggle = transform.Find("DrawerToggle");
        _toggleRect = toggle as RectTransform;
        _toggleButton = toggle != null ? toggle.GetComponent<Button>() : null;
        _toggleText = toggle != null ? toggle.GetComponentInChildren<TMP_Text>(true) : null;

        _isConfigured = _panelRect != null && contentRoot != null && cardTemplate != null;
        return _isConfigured;
    }

    private void ConfigurePanel()
    {
        if (_isConfigured)
        {
            return;
        }

        _panelRect = GetComponent<RectTransform>();
        _panelRect.anchorMin = new Vector2(1f, 1f);
        _panelRect.anchorMax = new Vector2(1f, 1f);
        _panelRect.pivot = new Vector2(1f, 1f);
        _panelRect.anchoredPosition = new Vector2(GetCollapsedX(), -TopInset);
        _panelRect.sizeDelta = new Vector2(PanelWidth, HeaderHeight + 18f);

        Image background = GetComponent<Image>();
        if (background == null)
        {
            background = gameObject.AddComponent<Image>();
        }

        ConfigureSlicedImage(background, WithAlpha(MenuPalette.PanelColor, 0.92f), true);

        EnsureHeader(_panelRect);
        ConfigureContentRoot(_panelRect);
        EnsureDrawerToggle();
        _tooltipView = CreateTooltipView(_panelRect);
        _isConfigured = true;
    }

    private void EnsureHeader(RectTransform panelRect)
    {
        if (panelRect.Find("RuntimeHeader") != null)
        {
            return;
        }

        Image header = CreatePanel("RuntimeHeader", panelRect, WithAlpha(MenuPalette.SecondaryPanelColor, 0.98f), false);
        RectTransform headerRect = header.rectTransform;
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.anchoredPosition = new Vector2(0f, -8f);
        headerRect.sizeDelta = new Vector2(-14f, HeaderHeight);

        Image accent = CreatePanel("Accent", headerRect, MenuPalette.AccentColor, false);
        RectTransform accentRect = accent.rectTransform;
        accentRect.anchorMin = new Vector2(0f, 0f);
        accentRect.anchorMax = new Vector2(0f, 1f);
        accentRect.pivot = new Vector2(0f, 0.5f);
        accentRect.sizeDelta = new Vector2(6f, 0f);

        TMP_Text title = CreateText("Title", headerRect, "Отряд", 15, FontStyles.Bold, TextAlignmentOptions.TopLeft, MenuPalette.TextPrimaryColor);
        title.rectTransform.anchorMin = new Vector2(0f, 1f);
        title.rectTransform.anchorMax = new Vector2(1f, 1f);
        title.rectTransform.pivot = new Vector2(0f, 1f);
        title.rectTransform.anchoredPosition = new Vector2(12f, -6f);
        title.rectTransform.sizeDelta = new Vector2(-14f, 16f);
    }

    private void ConfigureContentRoot(RectTransform panelRect)
    {
        if (contentRoot == null)
        {
            GameObject contentObject = new("CharacterRosterContent", typeof(RectTransform));
            SetUiLayer(contentObject);
            contentObject.transform.SetParent(panelRect, false);
            contentRoot = contentObject.GetComponent<RectTransform>();
        }

        contentRoot.anchorMin = new Vector2(0f, 0f);
        contentRoot.anchorMax = new Vector2(1f, 1f);
        contentRoot.pivot = new Vector2(0.5f, 1f);
        contentRoot.anchoredPosition = Vector2.zero;
        contentRoot.offsetMin = new Vector2(8f, 8f);
        contentRoot.offsetMax = new Vector2(-8f, -(HeaderHeight + 12f));

        VerticalLayoutGroup layout = contentRoot.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = contentRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        }

        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.spacing = Mathf.Clamp(cardSpacing, 3f, 5f);
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = contentRoot.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = contentRoot.gameObject.AddComponent<ContentSizeFitter>();
        }

        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private void EnsureDrawerToggle()
    {
        Transform existing = _panelRect.Find("DrawerToggle");
        if (existing != null)
        {
            _toggleRect = existing as RectTransform;
            _toggleButton = existing.GetComponent<Button>();
            _toggleText = existing.GetComponentInChildren<TMP_Text>(true);
        }

        if (_toggleRect == null)
        {
            Image toggleBackground = CreatePanel("DrawerToggle", _panelRect, WithAlpha(MenuPalette.SecondaryPanelColor, 0.98f), true);
            _toggleRect = toggleBackground.rectTransform;
            _toggleRect.anchorMin = new Vector2(0f, 1f);
            _toggleRect.anchorMax = new Vector2(0f, 1f);
            _toggleRect.pivot = new Vector2(1f, 0.5f);
            _toggleRect.sizeDelta = new Vector2(20f, 26f);

            _toggleButton = toggleBackground.gameObject.AddComponent<Button>();
            ColorBlock colors = _toggleButton.colors;
            colors.normalColor = toggleBackground.color;
            colors.highlightedColor = Color.Lerp(toggleBackground.color, Color.white, 0.08f);
            colors.pressedColor = MenuPalette.ButtonSecondaryColor;
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = MenuPalette.DisabledButtonColor;
            colors.fadeDuration = 0.1f;
            _toggleButton.colors = colors;
            _toggleButton.targetGraphic = toggleBackground;

            _toggleText = CreateText("Arrow", _toggleRect, "<", 14, FontStyles.Bold, TextAlignmentOptions.Center, MenuPalette.TextPrimaryColor);
            _toggleText.rectTransform.anchorMin = Vector2.zero;
            _toggleText.rectTransform.anchorMax = Vector2.one;
            _toggleText.rectTransform.offsetMin = Vector2.zero;
            _toggleText.rectTransform.offsetMax = Vector2.zero;
        }

        PositionToggleForSceneLayout();
        HookToggleButton();
    }

#if UNITY_EDITOR
    [ContextMenu("Rebuild Scene Layout")]
    public void RebuildSceneLayout()
    {
        HideTooltip();
        _cards.Clear();

        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }

        contentRoot = null;
        cardTemplate = null;
        _tooltipView = null;
        _panelRect = null;
        _toggleRect = null;
        _toggleButton = null;
        _toggleText = null;
        _targetX = 0f;
        _expandedX = 0f;
        _collapsedX = 0f;
        _isExpanded = false;
        _isConfigured = false;

        ConfigurePanel();
        CreatePreviewTemplate();
        InitializeDrawerState();
        RefreshPanelHeight();

        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }
#endif

    private CharacterStatTooltipView CreateTooltipView(RectTransform panelRect)
    {
        Transform existing = panelRect.Find("RuntimeTooltip");
        if (existing != null && existing.TryGetComponent(out CharacterStatTooltipView existingTooltip))
        {
            RectTransform existingRect = existing as RectTransform;
            TMP_Text existingBody = existing.Find("Body")?.GetComponent<TMP_Text>();

            Image existingImage = existing.GetComponent<Image>();
            if (existingImage != null)
            {
                ConfigureSlicedImage(existingImage, new Color(0.48f, 0.50f, 0.55f, 0.96f), false);
            }

            if (existingRect != null)
            {
                existingRect.anchorMin = new Vector2(0.5f, 0.5f);
                existingRect.anchorMax = new Vector2(0.5f, 0.5f);
                existingRect.pivot = new Vector2(0f, 1f);
                existingRect.sizeDelta = new Vector2(148f, 0f);
            }

            VerticalLayoutGroup existingLayout = existing.GetComponent<VerticalLayoutGroup>();
            if (existingLayout != null)
            {
                existingLayout.padding = new RectOffset(8, 8, 6, 6);
                existingLayout.spacing = 0f;
            }

            ContentSizeFitter existingFitter = existing.GetComponent<ContentSizeFitter>();
            if (existingFitter != null)
            {
                existingFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                existingFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            if (existingBody == null && existingRect != null)
            {
                existingBody = CreateText("Body", existingRect, string.Empty, 11, FontStyles.Normal, TextAlignmentOptions.Left, Color.white);
            }

            if (existingBody != null)
            {
                existingBody.fontSize = 11f;
                existingBody.color = Color.white;
                existingBody.textWrappingMode = TextWrappingModes.Normal;
                SetLayoutElement(existingBody.gameObject, preferredWidth: 132f);
            }

            existingTooltip.Initialize(panelRect, existingRect, existingBody);
            return existingTooltip;
        }

        Image tooltip = CreatePanel("RuntimeTooltip", panelRect, new Color(0.48f, 0.50f, 0.55f, 0.96f), false);
        RectTransform tooltipRect = tooltip.rectTransform;
        tooltipRect.anchorMin = new Vector2(0.5f, 0.5f);
        tooltipRect.anchorMax = new Vector2(0.5f, 0.5f);
        tooltipRect.pivot = new Vector2(0f, 1f);
        tooltipRect.anchoredPosition = Vector2.zero;
        tooltipRect.sizeDelta = new Vector2(148f, 0f);

        VerticalLayoutGroup layout = tooltip.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 6, 6);
        layout.spacing = 0f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = tooltip.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        TMP_Text body = CreateText("Body", tooltipRect, string.Empty, 11, FontStyles.Normal, TextAlignmentOptions.Left, Color.white);
        body.textWrappingMode = TextWrappingModes.Normal;
        SetLayoutElement(body.gameObject, preferredWidth: 132f);

        CharacterStatTooltipView tooltipView = tooltip.gameObject.AddComponent<CharacterStatTooltipView>();
        tooltipView.Initialize(panelRect, tooltipRect, body);
        tooltip.gameObject.SetActive(false);
        return tooltipView;
    }

    private void ClearCards()
    {
        foreach (CharacterCardView card in _cards)
        {
            if (card != null)
            {
                Destroy(card.gameObject);
            }
        }

        _cards.Clear();
    }

    private void CreatePreviewTemplate()
    {
        if (contentRoot == null)
        {
            return;
        }

        GameObject templateObject = new("CharacterCardTemplate", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CharacterCardView));
        SetUiLayer(templateObject);
        templateObject.transform.SetParent(contentRoot, false);

        cardTemplate = templateObject.GetComponent<CharacterCardView>();
        cardTemplate.Prepare(this);
        cardTemplate.SetPreviewData("Самурай", 1, 90, 90, 55, 0, 0.20f, 0, 0, 0.10f);
        SetLayoutElement(cardTemplate.gameObject, preferredHeight: CharacterCardView.PreferredHeight, flexibleWidth: 1f);
        cardTemplate.gameObject.SetActive(true);
    }

    private void RefreshPanelHeight()
    {
        if (_panelRect == null || contentRoot == null)
        {
            return;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
        float contentHeight = LayoutUtility.GetPreferredHeight(contentRoot);
        float panelHeight = HeaderHeight + 20f + contentHeight;
        _panelRect.sizeDelta = new Vector2(_panelRect.sizeDelta.x, panelHeight);
    }

    private void InitializeDrawerState()
    {
        if (_panelRect == null)
        {
            return;
        }

        _expandedX = GetExpandedX();
        float currentX = _panelRect.anchoredPosition.x;
        _isExpanded = Mathf.Abs(currentX - _expandedX) < 0.01f;
        _collapsedX = _isExpanded ? GetCollapsedX() : currentX;
        _targetX = currentX;

        HookToggleButton();
        UpdateToggleVisual();
    }

    private void HookToggleButton()
    {
        if (_toggleButton != null)
        {
            _toggleButton.enabled = true;
            _toggleButton.interactable = true;
            _toggleButton.onClick.RemoveListener(ToggleDrawer);
            _toggleButton.onClick.AddListener(ToggleDrawer);
        }
    }

    private static float GetExpandedX()
    {
        return -ExpandedRightInset;
    }

    private void ToggleDrawer()
    {
        _isExpanded = !_isExpanded;
        _targetX = _isExpanded ? _expandedX : _collapsedX;
        UpdateToggleVisual();
        HideTooltip();
    }

    private void UpdateToggleVisual()
    {
        if (_toggleText != null)
        {
            _toggleText.text = _isExpanded ? ">" : "<";
        }
    }

    private void PositionToggleForSceneLayout()
    {
        if (_toggleRect == null)
        {
            return;
        }

        float yOffset = HeaderHeight + 14f + (CharacterCardView.PreferredHeight * 0.5f);
        _toggleRect.anchoredPosition = new Vector2(-4f, -yOffset);
    }

    private static float GetCollapsedX()
    {
        return GetExpandedX() + (PanelWidth - CollapsedVisibleWidth);
    }

    private static TMP_Text CreateText(
        string name,
        Transform parent,
        string content,
        float fontSize,
        FontStyles fontStyles,
        TextAlignmentOptions alignment,
        Color color)
    {
        GameObject textObject = new(name, typeof(RectTransform));
        SetUiLayer(textObject);
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = fontStyles;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }

    private static Image CreatePanel(string name, Transform parent, Color color, bool raycastTarget)
    {
        GameObject panelObject = new(name, typeof(RectTransform));
        SetUiLayer(panelObject);
        panelObject.transform.SetParent(parent, false);

        Image image = panelObject.AddComponent<Image>();
        ConfigureSlicedImage(image, color, raycastTarget);
        return image;
    }

    private static void ConfigureSlicedImage(Image image, Color color, bool raycastTarget)
    {
        image.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        image.type = Image.Type.Sliced;
        image.color = color;
        image.raycastTarget = raycastTarget;
    }

    private static void SetLayoutElement(
        GameObject target,
        float preferredWidth = -1f,
        float preferredHeight = -1f,
        float flexibleWidth = -1f,
        float flexibleHeight = -1f)
    {
        LayoutElement layoutElement = target.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = target.AddComponent<LayoutElement>();
        }

        RectTransform rectTransform = target.GetComponent<RectTransform>();

        if (preferredWidth >= 0f)
        {
            layoutElement.preferredWidth = preferredWidth;
            rectTransform?.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, preferredWidth);
        }

        if (preferredHeight >= 0f)
        {
            layoutElement.preferredHeight = preferredHeight;
            rectTransform?.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, preferredHeight);
        }

        if (flexibleWidth >= 0f)
        {
            layoutElement.flexibleWidth = flexibleWidth;
        }

        if (flexibleHeight >= 0f)
        {
            layoutElement.flexibleHeight = flexibleHeight;
        }
    }

    private static void SetUiLayer(GameObject gameObject)
    {
        int layer = LayerMask.NameToLayer("UI");
        if (layer >= 0)
        {
            gameObject.layer = layer;
        }
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        return new Color(color.r, color.g, color.b, alpha);
    }
}

public sealed class CharacterStatTooltipView : MonoBehaviour
{
    private RectTransform _boundaryRect;
    private RectTransform _tooltipRect;
    private TMP_Text _bodyText;

    public void Initialize(RectTransform boundaryRect, RectTransform tooltipRect, TMP_Text bodyText)
    {
        _boundaryRect = boundaryRect;
        _tooltipRect = tooltipRect;
        _bodyText = bodyText;
    }

    public void Show(RectTransform target, string title, string description)
    {
        if (_boundaryRect == null || _tooltipRect == null || _bodyText == null || target == null)
        {
            return;
        }

        _bodyText.text = description;
        gameObject.SetActive(true);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_tooltipRect);

        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_boundaryRect, screenPoint, null, out Vector2 localPoint);

        Vector2 size = _tooltipRect.rect.size;
        Vector2 anchoredPosition = localPoint + new Vector2(16f, -8f);
        anchoredPosition.x = Mathf.Clamp(
            anchoredPosition.x,
            _boundaryRect.rect.xMin + 12f,
            _boundaryRect.rect.xMax - size.x - 12f);
        anchoredPosition.y = Mathf.Clamp(
            anchoredPosition.y,
            _boundaryRect.rect.yMin + size.y + 12f,
            _boundaryRect.rect.yMax - 12f);

        _tooltipRect.anchoredPosition = anchoredPosition;
        _tooltipRect.SetAsLastSibling();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
