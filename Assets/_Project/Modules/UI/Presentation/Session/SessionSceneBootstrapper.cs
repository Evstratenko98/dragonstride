using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public static class SessionSceneBootstrapper
{
    private static bool _isInitialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        var sceneName = scene.name;
        switch (sceneName)
        {
            case SessionSceneNames.MainMenu:
                BuildSingleButtonScreen("Создать лобби", SessionSceneNames.Lobby);
                break;
            case SessionSceneNames.Lobby:
                BuildSingleButtonScreen("Начать игру", SessionSceneNames.GameScene);
                break;
            case SessionSceneNames.GameOver:
                BuildSingleButtonScreen("Выйти из сессии", SessionSceneNames.MainMenu);
                break;
        }
    }

    private static void BuildSingleButtonScreen(string buttonText, string targetScene)
    {
        var activeScene = SceneManager.GetActiveScene();
        foreach (var root in activeScene.GetRootGameObjects())
        {
            Object.Destroy(root);
        }

        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.AddComponent<Camera>();
        cameraObject.AddComponent<AudioListener>();

        var canvasObject = new GameObject("Canvas");
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        var eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
#else
        eventSystemObject.AddComponent<StandaloneInputModule>();
#endif

        var buttonObject = new GameObject("CenterButton");
        buttonObject.transform.SetParent(canvasObject.transform, false);

        var buttonRect = buttonObject.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = Vector2.zero;
        buttonRect.sizeDelta = new Vector2(420f, 96f);

        var buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = new Color32(26, 102, 255, 255);

        var button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(() => SceneManager.LoadScene(targetScene));

        var labelObject = new GameObject("Label");
        labelObject.transform.SetParent(buttonObject.transform, false);
        var labelRect = labelObject.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        var label = labelObject.AddComponent<TextMeshProUGUI>();
        label.text = buttonText;
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 36;
        label.color = Color.white;
        label.font = TMP_Settings.defaultFontAsset;
    }
}
