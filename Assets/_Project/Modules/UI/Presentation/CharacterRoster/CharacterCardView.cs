using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CharacterCardView : MonoBehaviour
{
    public const float PreferredHeight = 46f;
    private const float HealthBarWidth = 64f;

    [SerializeField] private Text infoText;

    private CharacterInstance _character;
    private CharacterRosterPanelView _owner;
    private TMP_Text _nameText;
    private TMP_Text _metaText;
    private TMP_Text _healthText;
    private TMP_Text _attackText;
    private TMP_Text _armorText;
    private TMP_Text _dodgeText;
    private TMP_Text _initiativeText;
    private TMP_Text _speedText;
    private TMP_Text _luckText;
    private Image _healthFill;
    private bool _layoutBuilt;

    private void Awake()
    {
        EnsureLayout();
    }

    public void Prepare(CharacterRosterPanelView owner)
    {
        _owner = owner;
        EnsureLayout();
        BindTooltipOwner();
    }

    public void SetCharacter(CharacterInstance character)
    {
        if (_character != null && _character.Model != null)
        {
            _character.Model.StatsChanged -= UpdateInfo;
        }

        _character = character;

        if (_character != null && _character.Model != null)
        {
            _character.Model.StatsChanged += UpdateInfo;
        }

        UpdateInfo();
    }

    public void SetPreviewData(
        string name,
        int level,
        int health,
        int maxHealth,
        int attack,
        int armor,
        float dodgeChance,
        int initiative,
        int speed,
        float luck)
    {
        if (_character != null && _character.Model != null)
        {
            _character.Model.StatsChanged -= UpdateInfo;
        }

        _character = null;
        EnsureLayout();
        ApplyState(name, level, health, maxHealth, attack, armor, dodgeChance, initiative, speed, luck);
    }

    private void OnDisable()
    {
        if (_character != null && _character.Model != null)
        {
            _character.Model.StatsChanged -= UpdateInfo;
        }

        _owner?.HideTooltip();
    }

    private void EnsureLayout()
    {
        if (_layoutBuilt)
        {
            return;
        }

        if (!TryCacheRuntimeLayout())
        {
            BuildRuntimeLayout();
        }

        EnsureTooltipTriggers();
        _layoutBuilt = true;
    }

    private bool TryCacheRuntimeLayout()
    {
        Transform content = transform.Find("RuntimeContent");
        if (content == null)
        {
            return false;
        }

        _nameText = content.Find("HeroColumn/Name")?.GetComponent<TMP_Text>();
        _metaText = content.Find("HeroColumn/Meta")?.GetComponent<TMP_Text>();
        _healthFill = content.Find("HeroColumn/HealthBar/Fill")?.GetComponent<Image>();
        _healthText = content.Find("StatsRow/HP/Value")?.GetComponent<TMP_Text>();
        _attackText = content.Find("StatsRow/AT/Value")?.GetComponent<TMP_Text>();
        _armorText = content.Find("StatsRow/AR/Value")?.GetComponent<TMP_Text>();
        _dodgeText = content.Find("StatsRow/DG/Value")?.GetComponent<TMP_Text>();
        _initiativeText = content.Find("StatsRow/IN/Value")?.GetComponent<TMP_Text>();
        _speedText = content.Find("StatsRow/SP/Value")?.GetComponent<TMP_Text>();
        _luckText = content.Find("StatsRow/LK/Value")?.GetComponent<TMP_Text>();

        if (_healthFill != null)
        {
            ConfigureHealthFillRect(_healthFill);
        }

        return _nameText != null &&
               _metaText != null &&
               _healthFill != null &&
               _healthText != null &&
               _attackText != null &&
               _armorText != null &&
               _dodgeText != null &&
               _initiativeText != null &&
               _speedText != null &&
               _luckText != null;
    }

    private void BuildRuntimeLayout()
    {
        RectTransform rootRect = GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 1f);
        rootRect.anchorMax = new Vector2(1f, 1f);
        rootRect.pivot = new Vector2(0.5f, 1f);
        rootRect.sizeDelta = new Vector2(0f, PreferredHeight);

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child != null && child.name != "RuntimeAccent" && child.name != "RuntimeContent")
            {
                child.gameObject.SetActive(false);
            }
        }

        Image background = GetComponent<Image>();
        if (background == null)
        {
            background = gameObject.AddComponent<Image>();
        }

        ConfigureSlicedImage(background, new Color(0.88f, 0.95f, 1f, 0.82f), false);

        Image accent = CreatePanel("RuntimeAccent", rootRect, MenuPalette.AccentColor, false);
        RectTransform accentRect = accent.rectTransform;
        accentRect.anchorMin = new Vector2(0f, 0f);
        accentRect.anchorMax = new Vector2(0f, 1f);
        accentRect.pivot = new Vector2(0f, 0.5f);
        accentRect.sizeDelta = new Vector2(5f, 0f);

        RectTransform content = CreateRectTransform("RuntimeContent", rootRect);
        content.anchorMin = Vector2.zero;
        content.anchorMax = Vector2.one;
        content.offsetMin = new Vector2(8f, 6f);
        content.offsetMax = new Vector2(-8f, -6f);

        HorizontalLayoutGroup contentLayout = content.gameObject.AddComponent<HorizontalLayoutGroup>();
        contentLayout.spacing = 8f;
        contentLayout.childAlignment = TextAnchor.MiddleLeft;
        contentLayout.childControlWidth = false;
        contentLayout.childControlHeight = false;
        contentLayout.childForceExpandWidth = false;
        contentLayout.childForceExpandHeight = false;

        RectTransform heroColumn = CreateRectTransform("HeroColumn", content);
        SetLayoutElement(heroColumn.gameObject, preferredWidth: 72f, preferredHeight: 34f);
        VerticalLayoutGroup heroLayout = heroColumn.gameObject.AddComponent<VerticalLayoutGroup>();
        heroLayout.spacing = 2f;
        heroLayout.childAlignment = TextAnchor.UpperLeft;
        heroLayout.childControlWidth = true;
        heroLayout.childControlHeight = false;
        heroLayout.childForceExpandWidth = true;
        heroLayout.childForceExpandHeight = false;

        _nameText = CreateText("Name", heroColumn, "Герой", 12, FontStyles.Bold, TextAlignmentOptions.Left, MenuPalette.TextPrimaryColor);
        SetLayoutElement(_nameText.gameObject, preferredHeight: 14f);

        _metaText = CreateText("Meta", heroColumn, "ур. 1", 9, FontStyles.Normal, TextAlignmentOptions.Left, MenuPalette.TextSecondaryColor);
        SetLayoutElement(_metaText.gameObject, preferredHeight: 10f);

        Image healthBar = CreatePanel("HealthBar", heroColumn, new Color(1f, 1f, 1f, 0.22f), false);
        SetLayoutElement(healthBar.gameObject, preferredWidth: HealthBarWidth, preferredHeight: 6f);
        RectTransform barRect = healthBar.rectTransform;
        barRect.anchorMin = new Vector2(0f, 0.5f);
        barRect.anchorMax = new Vector2(0f, 0.5f);
        barRect.pivot = new Vector2(0f, 0.5f);

        _healthFill = CreatePanel("Fill", healthBar.rectTransform, MenuPalette.AccentColor, false);
        ConfigureHealthFillRect(_healthFill);

        RectTransform statsRow = CreateRectTransform("StatsRow", content);
        SetLayoutElement(statsRow.gameObject, flexibleWidth: 1f, preferredHeight: 34f);
        HorizontalLayoutGroup statsLayout = statsRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        statsLayout.spacing = 4f;
        statsLayout.childAlignment = TextAnchor.MiddleLeft;
        statsLayout.childControlWidth = false;
        statsLayout.childControlHeight = false;
        statsLayout.childForceExpandWidth = false;
        statsLayout.childForceExpandHeight = false;

        _healthText = CreateStatSlot(statsRow, "HP", "Здоровье", "здоровье персонажа", 44f);
        _attackText = CreateStatSlot(statsRow, "AT", "Атака", "сила удара персонажа", 32f);
        _armorText = CreateStatSlot(statsRow, "AR", "Броня", "защита от физического урона", 32f);
        _dodgeText = CreateStatSlot(statsRow, "DG", "Уклонение", "шанс уклониться от атаки", 32f);
        _initiativeText = CreateStatSlot(statsRow, "IN", "Инициатива", "влияет на порядок хода", 32f);
        _speedText = CreateStatSlot(statsRow, "SP", "Скорость", "добавляет шаги к броску", 32f);
        _luckText = CreateStatSlot(statsRow, "LK", "Удача", "влияет на удачные исходы", 32f);

        BindTooltipOwner();
    }

    private void BindTooltipOwner()
    {
        CharacterStatTooltipTrigger[] triggers = GetComponentsInChildren<CharacterStatTooltipTrigger>(true);
        for (int i = 0; i < triggers.Length; i++)
        {
            triggers[i].SetOwner(_owner);
        }
    }

    private void EnsureTooltipTriggers()
    {
        Transform content = transform.Find("RuntimeContent");
        if (content == null)
        {
            return;
        }

        EnsureTooltipTrigger(content, "StatsRow/HP", "Здоровье", "здоровье персонажа");
        EnsureTooltipTrigger(content, "StatsRow/AT", "Атака", "сила удара персонажа");
        EnsureTooltipTrigger(content, "StatsRow/AR", "Броня", "защита от физического урона");
        EnsureTooltipTrigger(content, "StatsRow/DG", "Уклонение", "шанс уклониться от атаки");
        EnsureTooltipTrigger(content, "StatsRow/IN", "Инициатива", "влияет на порядок хода");
        EnsureTooltipTrigger(content, "StatsRow/SP", "Скорость", "добавляет шаги к броску");
        EnsureTooltipTrigger(content, "StatsRow/LK", "Удача", "влияет на удачные исходы");
    }

    private static void EnsureTooltipTrigger(Transform root, string path, string title, string description)
    {
        Transform slot = root.Find(path);
        if (slot == null)
        {
            return;
        }

        Image slotImage = slot.GetComponent<Image>();
        if (slotImage != null)
        {
            slotImage.raycastTarget = true;
        }

        CharacterStatTooltipTrigger trigger = slot.GetComponent<CharacterStatTooltipTrigger>();
        if (trigger == null)
        {
            trigger = slot.gameObject.AddComponent<CharacterStatTooltipTrigger>();
        }

        trigger.Initialize(title, description);
    }

    private TMP_Text CreateStatSlot(Transform parent, string shortName, string fullName, string description, float width)
    {
        Image slot = CreatePanel(shortName, parent, new Color(1f, 1f, 1f, 0.40f), true);
        SetLayoutElement(slot.gameObject, preferredWidth: width, preferredHeight: 34f);

        VerticalLayoutGroup layout = slot.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(0, 0, 3, 3);
        layout.spacing = 0f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        TMP_Text label = CreateText("Label", slot.rectTransform, shortName, 9, FontStyles.Bold, TextAlignmentOptions.Center, MenuPalette.TextSecondaryColor);
        SetLayoutElement(label.gameObject, preferredHeight: 9f);

        TMP_Text value = CreateText("Value", slot.rectTransform, "0", 11, FontStyles.Bold, TextAlignmentOptions.Center, MenuPalette.TextPrimaryColor);
        SetLayoutElement(value.gameObject, preferredHeight: 13f);

        CharacterStatTooltipTrigger trigger = slot.gameObject.AddComponent<CharacterStatTooltipTrigger>();
        trigger.Initialize(fullName, description);

        return value;
    }

    private void UpdateInfo()
    {
        if (_character == null || _character.Model == null)
        {
            return;
        }

        Character model = _character.Model;
        ApplyState(
            model.Name,
            model.Level,
            model.Health,
            model.MaxHealth,
            model.Attack,
            model.Armor,
            model.DodgeChance,
            model.Initiative,
            model.Speed,
            model.Luck);
    }

    private void ApplyState(
        string name,
        int level,
        int health,
        int maxHealth,
        int attack,
        int armor,
        float dodgeChance,
        int initiative,
        int speed,
        float luck)
    {
        _nameText.text = name;
        _metaText.text = $"ур. {level}";
        _healthText.text = $"{health}/{maxHealth}";
        _attackText.text = attack.ToString();
        _armorText.text = armor.ToString();
        _dodgeText.text = $"{dodgeChance * 100f:0}%";
        _initiativeText.text = initiative.ToString();
        _speedText.text = speed.ToString();
        _luckText.text = $"{luck * 100f:0}%";

        if (_healthFill != null)
        {
            float healthRatio = Mathf.Clamp01((float)health / Mathf.Max(1, maxHealth));
            _healthFill.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, HealthBarWidth * healthRatio);
            _healthFill.color = Color.Lerp(MenuPalette.DangerButtonColor, MenuPalette.AccentColor, healthRatio);
        }
    }

    private static void ConfigureHealthFillRect(Image fillImage)
    {
        if (fillImage == null)
        {
            return;
        }

        RectTransform fillRect = fillImage.rectTransform;
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.anchoredPosition = Vector2.zero;
        fillRect.sizeDelta = new Vector2(HealthBarWidth, 0f);
        fillImage.type = Image.Type.Sliced;
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
        RectTransform rect = CreateRectTransform(name, parent);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
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
        RectTransform rect = CreateRectTransform(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        ConfigureSlicedImage(image, color, raycastTarget);
        return image;
    }

    private static RectTransform CreateRectTransform(string name, Transform parent)
    {
        GameObject gameObject = new(name, typeof(RectTransform));
        SetUiLayer(gameObject);
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
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
}

public sealed class CharacterStatTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private CharacterRosterPanelView _owner;
    private string _title;
    private string _description;

    public void Initialize(string title, string description)
    {
        _title = title;
        _description = description;
    }

    public void SetOwner(CharacterRosterPanelView owner)
    {
        _owner = owner;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_owner == null)
        {
            return;
        }

        _owner.ShowTooltip(transform as RectTransform, _title, _description);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _owner?.HideTooltip();
    }

    private void OnDisable()
    {
        _owner?.HideTooltip();
    }
}
