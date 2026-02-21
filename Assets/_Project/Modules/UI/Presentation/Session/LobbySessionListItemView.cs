using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class LobbySessionListItemView : MonoBehaviour
{
    [SerializeField] private Button rowButton;
    [SerializeField] private Text nameText;
    [SerializeField] private Text metaText;

    private MultiplayerSessionSummary _summary;
    private Action<MultiplayerSessionSummary> _selectedCallback;

    private void Awake()
    {
        rowButton ??= FindButton("SessionRowButton");
        nameText ??= FindText("NameText");
        metaText ??= FindText("MetaText");
    }

    private void OnEnable()
    {
        if (rowButton != null)
        {
            rowButton.onClick.AddListener(OnClicked);
        }
    }

    private void OnDisable()
    {
        if (rowButton != null)
        {
            rowButton.onClick.RemoveListener(OnClicked);
        }
    }

    public void Bind(
        MultiplayerSessionSummary summary,
        bool isJoinable,
        Action<MultiplayerSessionSummary> selectedCallback)
    {
        _summary = summary;
        _selectedCallback = selectedCallback;

        if (nameText != null)
        {
            nameText.text = string.IsNullOrWhiteSpace(summary.Name) ? "Unnamed lobby" : summary.Name;
        }

        if (metaText != null)
        {
            metaText.text = BuildMetaText(summary, isJoinable);
        }

        if (rowButton != null)
        {
            rowButton.interactable = isJoinable;
        }
    }

    private void OnClicked()
    {
        _selectedCallback?.Invoke(_summary);
    }

    private static string BuildMetaText(MultiplayerSessionSummary summary, bool isJoinable)
    {
        string state;
        if (summary.HasPassword)
        {
            state = "Password protected (join unavailable)";
        }
        else if (summary.IsLocked)
        {
            state = "Locked";
        }
        else if (summary.AvailableSlots <= 0)
        {
            state = "Full";
        }
        else if (isJoinable)
        {
            state = "Join available";
        }
        else
        {
            state = "Join unavailable";
        }

        return $"ID: {summary.SessionId}\nPlayers: {summary.PlayerCount}/{summary.MaxPlayers} | {state}";
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

        return GetComponent<Button>();
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

        return null;
    }
}
