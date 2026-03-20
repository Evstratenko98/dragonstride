using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameScreenView : MonoBehaviour
{
    private static readonly string[] GeneratedRootObjectNames =
    {
        "RuntimeHud",
        "RuntimeTurnState",
        "RuntimeActionBar",
        "RuntimeMenuOverlay"
    };

    private static readonly string[] LegacyRootObjectNames =
    {
        "HUD",
        "ActionPanel",
        "CharacterButton"
    };

    [SerializeField] private Button characaterButton;
    [SerializeField] private Button interactCellButton;
    [SerializeField] private Button endTurnButton;
    [SerializeField] private Button tradeButton;
    [SerializeField] private Button attackButton;
    [SerializeField] private Toggle followPlayerToggle;
    [SerializeField] private Toggle toggleFog;
    [SerializeField] private Toggle toggleHiddenCells;
    [SerializeField] private TMP_Text currentPlayerText;
    [SerializeField] private TMP_Text turnStateText;
    [SerializeField] private TMP_Text stepsText;

    private Button _resumeButton;
    private Button _settingsButton;
    private Button _settingsBackButton;
    private Button _exitButton;
    private TMP_Text _roundText;
    private GameObject _menuOverlay;
    private GameObject _menuContent;
    private GameObject _settingsContent;
    private RectTransform _gameScreenRoot;
    private bool _isBuilt;

    public Button CharacaterButton => characaterButton;
    public Button OpenCellButton => interactCellButton;
    public Button EndTurnButton => endTurnButton;
    public Button TradeButton => tradeButton;
    public Button AttackButton => attackButton;
    public Toggle FollowPlayerToggle => followPlayerToggle;
    public Toggle FogToggle => toggleFog;
    public Toggle HiddenCellsToggle => toggleHiddenCells;

    private void Awake()
    {
        BuildRuntimeLayout();
        BindMenuActions();
        CloseMenu();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying || !gameObject.scene.IsValid())
        {
            return;
        }

        _gameScreenRoot = ResolveGameScreenRoot();
        if (_gameScreenRoot == null)
        {
            return;
        }

        if (!NeedsSceneLayoutRebuild())
        {
            return;
        }

        UnityEditor.EditorApplication.delayCall += RebuildSceneLayoutIfAlive;
    }
#endif

    private void Update()
    {
        if (!_isBuilt || _gameScreenRoot == null || !_gameScreenRoot.gameObject.activeInHierarchy)
        {
            return;
        }

        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            return;
        }

        if (_menuOverlay != null && _menuOverlay.activeSelf)
        {
            CloseMenu();
            return;
        }

        OpenMenu();
    }

    private void OnDestroy()
    {
        UnbindMenuActions();
        GameMenuPauseState.Reset();
    }

    public void SetRound(int roundNumber)
    {
        if (_roundText == null)
        {
            return;
        }

        _roundText.text = roundNumber > 0 ? roundNumber.ToString() : "—";
    }

    public void SetCurrentPlayer(string playerName)
    {
        if (currentPlayerText == null)
        {
            return;
        }

        currentPlayerText.text = string.IsNullOrWhiteSpace(playerName) ? "—" : playerName;
    }

    public void SetTurnState(string state)
    {
        if (turnStateText == null)
        {
            return;
        }

        turnStateText.text = string.IsNullOrWhiteSpace(state) ? "Ожидание хода" : state;
    }

    public void SetSteps(int remaining, int total)
    {
        if (stepsText == null)
        {
            return;
        }

        stepsText.text = total > 0 ? $"{remaining}/{total}" : "—";
    }

    private void BuildRuntimeLayout()
    {
        if (_isBuilt)
        {
            return;
        }

        _gameScreenRoot = ResolveGameScreenRoot();
        if (_gameScreenRoot == null)
        {
            Debug.LogWarning("[GameScreenView] GameScreen root was not found.");
            return;
        }

        if (TryCacheExistingLayout())
        {
            _isBuilt = true;
            return;
        }

        bool followDefault = followPlayerToggle != null && followPlayerToggle.isOn;
        bool fogDefault = toggleFog != null && toggleFog.isOn;
        bool hiddenCellsDefault = toggleHiddenCells != null && toggleHiddenCells.isOn;

        DeleteNamedChildren(LegacyRootObjectNames, immediate: false);
        DeleteNamedChildren(GeneratedRootObjectNames, immediate: false);
        BuildHud();
        BuildActionBar();
        BuildMenuOverlay(followDefault, fogDefault, hiddenCellsDefault);
        if (_menuOverlay != null)
        {
            _menuOverlay.SetActive(false);
        }

        _isBuilt = true;
    }

    private RectTransform ResolveGameScreenRoot()
    {
        if (transform.parent != null)
        {
            Transform sibling = transform.parent.Find("GameScreen");
            if (sibling is RectTransform siblingRect)
            {
                return siblingRect;
            }
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            return canvas.GetComponent<RectTransform>();
        }

        return null;
    }

#if UNITY_EDITOR
    private bool NeedsSceneLayoutRebuild()
    {
        if (_gameScreenRoot.Find("RuntimeTurnState") != null)
        {
            return true;
        }

        for (int i = 0; i < LegacyRootObjectNames.Length; i++)
        {
            if (_gameScreenRoot.Find(LegacyRootObjectNames[i]) != null)
            {
                return true;
            }
        }

        return !TryCacheExistingLayout();
    }

    private void RebuildSceneLayoutIfAlive()
    {
        if (this == null || Application.isPlaying || !gameObject.scene.IsValid())
        {
            return;
        }

        _gameScreenRoot = ResolveGameScreenRoot();
        if (_gameScreenRoot == null || !NeedsSceneLayoutRebuild())
        {
            return;
        }

        RebuildSceneLayout();
    }
#endif

    private bool TryCacheExistingLayout()
    {
        if (_gameScreenRoot == null)
        {
            return false;
        }

        Transform hud = _gameScreenRoot.Find("RuntimeHud");
        Transform actionBar = _gameScreenRoot.Find("RuntimeActionBar");
        Transform menuOverlay = _gameScreenRoot.Find("RuntimeMenuOverlay");

        if (hud == null || actionBar == null || menuOverlay == null)
        {
            return false;
        }

        _roundText = FindComponent<TMP_Text>(hud, "InfoCard_Раунд/Value");
        currentPlayerText = FindComponent<TMP_Text>(hud, "InfoCard_Ход/Value");
        stepsText = FindComponent<TMP_Text>(hud, "InfoCard_Шаги/Value");
        turnStateText = null;

        characaterButton = FindComponent<Button>(actionBar, "ActionBarShell/CharacterActionButton");
        interactCellButton = FindComponent<Button>(actionBar, "ActionBarShell/OpenCellActionButton");
        attackButton = FindComponent<Button>(actionBar, "ActionBarShell/AttackActionButton");
        endTurnButton = FindComponent<Button>(actionBar, "ActionBarShell/EndTurnActionButton");
        tradeButton = FindComponent<Button>(actionBar, "ActionBarShell/TradeActionButton");

        _menuOverlay = menuOverlay.gameObject;
        _menuContent = FindTransform(menuOverlay, "RuntimeMenuPanel/MenuContent")?.gameObject;
        _settingsContent = FindTransform(menuOverlay, "RuntimeMenuPanel/SettingsContent")?.gameObject;
        _resumeButton = FindComponent<Button>(menuOverlay, "RuntimeMenuPanel/MenuContent/ResumeButton");
        _settingsButton = FindComponent<Button>(menuOverlay, "RuntimeMenuPanel/MenuContent/SettingsButton");
        _exitButton = FindComponent<Button>(menuOverlay, "RuntimeMenuPanel/MenuContent/ExitButton");
        _settingsBackButton = FindComponent<Button>(menuOverlay, "RuntimeMenuPanel/SettingsContent/SettingsBackButton");
        followPlayerToggle = FindComponent<Toggle>(menuOverlay, "RuntimeMenuPanel/SettingsContent/ToggleRow_Камера следует за игроком/Switch");
        toggleFog = FindComponent<Toggle>(menuOverlay, "RuntimeMenuPanel/SettingsContent/ToggleRow_Туман войны/Switch");
        toggleHiddenCells = FindComponent<Toggle>(menuOverlay, "RuntimeMenuPanel/SettingsContent/ToggleRow_Скрывать закрытые ячейки/Switch");

        return _roundText != null &&
               currentPlayerText != null &&
               stepsText != null &&
               characaterButton != null &&
               interactCellButton != null &&
               attackButton != null &&
               endTurnButton != null &&
               _menuOverlay != null &&
               _menuContent != null &&
               _settingsContent != null &&
               _resumeButton != null &&
               _settingsButton != null &&
               _settingsBackButton != null &&
               _exitButton != null &&
               followPlayerToggle != null &&
               toggleFog != null &&
               toggleHiddenCells != null;
    }

    private void BuildHud()
    {
        RectTransform hudRect = CreateRectTransform("RuntimeHud", _gameScreenRoot);
        hudRect.anchorMin = new Vector2(0f, 1f);
        hudRect.anchorMax = new Vector2(0f, 1f);
        hudRect.pivot = new Vector2(0f, 1f);
        hudRect.anchoredPosition = new Vector2(14f, -14f);

        HorizontalLayoutGroup hudLayout = hudRect.gameObject.AddComponent<HorizontalLayoutGroup>();
        hudLayout.padding = new RectOffset(0, 0, 0, 0);
        hudLayout.spacing = 5f;
        hudLayout.childAlignment = TextAnchor.UpperLeft;
        hudLayout.childControlWidth = false;
        hudLayout.childControlHeight = false;
        hudLayout.childForceExpandWidth = false;
        hudLayout.childForceExpandHeight = false;

        ContentSizeFitter hudFitter = hudRect.gameObject.AddComponent<ContentSizeFitter>();
        hudFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        hudFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _roundText = CreateInfoCard(hudRect, "Раунд", new Vector2(56f, 34f), MenuPalette.AccentColor);
        currentPlayerText = CreateInfoCard(hudRect, "Ход", new Vector2(134f, 34f), MenuPalette.TextPrimaryColor);
        stepsText = CreateInfoCard(hudRect, "Шаги", new Vector2(66f, 34f), MenuPalette.ButtonSecondaryColor);
    }

    private void BuildActionBar()
    {
        RectTransform panelRect = CreateRectTransform("RuntimeActionBar", _gameScreenRoot);
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(0f, 0f);
        panelRect.pivot = new Vector2(0f, 0f);
        panelRect.anchoredPosition = new Vector2(18f, 18f);

        Image shell = CreatePanel("ActionBarShell", panelRect, WithAlpha(MenuPalette.PanelColor, 0.98f), false);
        RectTransform shellRect = shell.rectTransform;

        HorizontalLayoutGroup panelLayout = shellRect.gameObject.AddComponent<HorizontalLayoutGroup>();
        panelLayout.padding = new RectOffset(8, 8, 8, 8);
        panelLayout.spacing = 6f;
        panelLayout.childAlignment = TextAnchor.LowerLeft;
        panelLayout.childControlWidth = false;
        panelLayout.childControlHeight = false;
        panelLayout.childForceExpandWidth = false;
        panelLayout.childForceExpandHeight = false;

        ContentSizeFitter panelFitter = shellRect.gameObject.AddComponent<ContentSizeFitter>();
        panelFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        panelFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        characaterButton = CreateButton(
            "CharacterActionButton",
            shellRect,
            "Персонаж",
            MenuPalette.ButtonSecondaryColor,
            MenuPalette.ButtonSecondaryPressedColor,
            MenuPalette.ButtonLabelColor,
            new Vector2(108f, 36f));

        interactCellButton = CreateButton(
            "OpenCellActionButton",
            shellRect,
            "Открыть",
            MenuPalette.SecondaryPanelColor,
            MenuPalette.ButtonSecondaryColor,
            MenuPalette.TextPrimaryColor,
            new Vector2(108f, 36f));

        attackButton = CreateButton(
            "AttackActionButton",
            shellRect,
            "Атака",
            MenuPalette.SecondaryPanelColor,
            MenuPalette.ButtonSecondaryColor,
            MenuPalette.TextPrimaryColor,
            new Vector2(108f, 36f));

        endTurnButton = CreateButton(
            "EndTurnActionButton",
            shellRect,
            "Завершить ход",
            MenuPalette.SecondaryPanelColor,
            MenuPalette.ButtonSecondaryColor,
            MenuPalette.TextPrimaryColor,
            new Vector2(108f, 36f));
    }

    private void BuildMenuOverlay(bool followDefault, bool fogDefault, bool hiddenCellsDefault)
    {
        Image overlay = CreatePanel("RuntimeMenuOverlay", _gameScreenRoot, new Color(0.10f, 0.14f, 0.24f, 0.68f), true);
        RectTransform overlayRect = overlay.rectTransform;
        Stretch(overlayRect, Vector2.zero, Vector2.zero);
        _menuOverlay = overlay.gameObject;

        Image modalPanel = CreatePanel("RuntimeMenuPanel", overlayRect, WithAlpha(MenuPalette.PanelColor, 0.99f), true);
        RectTransform modalRect = modalPanel.rectTransform;
        modalRect.anchorMin = new Vector2(0.5f, 0.5f);
        modalRect.anchorMax = new Vector2(0.5f, 0.5f);
        modalRect.pivot = new Vector2(0.5f, 0.5f);
        modalRect.anchoredPosition = Vector2.zero;
        modalRect.sizeDelta = new Vector2(468f, 372f);

        TMP_Text title = CreateText(
            "MenuTitle",
            modalRect,
            "Игровое меню",
            32,
            FontStyles.Bold,
            TextAlignmentOptions.TopLeft,
            MenuPalette.TextPrimaryColor);
        title.rectTransform.anchorMin = new Vector2(0f, 1f);
        title.rectTransform.anchorMax = new Vector2(1f, 1f);
        title.rectTransform.pivot = new Vector2(0f, 1f);
        title.rectTransform.anchoredPosition = new Vector2(24f, -24f);
        title.rectTransform.sizeDelta = new Vector2(-48f, 40f);

        TMP_Text subtitle = CreateText(
            "MenuSubtitle",
            modalRect,
            "Пауза партии и быстрый доступ к основным настройкам.",
            18,
            FontStyles.Normal,
            TextAlignmentOptions.TopLeft,
            MenuPalette.TextSecondaryColor);
        subtitle.rectTransform.anchorMin = new Vector2(0f, 1f);
        subtitle.rectTransform.anchorMax = new Vector2(1f, 1f);
        subtitle.rectTransform.pivot = new Vector2(0f, 1f);
        subtitle.rectTransform.anchoredPosition = new Vector2(24f, -62f);
        subtitle.rectTransform.sizeDelta = new Vector2(-48f, 30f);

        _menuContent = CreateContainer("MenuContent", modalRect);
        RectTransform menuContentRect = _menuContent.GetComponent<RectTransform>();
        Stretch(menuContentRect, new Vector2(22f, 108f), new Vector2(-22f, -22f));
        VerticalLayoutGroup menuLayout = _menuContent.AddComponent<VerticalLayoutGroup>();
        menuLayout.spacing = 10f;
        menuLayout.childAlignment = TextAnchor.UpperCenter;
        menuLayout.childControlWidth = true;
        menuLayout.childControlHeight = false;
        menuLayout.childForceExpandWidth = true;
        menuLayout.childForceExpandHeight = false;

        _resumeButton = CreateButton(
            "ResumeButton",
            menuContentRect,
            "Продолжить",
            MenuPalette.AccentColor,
            MenuPalette.AccentPressedColor,
            MenuPalette.TextPrimaryColor,
            new Vector2(0f, 58f));
        SetLayoutElement(_resumeButton.gameObject, preferredHeight: 58f);

        _settingsButton = CreateButton(
            "SettingsButton",
            menuContentRect,
            "Настройки",
            MenuPalette.ButtonSecondaryColor,
            MenuPalette.ButtonSecondaryPressedColor,
            MenuPalette.ButtonLabelColor,
            new Vector2(0f, 54f));
        SetLayoutElement(_settingsButton.gameObject, preferredHeight: 54f);

        _exitButton = CreateButton(
            "ExitButton",
            menuContentRect,
            "Выйти в главное меню",
            MenuPalette.DangerButtonColor,
            MenuPalette.DangerButtonPressedColor,
            MenuPalette.ButtonLabelColor,
            new Vector2(0f, 54f));
        SetLayoutElement(_exitButton.gameObject, preferredHeight: 54f);

        _settingsContent = CreateContainer("SettingsContent", modalRect);
        RectTransform settingsRect = _settingsContent.GetComponent<RectTransform>();
        Stretch(settingsRect, new Vector2(22f, 108f), new Vector2(-22f, -22f));
        VerticalLayoutGroup settingsLayout = _settingsContent.AddComponent<VerticalLayoutGroup>();
        settingsLayout.spacing = 8f;
        settingsLayout.childAlignment = TextAnchor.UpperCenter;
        settingsLayout.childControlWidth = true;
        settingsLayout.childControlHeight = false;
        settingsLayout.childForceExpandWidth = true;
        settingsLayout.childForceExpandHeight = false;
        _settingsContent.SetActive(false);

        TMP_Text settingsTitle = CreateText(
            "SettingsTitle",
            settingsRect,
            "Настройки партии",
            22,
            FontStyles.Bold,
            TextAlignmentOptions.Left,
            MenuPalette.TextPrimaryColor);
        SetLayoutElement(settingsTitle.gameObject, preferredHeight: 28f);

        TMP_Text settingsHint = CreateText(
            "SettingsHint",
            settingsRect,
            "Те же игровые переключатели, что были в старом HUD, но в отдельном меню.",
            14,
            FontStyles.Normal,
            TextAlignmentOptions.Left,
            MenuPalette.TextSecondaryColor);
        settingsHint.textWrappingMode = TextWrappingModes.Normal;
        SetLayoutElement(settingsHint.gameObject, preferredHeight: 38f);

        followPlayerToggle = CreateSettingsToggleRow(
            settingsRect,
            "Камера следует за игроком",
            "Автоматически фокусирует камеру на активном герое.",
            followDefault);
        toggleFog = CreateSettingsToggleRow(
            settingsRect,
            "Туман войны",
            "Скрывает непросмотренную часть поля до разведки.",
            fogDefault);
        toggleHiddenCells = CreateSettingsToggleRow(
            settingsRect,
            "Скрывать закрытые ячейки",
            "Прячет содержимое неоткрытых клеток до взаимодействия.",
            hiddenCellsDefault);

        _settingsBackButton = CreateButton(
            "SettingsBackButton",
            settingsRect,
            "Назад",
            MenuPalette.ButtonSecondaryColor,
            MenuPalette.ButtonSecondaryPressedColor,
            MenuPalette.ButtonLabelColor,
            new Vector2(0f, 50f));
        SetLayoutElement(_settingsBackButton.gameObject, preferredHeight: 50f);
    }

#if UNITY_EDITOR
    [ContextMenu("Rebuild Scene Layout")]
    public void RebuildSceneLayout()
    {
        _gameScreenRoot = ResolveGameScreenRoot();
        if (_gameScreenRoot == null)
        {
            Debug.LogWarning("[GameScreenView] GameScreen root was not found.");
            return;
        }

        bool followDefault = followPlayerToggle != null && followPlayerToggle.isOn;
        bool fogDefault = toggleFog != null && toggleFog.isOn;
        bool hiddenCellsDefault = toggleHiddenCells != null && toggleHiddenCells.isOn;

        ResetCachedReferences();
        DeleteNamedChildren(GeneratedRootObjectNames, immediate: true);
        DeleteNamedChildren(LegacyRootObjectNames, immediate: true);

        BuildHud();
        BuildActionBar();
        BuildMenuOverlay(followDefault, fogDefault, hiddenCellsDefault);

        if (_menuOverlay != null)
        {
            _menuOverlay.SetActive(false);
        }

        _isBuilt = true;
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }
#endif

    private void BindMenuActions()
    {
        if (_resumeButton != null)
        {
            _resumeButton.onClick.AddListener(CloseMenu);
        }

        if (_settingsButton != null)
        {
            _settingsButton.onClick.AddListener(ShowSettings);
        }

        if (_settingsBackButton != null)
        {
            _settingsBackButton.onClick.AddListener(ShowMainMenu);
        }

        if (_exitButton != null)
        {
            _exitButton.onClick.AddListener(ExitToMainMenu);
        }
    }

    private void UnbindMenuActions()
    {
        if (_resumeButton != null)
        {
            _resumeButton.onClick.RemoveListener(CloseMenu);
        }

        if (_settingsButton != null)
        {
            _settingsButton.onClick.RemoveListener(ShowSettings);
        }

        if (_settingsBackButton != null)
        {
            _settingsBackButton.onClick.RemoveListener(ShowMainMenu);
        }

        if (_exitButton != null)
        {
            _exitButton.onClick.RemoveListener(ExitToMainMenu);
        }
    }

    private void OpenMenu()
    {
        if (_menuOverlay == null)
        {
            return;
        }

        _menuOverlay.SetActive(true);
        ShowMainMenu();
        GameMenuPauseState.SetMenuOpen(true);
    }

    private void CloseMenu()
    {
        if (_menuOverlay != null)
        {
            _menuOverlay.SetActive(false);
        }

        GameMenuPauseState.SetMenuOpen(false);
    }

    private void ShowSettings()
    {
        if (_menuContent != null)
        {
            _menuContent.SetActive(false);
        }

        if (_settingsContent != null)
        {
            _settingsContent.SetActive(true);
        }
    }

    private void ShowMainMenu()
    {
        if (_menuContent != null)
        {
            _menuContent.SetActive(true);
        }

        if (_settingsContent != null)
        {
            _settingsContent.SetActive(false);
        }
    }

    private void ExitToMainMenu()
    {
        GameMenuPauseState.Reset();
        SceneManager.LoadScene(GameSceneNames.MainMenuScene);
    }

    private void ResetCachedReferences()
    {
        characaterButton = null;
        interactCellButton = null;
        endTurnButton = null;
        tradeButton = null;
        attackButton = null;
        followPlayerToggle = null;
        toggleFog = null;
        toggleHiddenCells = null;
        currentPlayerText = null;
        turnStateText = null;
        stepsText = null;

        _resumeButton = null;
        _settingsButton = null;
        _settingsBackButton = null;
        _exitButton = null;
        _roundText = null;
        _menuOverlay = null;
        _menuContent = null;
        _settingsContent = null;
        _isBuilt = false;
    }

    private void DeleteNamedChildren(IReadOnlyList<string> childNames, bool immediate)
    {
        if (_gameScreenRoot == null || childNames == null)
        {
            return;
        }

        for (int i = 0; i < childNames.Count; i++)
        {
            Transform child = _gameScreenRoot.Find(childNames[i]);
            if (child == null)
            {
                continue;
            }

            if (immediate)
            {
                DestroyImmediate(child.gameObject);
            }
            else
            {
                Destroy(child.gameObject);
            }
        }
    }

    private static Transform FindTransform(Transform root, string path)
    {
        return root == null ? null : root.Find(path);
    }

    private static T FindComponent<T>(Transform root, string path) where T : Component
    {
        Transform target = FindTransform(root, path);
        return target != null ? target.GetComponent<T>() : null;
    }

    private Toggle CreateSettingsToggleRow(Transform parent, string title, string subtitle, bool initialValue)
    {
        Image rowPanel = CreatePanel($"ToggleRow_{title}", parent, WithAlpha(MenuPalette.SecondaryPanelColor, 0.94f), false);
        SetLayoutElement(rowPanel.gameObject, preferredHeight: 66f);

        HorizontalLayoutGroup rowLayout = rowPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
        rowLayout.padding = new RectOffset(14, 14, 10, 10);
        rowLayout.spacing = 12f;
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.childControlWidth = false;
        rowLayout.childControlHeight = false;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;

        GameObject info = CreateContainer("Info", rowPanel.rectTransform);
        VerticalLayoutGroup infoLayout = info.AddComponent<VerticalLayoutGroup>();
        infoLayout.spacing = 4f;
        infoLayout.childAlignment = TextAnchor.UpperLeft;
        infoLayout.childControlWidth = true;
        infoLayout.childControlHeight = false;
        infoLayout.childForceExpandWidth = true;
        infoLayout.childForceExpandHeight = false;
        SetLayoutElement(info, preferredWidth: 252f, flexibleWidth: 1f);

        TMP_Text titleText = CreateText("Title", info.transform, title, 18, FontStyles.Bold, TextAlignmentOptions.Left, MenuPalette.TextPrimaryColor);
        SetLayoutElement(titleText.gameObject, preferredHeight: 22f);

        TMP_Text subtitleText = CreateText("Subtitle", info.transform, subtitle, 13, FontStyles.Normal, TextAlignmentOptions.Left, MenuPalette.TextSecondaryColor);
        subtitleText.textWrappingMode = TextWrappingModes.Normal;
        SetLayoutElement(subtitleText.gameObject, preferredHeight: 28f);

        RectTransform switchRoot = CreateRectTransform("Switch", rowPanel.rectTransform);
        SetLayoutElement(switchRoot.gameObject, preferredWidth: 120f, preferredHeight: 42f);

        Toggle toggle = switchRoot.gameObject.AddComponent<Toggle>();
        toggle.isOn = initialValue;

        Image track = switchRoot.gameObject.AddComponent<Image>();
        ConfigureSlicedImage(track, MenuPalette.DisabledButtonColor, true);
        toggle.targetGraphic = track;

        TMP_Text stateText = CreateText("State", switchRoot, string.Empty, 14, FontStyles.Bold, TextAlignmentOptions.Center, MenuPalette.TextPrimaryColor);
        Stretch(stateText.rectTransform, new Vector2(12f, 0f), new Vector2(-12f, 0f));

        Image knob = CreatePanel("Knob", switchRoot, Color.white, false);
        RectTransform knobRect = knob.rectTransform;
        knobRect.anchorMin = new Vector2(0f, 0.5f);
        knobRect.anchorMax = new Vector2(0f, 0.5f);
        knobRect.pivot = new Vector2(0.5f, 0.5f);
        knobRect.sizeDelta = new Vector2(30f, 30f);

        SwitchToggleVisual visual = switchRoot.gameObject.AddComponent<SwitchToggleVisual>();
        visual.Initialize(toggle, track, knobRect, stateText);

        return toggle;
    }

    private static TMP_Text CreateInfoCard(Transform parent, string label, Vector2 size, Color accentColor)
    {
        Image card = CreatePanel($"InfoCard_{label}", parent, WithAlpha(MenuPalette.SecondaryPanelColor, 0.98f), false);
        SetLayoutElement(card.gameObject, preferredWidth: size.x, preferredHeight: size.y);

        Image accent = CreatePanel("Accent", card.rectTransform, accentColor, false);
        RectTransform accentRect = accent.rectTransform;
        accentRect.anchorMin = new Vector2(0f, 0f);
        accentRect.anchorMax = new Vector2(0f, 1f);
        accentRect.pivot = new Vector2(0f, 0.5f);
        accentRect.sizeDelta = new Vector2(6f, 0f);

        TMP_Text labelText = CreateText("Label", card.rectTransform, label.ToUpperInvariant(), 8, FontStyles.Bold, TextAlignmentOptions.TopLeft, MenuPalette.TextSecondaryColor);
        labelText.rectTransform.anchorMin = new Vector2(0f, 1f);
        labelText.rectTransform.anchorMax = new Vector2(1f, 1f);
        labelText.rectTransform.pivot = new Vector2(0f, 1f);
        labelText.rectTransform.anchoredPosition = new Vector2(10f, -6f);
        labelText.rectTransform.sizeDelta = new Vector2(-14f, 11f);

        TMP_Text valueText = CreateText("Value", card.rectTransform, "—", 14, FontStyles.Bold, TextAlignmentOptions.BottomLeft, MenuPalette.TextPrimaryColor);
        valueText.rectTransform.anchorMin = new Vector2(0f, 0f);
        valueText.rectTransform.anchorMax = new Vector2(1f, 1f);
        valueText.rectTransform.pivot = new Vector2(0f, 0.5f);
        valueText.rectTransform.anchoredPosition = new Vector2(10f, -1f);
        valueText.rectTransform.sizeDelta = new Vector2(-14f, -11f);

        return valueText;
    }

    private static Button CreateButton(
        string name,
        Transform parent,
        string label,
        Color backgroundColor,
        Color pressedColor,
        Color labelColor,
        Vector2 size)
    {
        RectTransform rect = CreateRectTransform(name, parent);
        rect.sizeDelta = size;

        Image image = rect.gameObject.AddComponent<Image>();
        ConfigureSlicedImage(image, backgroundColor, true);

        Button button = rect.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = backgroundColor;
        colors.highlightedColor = Color.Lerp(backgroundColor, Color.white, 0.08f);
        colors.pressedColor = pressedColor;
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = MenuPalette.DisabledButtonColor;
        colors.fadeDuration = 0.1f;
        button.colors = colors;
        button.targetGraphic = image;

        float fontSize = 14f;
        TMP_Text text = CreateText("Label", rect, label, fontSize, FontStyles.Bold, TextAlignmentOptions.Center, labelColor);
        Stretch(text.rectTransform, new Vector2(8f, 6f), new Vector2(-8f, -6f));

        return button;
    }

    private static Image CreatePanel(string name, Transform parent, Color color, bool raycastTarget)
    {
        RectTransform rect = CreateRectTransform(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        ConfigureSlicedImage(image, color, raycastTarget);
        return image;
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
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        return text;
    }

    private static GameObject CreateContainer(string name, Transform parent)
    {
        RectTransform rect = CreateRectTransform(name, parent);
        return rect.gameObject;
    }

    private static RectTransform CreateRectTransform(string name, Transform parent)
    {
        GameObject gameObject = new(name, typeof(RectTransform));
        SetUiLayer(gameObject);
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static void ConfigureSlicedImage(Image image, Color color, bool raycastTarget)
    {
        image.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        image.type = Image.Type.Sliced;
        image.color = color;
        image.raycastTarget = raycastTarget;
    }

    private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
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

public static class GameMenuPauseState
{
    public static bool IsMenuOpen { get; private set; }

    public static void SetMenuOpen(bool isOpen)
    {
        IsMenuOpen = isOpen;
        Time.timeScale = isOpen ? 0f : 1f;
    }

    public static void Reset()
    {
        SetMenuOpen(false);
    }
}

public sealed class SwitchToggleVisual : MonoBehaviour
{
    private Toggle _toggle;
    private Image _track;
    private RectTransform _knob;
    private TMP_Text _stateText;

    public void Initialize(Toggle toggle, Image track, RectTransform knob, TMP_Text stateText)
    {
        _toggle = toggle;
        _track = track;
        _knob = knob;
        _stateText = stateText;

        _toggle.onValueChanged.AddListener(OnToggleChanged);
        OnToggleChanged(_toggle.isOn);
    }

    private void OnDestroy()
    {
        if (_toggle != null)
        {
            _toggle.onValueChanged.RemoveListener(OnToggleChanged);
        }
    }

    private void OnToggleChanged(bool isOn)
    {
        if (_track != null)
        {
            _track.color = isOn
                ? MenuPalette.AccentColor
                : MenuPalette.DisabledButtonColor;
        }

        if (_knob != null)
        {
            _knob.anchoredPosition = isOn
                ? new Vector2(97f, 0f)
                : new Vector2(23f, 0f);
        }

        if (_stateText != null)
        {
            _stateText.text = isOn ? "ВКЛ" : "ВЫКЛ";
            _stateText.alignment = isOn ? TextAlignmentOptions.Left : TextAlignmentOptions.Right;
            _stateText.color = isOn ? MenuPalette.TextPrimaryColor : MenuPalette.TextSecondaryColor;
        }
    }
}
