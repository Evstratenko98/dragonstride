using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class CharacterSelectView : MonoBehaviour
{
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button joinByCodeButton;
    [SerializeField] private Button startMatchButton;
    [SerializeField] private Button backToMenuButton;
    [SerializeField] private InputField joinCodeInputField;
    [SerializeField] private RectTransform sessionListContent;
    [SerializeField] private CharacterSelectCardItemView sessionRowTemplate;
    [SerializeField] private Text lobbyListPlaceholderText;
    [SerializeField] private Text activeSessionText;
    [SerializeField] private Text statusText;

    private readonly List<CharacterSelectCardItemView> _spawnedRows = new();
    private float _rowHeight = 68f;
    private float _rowSpacing = 8f;
    private float _minContentHeight = 180f;

    public Button ConfirmButton => createLobbyButton;
    public Button UnconfirmButton => refreshButton;
    public Button RefreshButton => joinByCodeButton;
    public Button StartMatchButton => startMatchButton;
    public Button BackButton => backToMenuButton;
    public InputField NameInputField => joinCodeInputField;

    private void Awake()
    {
        createLobbyButton ??= FindButton("CreateLobbyButton");
        refreshButton ??= FindButton("RefreshButton");
        joinByCodeButton ??= FindButton("JoinByCodeButton");
        startMatchButton ??= FindButton("StartMatchButton");
        backToMenuButton ??= FindButton("BackToMenuButton");
        joinCodeInputField ??= FindInputField("JoinCodeInputField");
        sessionListContent ??= FindRectTransform("Content");
        sessionRowTemplate ??= FindRowTemplate("SessionRowTemplate");
        lobbyListPlaceholderText ??= FindText("LobbyListPlaceholderText");
        activeSessionText ??= FindText("ActiveSessionText");
        statusText ??= FindText("LobbyStatusText");
        EnsureRequiredReferences();

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

        SetButtonLabel(createLobbyButton, "Confirm");
        SetButtonLabel(refreshButton, "Unconfirm");
        SetButtonLabel(joinByCodeButton, "Refresh");
        SetButtonLabel(startMatchButton, "Start Match");
        SetButtonLabel(backToMenuButton, "Back");

        SetNamePlaceholder("Enter character name");
        ClearRows();
    }

    public string GetCharacterNameInput()
    {
        return joinCodeInputField == null ? string.Empty : joinCodeInputField.text;
    }

    public void SetCharacterNameInput(string value)
    {
        if (joinCodeInputField != null)
        {
            joinCodeInputField.text = value;
        }
    }

    public void ClearCharacterNameInput()
    {
        SetCharacterNameInput(string.Empty);
    }

    public void SetNamePlaceholder(string value)
    {
        if (joinCodeInputField == null || joinCodeInputField.placeholder == null)
        {
            return;
        }

        if (joinCodeInputField.placeholder is Text placeholderText)
        {
            placeholderText.text = value;
        }
    }

    public void SetListHeader(string text)
    {
        if (lobbyListPlaceholderText != null)
        {
            lobbyListPlaceholderText.text = text;
        }
    }

    public void SetSelectionSummary(string text)
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

    public CharacterSelectCardItemView CreateRow()
    {
        if (sessionRowTemplate == null || sessionListContent == null)
        {
            Debug.LogError("[CharacterSelectView] Row template or content container is missing.");
            return null;
        }

        CharacterSelectCardItemView row = Instantiate(sessionRowTemplate, sessionListContent);
        row.gameObject.SetActive(true);
        row.gameObject.name = $"CharacterRow_{_spawnedRows.Count + 1}";

        RectTransform rowRect = row.GetComponent<RectTransform>();
        if (rowRect != null)
        {
            rowRect.anchorMin = new Vector2(0f, 1f);
            rowRect.anchorMax = new Vector2(1f, 1f);
            rowRect.pivot = new Vector2(0.5f, 1f);
            rowRect.anchoredPosition = new Vector2(0f, -_spawnedRows.Count * (_rowHeight + _rowSpacing));
            rowRect.sizeDelta = new Vector2(0f, _rowHeight);
        }

        _spawnedRows.Add(row);
        ResizeContent();
        return row;
    }

    public void ClearRows()
    {
        for (int i = 0; i < _spawnedRows.Count; i++)
        {
            CharacterSelectCardItemView row = _spawnedRows[i];
            if (row != null)
            {
                Destroy(row.gameObject);
            }
        }

        _spawnedRows.Clear();
        ResizeContent();
    }

    public void SetConfirmInteractable(bool value) => SetButtonInteractable(createLobbyButton, value);
    public void SetUnconfirmInteractable(bool value) => SetButtonInteractable(refreshButton, value);
    public void SetRefreshInteractable(bool value) => SetButtonInteractable(joinByCodeButton, value);
    public void SetStartMatchInteractable(bool value) => SetButtonInteractable(startMatchButton, value);

    public void SetBackInteractable(bool value)
    {
        SetButtonInteractable(backToMenuButton, value);
        if (joinCodeInputField != null)
        {
            joinCodeInputField.interactable = value;
        }
    }

    private void ResizeContent()
    {
        if (sessionListContent == null)
        {
            return;
        }

        float rowsHeight = _spawnedRows.Count * (_rowHeight + _rowSpacing);
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

    private static void SetButtonLabel(Button button, string label)
    {
        if (button == null)
        {
            return;
        }

        Text text = button.GetComponentInChildren<Text>(true);
        if (text != null)
        {
            text.text = label;
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

        Debug.LogError($"[CharacterSelectView] Button '{objectName}' was not found in scene hierarchy.");
        return null;
    }

    private InputField FindInputField(string objectName)
    {
        InputField[] fields = GetComponentsInChildren<InputField>(true);
        for (int i = 0; i < fields.Length; i++)
        {
            InputField field = fields[i];
            if (field != null && field.gameObject.name == objectName)
            {
                return field;
            }
        }

        Debug.LogError($"[CharacterSelectView] InputField '{objectName}' was not found in scene hierarchy.");
        return null;
    }

    private RectTransform FindRectTransform(string objectName)
    {
        RectTransform[] rects = GetComponentsInChildren<RectTransform>(true);
        for (int i = 0; i < rects.Length; i++)
        {
            RectTransform rect = rects[i];
            if (rect != null && rect.gameObject.name == objectName)
            {
                return rect;
            }
        }

        Debug.LogError($"[CharacterSelectView] RectTransform '{objectName}' was not found in scene hierarchy.");
        return null;
    }

    private CharacterSelectCardItemView FindRowTemplate(string objectName)
    {
        CharacterSelectCardItemView[] rows = GetComponentsInChildren<CharacterSelectCardItemView>(true);
        for (int i = 0; i < rows.Length; i++)
        {
            CharacterSelectCardItemView row = rows[i];
            if (row != null && row.gameObject.name == objectName)
            {
                return row;
            }
        }

        Debug.LogError($"[CharacterSelectView] Character row template '{objectName}' was not found in scene hierarchy.");
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

        Debug.LogError($"[CharacterSelectView] Text '{objectName}' was not found in scene hierarchy.");
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
            "[CharacterSelectView] Required UI references are missing. Check CharacterSelect scene bindings and object names.");
    }
}
