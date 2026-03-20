using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class BaseSceneGenerator
{
    private const string GameScenePath = "Assets/Scenes/GameScene.unity";
    private const string MainMenuScenePath = "Assets/Scenes/MainMenuScene.unity";
    private const string LobbyScenePath = "Assets/Scenes/LobbyScene.unity";
    private const string FinishScenePath = "Assets/Scenes/FinishScene.unity";

    private static readonly Color BackgroundColor = MenuPalette.BackgroundColor;
    private static readonly Color PanelColor = MenuPalette.PanelColor;
    private static readonly Color SecondaryPanelColor = MenuPalette.SecondaryPanelColor;
    private static readonly Color AccentColor = MenuPalette.AccentColor;
    private static readonly Color AccentColorDark = MenuPalette.AccentPressedColor;
    private static readonly Color TextPrimaryColor = MenuPalette.TextPrimaryColor;
    private static readonly Color TextSecondaryColor = MenuPalette.TextSecondaryColor;
    private static readonly Color ButtonSecondaryColor = MenuPalette.ButtonSecondaryColor;
    private static readonly Color DangerButtonColor = MenuPalette.DangerButtonColor;
    private static readonly Color DangerButtonPressedColor = MenuPalette.DangerButtonPressedColor;

    private static TMP_DefaultControls.Resources _resources;

    [MenuItem("Tools/DragonStride/Generate Base Scenes")]
    public static void GenerateScenes()
    {
        CreateOrUpdateGameScene();
        CreateMainMenuScene();
        CreateLobbyScene();
        CreateFinishScene();
        UpdateBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[BaseSceneGenerator] Base scenes were generated successfully.");
    }

    private static void CreateOrUpdateGameScene()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(GameScenePath) != null)
        {
            return;
        }

        throw new UnityException($"[BaseSceneGenerator] Expected '{GameScenePath}' to exist, but it was not found.");
    }

    private static void CreateMainMenuScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Canvas canvas = CreateSceneShell("MainMenuRoot");
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();

        AddSceneBackground(canvasRect);

        Image rootPanel = CreatePanel(
            "RootPanel",
            canvasRect,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(1520f, 820f),
            PanelColor);

        Image brandPanel = CreatePanel(
            "BrandPanel",
            rootPanel.rectTransform,
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(78f, 0f),
            new Vector2(760f, 680f),
            SecondaryPanelColor);

        CreateText(
            "Eyebrow",
            brandPanel.rectTransform,
            "TACTICAL ROGUELITE",
            24,
            FontStyles.Bold,
            TextAlignmentOptions.TopLeft,
            AccentColor,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
            new Vector2(42f, -42f),
            new Vector2(-42f, 44f));

        CreateText(
            "Title",
            brandPanel.rectTransform,
            "DragonStride",
            68,
            FontStyles.Bold,
            TextAlignmentOptions.TopLeft,
            TextPrimaryColor,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
            new Vector2(42f, -96f),
            new Vector2(-42f, 90f));

        CreateText(
            "Subtitle",
            brandPanel.rectTransform,
            "Соберите отряд, настройте состав в лобби и ведите героев к короне.",
            30,
            FontStyles.Normal,
            TextAlignmentOptions.TopLeft,
            TextSecondaryColor,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
            new Vector2(42f, -202f),
            new Vector2(-58f, 120f));

        CreateText(
            "Highlights",
            brandPanel.rectTransform,
            "• До 4 героев в одной партии\n• Выбор имени и класса в лобби\n• Единый визуальный стиль всех экранов",
            26,
            FontStyles.Normal,
            TextAlignmentOptions.TopLeft,
            TextPrimaryColor,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
            new Vector2(42f, -340f),
            new Vector2(-58f, 170f));

        Image menuPanel = CreatePanel(
            "MenuPanel",
            rootPanel.rectTransform,
            new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f),
            new Vector2(-92f, 0f),
            new Vector2(470f, 590f),
            SecondaryPanelColor);

        CreateText(
            "MenuLabel",
            menuPanel.rectTransform,
            "ГЛАВНОЕ МЕНЮ",
            30,
            FontStyles.Bold,
            TextAlignmentOptions.TopLeft,
            TextPrimaryColor,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
            new Vector2(36f, -36f),
            new Vector2(-36f, 50f));

        CreateText(
            "MenuHint",
            menuPanel.rectTransform,
            "Основной путь входа в игру проходит через лобби.",
            24,
            FontStyles.Normal,
            TextAlignmentOptions.TopLeft,
            TextSecondaryColor,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
            new Vector2(36f, -84f),
            new Vector2(-36f, 60f));

        RectTransform buttonStack = CreateLayoutContainer(
            "ButtonStack",
            menuPanel.rectTransform,
            new Vector2(0f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, -28f),
            new Vector2(-64f, -196f),
            spacing: 20,
            padding: new RectOffset(0, 0, 0, 0),
            childControlHeight: true);

        Button openLobbyButton = CreateButton("OpenLobbyButton", buttonStack, "Открыть лобби", AccentColor, AccentColorDark, MenuPalette.ButtonLabelColor);
        SetLayoutElement(openLobbyButton.gameObject, preferredHeight: 74f, flexibleWidth: 1f);

        Button settingsButton = CreateButton("SettingsButton", buttonStack, "Настройки", ButtonSecondaryColor, ButtonSecondaryColor * 0.8f, MenuPalette.ButtonLabelColor);
        settingsButton.interactable = false;
        SetLayoutElement(settingsButton.gameObject, preferredHeight: 74f, flexibleWidth: 1f);

        Button collectionButton = CreateButton("CollectionButton", buttonStack, "Коллекция", ButtonSecondaryColor, ButtonSecondaryColor * 0.8f, MenuPalette.ButtonLabelColor);
        collectionButton.interactable = false;
        SetLayoutElement(collectionButton.gameObject, preferredHeight: 74f, flexibleWidth: 1f);

        Button quitButton = CreateButton("QuitButton", buttonStack, "Выход", DangerButtonColor, DangerButtonPressedColor, MenuPalette.ButtonLabelColor);
        SetLayoutElement(quitButton.gameObject, preferredHeight: 74f, flexibleWidth: 1f);

        GameObject controllerObject = new("SceneFlow");
        MainMenuController controller = controllerObject.AddComponent<MainMenuController>();
        SetPrivateField(controller, "openLobbyButton", openLobbyButton);
        SetPrivateField(controller, "quitButton", quitButton);

        EditorSceneManager.SaveScene(scene, MainMenuScenePath);
    }

    private static void CreateLobbyScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Canvas canvas = CreateSceneShell("LobbyRoot");
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();

        AddSceneBackground(canvasRect);

        Image rootPanel = CreatePanel(
            "RootPanel",
            canvasRect,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(1640f, 900f),
            PanelColor);

        CreateText(
            "Title",
            rootPanel.rectTransform,
            "Игровое лобби",
            56,
            FontStyles.Bold,
            TextAlignmentOptions.TopLeft,
            TextPrimaryColor,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
            new Vector2(56f, -42f),
            new Vector2(-56f, 70f));

        CreateText(
            "Subtitle",
            rootPanel.rectTransform,
            "Добавьте до четырёх персонажей, задайте им имена и подберите классы.",
            28,
            FontStyles.Normal,
            TextAlignmentOptions.TopLeft,
            TextSecondaryColor,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
            new Vector2(56f, -102f),
            new Vector2(-56f, 56f));

        RectTransform topBar = CreateHorizontalContainer(
            "TopBar",
            rootPanel.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -162f),
            new Vector2(-112f, 72f),
            spacing: 18,
            padding: new RectOffset(0, 0, 0, 0));

        TMP_Text topBarText = CreateFlowText(
            "TopBarText",
            topBar,
            "Слоты появляются по кнопке. Если имя пустое, игра возьмёт название класса.",
            24,
            FontStyles.Normal,
            TextAlignmentOptions.Left,
            TextSecondaryColor);
        SetLayoutElement(topBarText.gameObject, flexibleWidth: 1f, preferredHeight: 72f);

        Button addPlayerButton = CreateButton("AddPlayerButton", topBar, "Добавить слот", AccentColor, AccentColorDark, MenuPalette.ButtonLabelColor);
        SetLayoutElement(addPlayerButton.gameObject, preferredWidth: 300f, preferredHeight: 72f);

        RectTransform listRoot = CreateLayoutContainer(
            "SlotList",
            rootPanel.rectTransform,
            new Vector2(0f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, -18f),
            new Vector2(-112f, -330f),
            spacing: 14,
            padding: new RectOffset(0, 0, 0, 0));

        var slotViews = new LobbySceneController.SlotView[GameSessionState.MaxSlots];
        for (int i = 0; i < slotViews.Length; i++)
        {
            slotViews[i] = CreateLobbySlotCard(listRoot, i + 1);
        }

        RectTransform footer = CreateHorizontalContainer(
            "Footer",
            rootPanel.rectTransform,
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 42f),
            new Vector2(-96f, 82f),
            spacing: 18,
            padding: new RectOffset(0, 0, 0, 0));

        TMP_Text helperText = CreateFlowText(
            "HelperText",
            footer,
            "Добавьте хотя бы один слот, чтобы начать игру.",
            24,
            FontStyles.Normal,
            TextAlignmentOptions.Left,
            TextSecondaryColor);
        SetLayoutElement(helperText.gameObject, flexibleWidth: 1f, preferredHeight: 66f);

        RectTransform actions = CreateHorizontalContainer(
            "Actions",
            footer,
            new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f),
            Vector2.zero,
            new Vector2(460f, 72f),
            spacing: 16,
            padding: new RectOffset(0, 0, 0, 0));
        SetLayoutElement(actions.gameObject, preferredWidth: 460f, preferredHeight: 72f);

        Button backButton = CreateButton("BackButton", actions, "Назад", ButtonSecondaryColor, MenuPalette.ButtonSecondaryPressedColor, MenuPalette.ButtonLabelColor);
        SetLayoutElement(backButton.gameObject, preferredWidth: 180f, preferredHeight: 72f);

        Button startButton = CreateButton("StartButton", actions, "Начать игру", AccentColor, AccentColorDark, MenuPalette.ButtonLabelColor);
        startButton.interactable = false;
        SetLayoutElement(startButton.gameObject, preferredWidth: 260f, preferredHeight: 72f);

        GameObject controllerObject = new("SceneFlow");
        LobbySceneController controller = controllerObject.AddComponent<LobbySceneController>();
        SetPrivateField(controller, "slotViews", slotViews);
        SetPrivateField(controller, "addPlayerButton", addPlayerButton);
        SetPrivateField(controller, "startGameButton", startButton);
        SetPrivateField(controller, "backButton", backButton);
        SetPrivateField(controller, "helperText", helperText);

        EditorSceneManager.SaveScene(scene, LobbyScenePath);
    }

    private static void CreateFinishScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Canvas canvas = CreateSceneShell("FinishRoot");
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();

        AddSceneBackground(canvasRect);

        Image panel = CreatePanel(
            "FinishPanel",
            canvasRect,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(980f, 620f),
            PanelColor);

        CreateText(
            "Title",
            panel.rectTransform,
            "Игра завершена",
            60,
            FontStyles.Bold,
            TextAlignmentOptions.Top,
            MenuPalette.ButtonLabelColor,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -70f),
            new Vector2(-96f, 80f));

        CreateText(
            "Body",
            panel.rectTransform,
            "Партия окончена. Корона возвращается в зал трофеев.",
            28,
            FontStyles.Normal,
            TextAlignmentOptions.Top,
            TextSecondaryColor,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -150f),
            new Vector2(-120f, 72f));

        TMP_Text winnerText = CreateText(
            "WinnerText",
            panel.rectTransform,
            "Победитель: Неизвестный герой",
            42,
            FontStyles.Bold,
            TextAlignmentOptions.Center,
            AccentColor,
            new Vector2(0f, 0.5f),
            new Vector2(1f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 12f),
            new Vector2(-120f, 90f));

        Button returnButton = CreateButton(
            "ReturnButton",
            panel.rectTransform,
            "В главное меню",
            AccentColor,
            AccentColorDark,
            TextPrimaryColor,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 68f),
            new Vector2(320f, 72f));

        GameObject controllerObject = new("SceneFlow");
        FinishSceneController controller = controllerObject.AddComponent<FinishSceneController>();
        SetPrivateField(controller, "winnerText", winnerText);
        SetPrivateField(controller, "returnToMenuButton", returnButton);

        EditorSceneManager.SaveScene(scene, FinishScenePath);
    }

    private static Canvas CreateSceneShell(string rootName)
    {
        GameObject root = new(rootName);
        root.transform.position = Vector3.zero;

        CreateCamera();
        CreateEventSystem();
        return CreateCanvas(root.transform);
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new("Main Camera", typeof(Camera), typeof(AudioListener));
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = BackgroundColor;
        camera.nearClipPlane = 0.3f;
        camera.farClipPlane = 1000f;
        camera.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
    }

    private static void CreateEventSystem()
    {
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    private static Canvas CreateCanvas(Transform parent)
    {
        GameObject canvasObject = new("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.layer = LayerMask.NameToLayer("UI");
        canvasObject.transform.SetParent(parent, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform rectTransform = canvasObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        return canvas;
    }

    private static void AddSceneBackground(RectTransform parent)
    {
        CreatePanel(
            "Backdrop",
            parent,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero,
            BackgroundColor,
            stretchToParent: true);

        CreateDecorShape(parent, "GlowTopRight", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-160f, -120f), new Vector2(640f, 640f), MenuPalette.GlowTopRightColor, -18f);
        CreateDecorShape(parent, "GlowBottomLeft", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(160f, 120f), new Vector2(700f, 700f), MenuPalette.GlowBottomLeftColor, 14f);
        CreateDecorShape(parent, "Stripe", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -300f), new Vector2(1880f, 120f), MenuPalette.StripeColor, 0f);
    }

    private static LobbySceneController.SlotView CreateLobbySlotCard(Transform parent, int slotNumber)
    {
        Image card = CreatePanel(
            $"SlotCard_{slotNumber}",
            parent,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(0f, 120f),
            SecondaryPanelColor);
        SetLayoutElement(card.gameObject, preferredHeight: 120f, flexibleWidth: 1f);

        RectTransform content = CreateHorizontalContainer(
            "Content",
            card.rectTransform,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(-36f, -24f),
            spacing: 14,
            padding: new RectOffset(18, 18, 16, 16));

        RectTransform infoColumn = CreateLayoutContainer(
            "InfoColumn",
            content,
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            Vector2.zero,
            new Vector2(240f, 0f),
            spacing: 4,
            padding: new RectOffset(0, 0, 0, 0));
        SetLayoutElement(infoColumn.gameObject, preferredWidth: 240f, preferredHeight: 88f);

        TMP_Text title = CreateFlowText($"SlotTitle_{slotNumber}", infoColumn, $"Игрок {slotNumber}", 28, FontStyles.Bold, TextAlignmentOptions.Left, TextPrimaryColor);
        SetLayoutElement(title.gameObject, preferredHeight: 32f);

        TMP_Text state = CreateFlowText($"SlotState_{slotNumber}", infoColumn, "Если имя пустое, возьмём название класса.", 18, FontStyles.Normal, TextAlignmentOptions.Left, TextSecondaryColor);
        SetLayoutElement(state.gameObject, preferredHeight: 42f);

        RectTransform nameColumn = CreateLayoutContainer(
            "NameColumn",
            content,
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            Vector2.zero,
            new Vector2(520f, 0f),
            spacing: 8,
            padding: new RectOffset(0, 0, 0, 0));
        SetLayoutElement(nameColumn.gameObject, preferredWidth: 520f, preferredHeight: 88f, flexibleWidth: 1f);

        TMP_Text nameLabel = CreateFlowText($"NameLabel_{slotNumber}", nameColumn, "Имя героя", 18, FontStyles.Bold, TextAlignmentOptions.Left, TextSecondaryColor);
        SetLayoutElement(nameLabel.gameObject, preferredHeight: 22f);

        TMP_InputField nameInput = CreateInputField(nameColumn, "NameInput", "Например, Каэл");
        SetInputPlaceholder(nameInput, "Например, Каэл");
        SetLayoutElement(nameInput.gameObject, preferredHeight: 54f, flexibleWidth: 1f);

        RectTransform classColumn = CreateLayoutContainer(
            "ClassColumn",
            content,
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            Vector2.zero,
            new Vector2(360f, 0f),
            spacing: 8,
            padding: new RectOffset(0, 0, 0, 0));
        SetLayoutElement(classColumn.gameObject, preferredWidth: 360f, preferredHeight: 88f);

        TMP_Text classLabel = CreateFlowText($"ClassLabel_{slotNumber}", classColumn, "Класс", 18, FontStyles.Bold, TextAlignmentOptions.Left, TextSecondaryColor);
        SetLayoutElement(classLabel.gameObject, preferredHeight: 22f);

        TMP_Dropdown classDropdown = CreateDropdown(classColumn, "ClassDropdown");
        SetLayoutElement(classDropdown.gameObject, preferredHeight: 54f, preferredWidth: 360f);

        Button removeButton = CreateButton($"RemoveButton_{slotNumber}", content, "Убрать", DangerButtonColor, DangerButtonPressedColor, MenuPalette.ButtonLabelColor);
        SetLayoutElement(removeButton.gameObject, preferredWidth: 164f, preferredHeight: 54f);

        return new LobbySceneController.SlotView
        {
            Root = card.gameObject,
            CardBackground = card,
            TitleText = title,
            StateText = state,
            NameInput = nameInput,
            ClassDropdown = classDropdown,
            RemoveButton = removeButton
        };
    }

    private static Image CreatePanel(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        Color color,
        bool stretchToParent = false)
    {
        GameObject panelObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObject.transform.SetParent(parent, false);

        Image image = panelObject.GetComponent<Image>();
        image.sprite = GetStandardResources().standard;
        image.type = Image.Type.Sliced;
        image.color = color;
        image.raycastTarget = false;

        RectTransform rect = image.rectTransform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        if (stretchToParent)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        return image;
    }

    private static void CreateDecorShape(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        Color color,
        float rotationZ)
    {
        Image image = CreatePanel(name, parent, anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta, color);
        image.rectTransform.localEulerAngles = new Vector3(0f, 0f, rotationZ);
        image.raycastTarget = false;
    }

    private static TMP_Text CreateText(
        string name,
        Transform parent,
        string content,
        float fontSize,
        FontStyles fontStyles,
        TextAlignmentOptions alignment,
        Color color,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 sizeDelta)
    {
        GameObject textObject = TMP_DefaultControls.CreateText(GetStandardResources());
        textObject.name = name;
        textObject.transform.SetParent(parent, false);

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = fontStyles;
        text.alignment = alignment;
        text.color = color;
        text.enableWordWrapping = true;

        RectTransform rect = text.rectTransform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        return text;
    }

    private static TMP_Text CreateFlowText(
        string name,
        Transform parent,
        string content,
        float fontSize,
        FontStyles fontStyles,
        TextAlignmentOptions alignment,
        Color color)
    {
        GameObject textObject = TMP_DefaultControls.CreateText(GetStandardResources());
        textObject.name = name;
        textObject.transform.SetParent(parent, false);

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = fontStyles;
        text.alignment = alignment;
        text.color = color;
        text.enableWordWrapping = true;
        text.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        text.rectTransform.anchorMax = new Vector2(1f, 0.5f);
        text.rectTransform.sizeDelta = new Vector2(0f, text.rectTransform.sizeDelta.y);

        return text;
    }

    private static Button CreateButton(
        string name,
        Transform parent,
        string label,
        Color backgroundColor,
        Color pressedColor,
        Color labelColor,
        Vector2? anchorMin = null,
        Vector2? anchorMax = null,
        Vector2? pivot = null,
        Vector2? anchoredPosition = null,
        Vector2? sizeDelta = null)
    {
        GameObject buttonObject = TMP_DefaultControls.CreateButton(GetStandardResources());
        buttonObject.name = name;
        buttonObject.transform.SetParent(parent, false);

        Button button = buttonObject.GetComponent<Button>();
        Image image = buttonObject.GetComponent<Image>();
        image.color = backgroundColor;

        ColorBlock colors = button.colors;
        colors.normalColor = backgroundColor;
        colors.highlightedColor = Color.Lerp(backgroundColor, Color.white, 0.08f);
        colors.pressedColor = pressedColor;
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = MenuPalette.DisabledButtonColor;
        colors.fadeDuration = 0.1f;
        button.colors = colors;

        TMP_Text text = buttonObject.GetComponentInChildren<TMP_Text>(true);
        text.text = label;
        text.fontSize = 30;
        text.fontStyle = FontStyles.Bold;
        text.color = labelColor;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.margin = Vector4.zero;
        RectTransform textRect = text.rectTransform;
        textRect.offsetMin = new Vector2(20f, 8f);
        textRect.offsetMax = new Vector2(-20f, -8f);

        RectTransform rect = button.GetComponent<RectTransform>();
        if (anchorMin.HasValue)
        {
            rect.anchorMin = anchorMin.Value;
        }
        if (anchorMax.HasValue)
        {
            rect.anchorMax = anchorMax.Value;
        }
        if (pivot.HasValue)
        {
            rect.pivot = pivot.Value;
        }
        if (anchoredPosition.HasValue)
        {
            rect.anchoredPosition = anchoredPosition.Value;
        }
        if (sizeDelta.HasValue)
        {
            rect.sizeDelta = sizeDelta.Value;
        }

        return button;
    }

    private static TMP_InputField CreateInputField(Transform parent, string name, string placeholderText)
    {
        GameObject inputObject = TMP_DefaultControls.CreateInputField(GetStandardResources());
        inputObject.name = name;
        inputObject.transform.SetParent(parent, false);

        TMP_InputField inputField = inputObject.GetComponent<TMP_InputField>();
        Image background = inputObject.GetComponent<Image>();
        background.color = MenuPalette.InputBackgroundColor;

        TMP_Text text = inputObject.transform.Find("Text Area/Text").GetComponent<TMP_Text>();
        text.fontSize = 24;
        text.color = TextPrimaryColor;
        text.margin = Vector4.zero;

        TMP_Text placeholder = inputObject.transform.Find("Text Area/Placeholder").GetComponent<TMP_Text>();
        placeholder.text = placeholderText;
        placeholder.fontSize = 24;
        placeholder.color = new Color(TextSecondaryColor.r, TextSecondaryColor.g, TextSecondaryColor.b, 0.55f);
        placeholder.margin = Vector4.zero;

        RectTransform textArea = inputObject.transform.Find("Text Area").GetComponent<RectTransform>();
        textArea.offsetMin = new Vector2(20f, 10f);
        textArea.offsetMax = new Vector2(-20f, -10f);

        inputField.contentType = TMP_InputField.ContentType.Name;
        inputField.characterLimit = 18;

        return inputField;
    }

    private static void SetInputPlaceholder(TMP_InputField inputField, string placeholderText)
    {
        if (inputField == null)
        {
            return;
        }

        TMP_Text placeholder = inputField.transform.Find("Text Area/Placeholder").GetComponent<TMP_Text>();
        placeholder.text = placeholderText;
    }

    private static TMP_Dropdown CreateDropdown(Transform parent, string name)
    {
        GameObject dropdownObject = TMP_DefaultControls.CreateDropdown(GetStandardResources());
        dropdownObject.name = name;
        dropdownObject.transform.SetParent(parent, false);

        TMP_Dropdown dropdown = dropdownObject.GetComponent<TMP_Dropdown>();
        Image background = dropdownObject.GetComponent<Image>();
        background.color = MenuPalette.InputBackgroundColor;

        TMP_Text label = dropdownObject.transform.Find("Label").GetComponent<TMP_Text>();
        label.fontSize = 24;
        label.color = TextPrimaryColor;
        label.margin = Vector4.zero;
        RectTransform labelRect = label.rectTransform;
        labelRect.offsetMin = new Vector2(20f, 10f);
        labelRect.offsetMax = new Vector2(-56f, -10f);

        Image arrow = dropdownObject.transform.Find("Arrow").GetComponent<Image>();
        arrow.color = AccentColor;

        Transform template = dropdownObject.transform.Find("Template");
        template.GetComponent<Image>().color = MenuPalette.DropdownTemplateColor;
        Transform item = template.Find("Viewport/Content/Item");
        item.Find("Item Label").GetComponent<TMP_Text>().color = TextPrimaryColor;
        item.Find("Item Label").GetComponent<TMP_Text>().fontSize = 22;
        item.Find("Item Background").GetComponent<Image>().color = MenuPalette.DropdownItemBackgroundColor;

        dropdown.template.sizeDelta = new Vector2(dropdown.template.sizeDelta.x, 220f);
        return dropdown;
    }

    private static RectTransform CreateLayoutContainer(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        float spacing,
        RectOffset padding,
        bool childControlHeight = false)
    {
        GameObject containerObject = new(name, typeof(RectTransform), typeof(VerticalLayoutGroup));
        containerObject.transform.SetParent(parent, false);

        RectTransform rect = containerObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        VerticalLayoutGroup layout = containerObject.GetComponent<VerticalLayoutGroup>();
        layout.spacing = spacing;
        layout.padding = padding;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = childControlHeight;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        return rect;
    }

    private static RectTransform CreateHorizontalContainer(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        float spacing,
        RectOffset padding)
    {
        GameObject containerObject = new(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
        containerObject.transform.SetParent(parent, false);

        RectTransform rect = containerObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        HorizontalLayoutGroup layout = containerObject.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = spacing;
        layout.padding = padding;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        return rect;
    }

    private static RectTransform CreateGridContainer(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        Vector2 cellSize,
        Vector2 spacing,
        int columns)
    {
        GameObject containerObject = new(name, typeof(RectTransform), typeof(GridLayoutGroup));
        containerObject.transform.SetParent(parent, false);

        RectTransform rect = containerObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        GridLayoutGroup layout = containerObject.GetComponent<GridLayoutGroup>();
        layout.cellSize = cellSize;
        layout.spacing = spacing;
        layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        layout.startAxis = GridLayoutGroup.Axis.Horizontal;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = columns;

        return rect;
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

    private static TMP_DefaultControls.Resources GetStandardResources()
    {
        if (_resources.standard != null)
        {
            return _resources;
        }

        _resources.standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        _resources.background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        _resources.inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd");
        _resources.knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        _resources.checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");
        _resources.dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd");
        _resources.mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd");
        return _resources;
    }

    private static void UpdateBuildSettings()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MainMenuScenePath, true),
            new EditorBuildSettingsScene(LobbyScenePath, true),
            new EditorBuildSettingsScene(GameScenePath, true),
            new EditorBuildSettingsScene(FinishScenePath, true)
        };
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo fieldInfo = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (fieldInfo == null)
        {
            throw new UnityException($"[BaseSceneGenerator] Field '{fieldName}' was not found on '{target.GetType().Name}'.");
        }

        fieldInfo.SetValue(target, value);
        if (target is Object unityObject)
        {
            EditorUtility.SetDirty(unityObject);
        }
    }
}
