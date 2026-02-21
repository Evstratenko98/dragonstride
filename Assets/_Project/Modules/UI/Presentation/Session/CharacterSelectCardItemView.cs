using System;
using UnityEngine;
using UnityEngine.UI;

public enum CharacterSelectCardState
{
    Available,
    Taken,
    SelectedByYou,
    Locked
}

public sealed class CharacterSelectCardItemView : MonoBehaviour
{
    [SerializeField] private Button rowButton;
    [SerializeField] private Text nameText;
    [SerializeField] private Text metaText;

    private CharacterDefinition _definition;
    private Action<CharacterDefinition> _selectedCallback;

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
        CharacterDefinition definition,
        CharacterSelectCardState state,
        string occupiedByName,
        Action<CharacterDefinition> selectedCallback)
    {
        _definition = definition;
        _selectedCallback = selectedCallback;

        if (nameText != null)
        {
            nameText.text = definition == null || string.IsNullOrWhiteSpace(definition.DisplayName)
                ? "Unknown character"
                : definition.DisplayName;
        }

        if (metaText != null)
        {
            metaText.text = BuildMetaText(definition, state, occupiedByName);
        }

        if (rowButton != null)
        {
            rowButton.interactable = state == CharacterSelectCardState.Available || state == CharacterSelectCardState.SelectedByYou;
        }
    }

    private void OnClicked()
    {
        if (_definition == null)
        {
            return;
        }

        _selectedCallback?.Invoke(_definition);
    }

    private static string BuildMetaText(CharacterDefinition definition, CharacterSelectCardState state, string occupiedByName)
    {
        string id = definition == null || string.IsNullOrWhiteSpace(definition.Id) ? "-" : definition.Id;
        string stateText = state switch
        {
            CharacterSelectCardState.Available => "Available",
            CharacterSelectCardState.SelectedByYou => "Selected by you",
            CharacterSelectCardState.Locked => "Locked",
            CharacterSelectCardState.Taken => string.IsNullOrWhiteSpace(occupiedByName)
                ? "Taken"
                : $"Taken by {occupiedByName}",
            _ => "Unavailable"
        };

        return $"ID: {id}\nState: {stateText}";
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
