using UnityEngine;
using UnityEngine.UI;

public sealed class LobbyView : MonoBehaviour
{
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button joinByCodeButton;
    [SerializeField] private Button startMatchButton;
    [SerializeField] private Button backToMenuButton;
    [SerializeField] private Text lobbyListPlaceholderText;
    [SerializeField] private Text statusText;

    public Button CreateLobbyButton => createLobbyButton;
    public Button RefreshButton => refreshButton;
    public Button JoinByCodeButton => joinByCodeButton;
    public Button StartMatchButton => startMatchButton;
    public Button BackToMenuButton => backToMenuButton;

    private void Awake()
    {
        createLobbyButton ??= FindButton("CreateLobbyButton");
        refreshButton ??= FindButton("RefreshButton");
        joinByCodeButton ??= FindButton("JoinByCodeButton");
        startMatchButton ??= FindButton("StartMatchButton");
        backToMenuButton ??= FindButton("BackToMenuButton");
        lobbyListPlaceholderText ??= FindText("LobbyListPlaceholderText");
        statusText ??= FindText("LobbyStatusText");
    }

    public void SetLobbyPlaceholderText(string text)
    {
        if (lobbyListPlaceholderText != null)
        {
            lobbyListPlaceholderText.text = text;
        }
    }

    public void SetStatus(string text)
    {
        if (statusText != null)
        {
            statusText.text = text;
        }
    }

    private Button FindButton(string objectName)
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (button != null && button.gameObject.name == objectName)
            {
                return button;
            }
        }

        Debug.LogError($"[LobbyView] Button '{objectName}' was not found in scene hierarchy.");
        return null;
    }

    private Text FindText(string objectName)
    {
        Text[] texts = GetComponentsInChildren<Text>(true);
        foreach (Text text in texts)
        {
            if (text != null && text.gameObject.name == objectName)
            {
                return text;
            }
        }

        Debug.LogError($"[LobbyView] Text '{objectName}' was not found in scene hierarchy.");
        return null;
    }
}
