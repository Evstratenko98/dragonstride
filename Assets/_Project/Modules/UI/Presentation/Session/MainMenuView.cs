using UnityEngine;
using UnityEngine.UI;

public sealed class MainMenuView : MonoBehaviour
{
    [SerializeField] private Button playOnlineButton;
    [SerializeField] private Button offlineTrainingButton;
    [SerializeField] private Button reconnectButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private Text statusText;

    public Button PlayOnlineButton => playOnlineButton;
    public Button OfflineTrainingButton => offlineTrainingButton;
    public Button ReconnectButton => reconnectButton;
    public Button SettingsButton => settingsButton;
    public Button ExitButton => exitButton;

    private void Awake()
    {
        playOnlineButton ??= FindButton("PlayOnlineButton");
        offlineTrainingButton ??= FindButton("OfflineTrainingButton");
        reconnectButton ??= FindButton("ReconnectButton");
        settingsButton ??= FindButton("SettingsButton");
        exitButton ??= FindButton("ExitButton");
        statusText ??= FindText("StatusText");
    }

    public void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    public void SetPlayOnlineInteractable(bool isInteractable)
    {
        if (playOnlineButton != null)
        {
            playOnlineButton.interactable = isInteractable;
        }
    }

    public void SetOfflineTrainingInteractable(bool isInteractable)
    {
        if (offlineTrainingButton != null)
        {
            offlineTrainingButton.interactable = isInteractable;
        }
    }

    public void SetReconnectInteractable(bool isInteractable)
    {
        if (reconnectButton != null)
        {
            reconnectButton.interactable = isInteractable;
        }
    }

    public void SetSettingsInteractable(bool isInteractable)
    {
        if (settingsButton != null)
        {
            settingsButton.interactable = isInteractable;
        }
    }

    public void SetExitInteractable(bool isInteractable)
    {
        if (exitButton != null)
        {
            exitButton.interactable = isInteractable;
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

        Debug.LogError($"[MainMenuView] Button '{objectName}' was not found in scene hierarchy.");
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

        Debug.LogError($"[MainMenuView] Text '{objectName}' was not found in scene hierarchy.");
        return null;
    }
}
