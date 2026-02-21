using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class LobbyView : MonoBehaviour
{
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button joinByCodeButton;
    [SerializeField] private Button startMatchButton;
    [SerializeField] private Button backToMenuButton;
    [SerializeField] private InputField joinCodeInputField;
    [SerializeField] private RectTransform sessionListContent;
    [SerializeField] private LobbySessionListItemView sessionRowTemplate;
    [SerializeField] private Text lobbyListPlaceholderText;
    [SerializeField] private Text activeSessionText;
    [SerializeField] private Text statusText;

    private readonly List<LobbySessionListItemView> _spawnedSessionRows = new();
    private float _rowHeight = 68f;
    private float _rowSpacing = 8f;
    private float _minContentHeight = 180f;

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
        joinCodeInputField ??= FindInputField("JoinCodeInputField");
        sessionListContent ??= FindRectTransform("Content");
        sessionRowTemplate ??= FindSessionRowTemplate("SessionRowTemplate");
        lobbyListPlaceholderText ??= FindText("LobbyListPlaceholderText");
        activeSessionText ??= FindText("ActiveSessionText");
        statusText ??= FindText("LobbyStatusText");
        EnsureRequiredReferences();
        ApplyButtonLabels();

        if (sessionListContent != null)
        {
            _minContentHeight = Mathf.Max(120f, sessionListContent.sizeDelta.y);
        }

        if (sessionRowTemplate != null)
        {
            RectTransform templateRect = sessionRowTemplate.GetComponent<RectTransform>();
            if (templateRect != null && templateRect.sizeDelta.y > 1f)
            {
                _rowHeight = templateRect.sizeDelta.y;
            }

            sessionRowTemplate.gameObject.SetActive(false);
        }

        ClearSessionRows();
    }

    public string GetJoinCode()
    {
        return joinCodeInputField == null ? string.Empty : joinCodeInputField.text;
    }

    public void ClearJoinCode()
    {
        if (joinCodeInputField != null)
        {
            joinCodeInputField.text = string.Empty;
        }
    }

    public void SetLobbyPlaceholderText(string text)
    {
        if (lobbyListPlaceholderText != null)
        {
            lobbyListPlaceholderText.text = text;
        }
    }

    public void SetActiveSessionText(string text)
    {
        if (activeSessionText != null)
        {
            activeSessionText.text = text;
        }
    }

    public void SetStatus(string text)
    {
        if (statusText != null)
        {
            statusText.text = text;
        }
    }

    public LobbySessionListItemView CreateSessionRow()
    {
        if (sessionRowTemplate == null || sessionListContent == null)
        {
            Debug.LogError("[LobbyView] Session row template or content container is missing.");
            return null;
        }

        LobbySessionListItemView row = Instantiate(sessionRowTemplate, sessionListContent);
        row.gameObject.SetActive(true);
        row.gameObject.name = $"SessionRow_{_spawnedSessionRows.Count + 1}";

        RectTransform rowRect = row.GetComponent<RectTransform>();
        if (rowRect != null)
        {
            rowRect.anchorMin = new Vector2(0f, 1f);
            rowRect.anchorMax = new Vector2(1f, 1f);
            rowRect.pivot = new Vector2(0.5f, 1f);
            rowRect.anchoredPosition = new Vector2(0f, -_spawnedSessionRows.Count * (_rowHeight + _rowSpacing));
            rowRect.sizeDelta = new Vector2(0f, _rowHeight);
        }

        _spawnedSessionRows.Add(row);
        ResizeContentForRows();
        return row;
    }

    public void ClearSessionRows()
    {
        for (int i = 0; i < _spawnedSessionRows.Count; i++)
        {
            LobbySessionListItemView row = _spawnedSessionRows[i];
            if (row != null)
            {
                Destroy(row.gameObject);
            }
        }

        _spawnedSessionRows.Clear();
        ResizeContentForRows();
    }

    public void SetCreateLobbyInteractable(bool isInteractable)
    {
        SetButtonInteractable(createLobbyButton, isInteractable);
    }

    public void SetRefreshInteractable(bool isInteractable)
    {
        SetButtonInteractable(refreshButton, isInteractable);
    }

    public void SetJoinByCodeInteractable(bool isInteractable)
    {
        SetButtonInteractable(joinByCodeButton, isInteractable);
        if (joinCodeInputField != null)
        {
            joinCodeInputField.interactable = isInteractable;
        }
    }

    public void SetStartMatchInteractable(bool isInteractable)
    {
        SetButtonInteractable(startMatchButton, isInteractable);
    }

    public void SetBackToMenuInteractable(bool isInteractable)
    {
        SetButtonInteractable(backToMenuButton, isInteractable);
    }

    private void ResizeContentForRows()
    {
        if (sessionListContent == null)
        {
            return;
        }

        float rowsHeight = _spawnedSessionRows.Count * (_rowHeight + _rowSpacing);
        Vector2 size = sessionListContent.sizeDelta;
        size.y = Mathf.Max(_minContentHeight, rowsHeight);
        sessionListContent.sizeDelta = size;
    }

    private static void SetButtonInteractable(Button button, bool isInteractable)
    {
        if (button != null)
        {
            button.interactable = isInteractable;
        }
    }

    private Button FindButton(string objectName)
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button != null && button.gameObject.name == objectName)
            {
                return button;
            }
        }

        Debug.LogError($"[LobbyView] Button '{objectName}' was not found in scene hierarchy.");
        return null;
    }

    private InputField FindInputField(string objectName)
    {
        InputField[] inputFields = GetComponentsInChildren<InputField>(true);
        for (int i = 0; i < inputFields.Length; i++)
        {
            InputField inputField = inputFields[i];
            if (inputField != null && inputField.gameObject.name == objectName)
            {
                return inputField;
            }
        }

        Debug.LogError($"[LobbyView] InputField '{objectName}' was not found in scene hierarchy.");
        return null;
    }

    private RectTransform FindRectTransform(string objectName)
    {
        RectTransform[] rectTransforms = GetComponentsInChildren<RectTransform>(true);
        for (int i = 0; i < rectTransforms.Length; i++)
        {
            RectTransform rectTransform = rectTransforms[i];
            if (rectTransform != null && rectTransform.gameObject.name == objectName)
            {
                return rectTransform;
            }
        }

        Debug.LogError($"[LobbyView] RectTransform '{objectName}' was not found in scene hierarchy.");
        return null;
    }

    private LobbySessionListItemView FindSessionRowTemplate(string objectName)
    {
        LobbySessionListItemView[] items = GetComponentsInChildren<LobbySessionListItemView>(true);
        for (int i = 0; i < items.Length; i++)
        {
            LobbySessionListItemView item = items[i];
            if (item != null && item.gameObject.name == objectName)
            {
                return item;
            }
        }

        Debug.LogError($"[LobbyView] Session row template '{objectName}' was not found in scene hierarchy.");
        return null;
    }

    private Text FindText(string objectName)
    {
        Text[] texts = GetComponentsInChildren<Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            Text text = texts[i];
            if (text != null && text.gameObject.name == objectName)
            {
                return text;
            }
        }

        Debug.LogError($"[LobbyView] Text '{objectName}' was not found in scene hierarchy.");
        return null;
    }

    private void EnsureRequiredReferences()
    {
        if (createLobbyButton != null &&
            refreshButton != null &&
            joinByCodeButton != null &&
            startMatchButton != null &&
            backToMenuButton != null &&
            joinCodeInputField != null &&
            sessionListContent != null &&
            sessionRowTemplate != null &&
            lobbyListPlaceholderText != null &&
            activeSessionText != null &&
            statusText != null)
        {
            return;
        }

        throw new InvalidOperationException(
            "[LobbyView] Required UI references are missing. Check Lobby scene bindings and object names.");
    }

    private void ApplyButtonLabels()
    {
        SetButtonLabel(createLobbyButton, "Create Lobby");
        SetButtonLabel(refreshButton, "Refresh");
        SetButtonLabel(joinByCodeButton, "Join by Code");
        SetButtonLabel(startMatchButton, "Start Match");
        SetButtonLabel(backToMenuButton, "Back to Menu");
    }

    private static void SetButtonLabel(Button button, string label)
    {
        if (button == null)
        {
            return;
        }

        Text labelText = button.GetComponentInChildren<Text>(true);
        if (labelText != null)
        {
            labelText.text = label;
        }
    }
}
