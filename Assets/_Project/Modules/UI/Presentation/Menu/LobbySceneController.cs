using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class LobbySceneController : MonoBehaviour
{
    [Serializable]
    public sealed class SlotView
    {
        public GameObject Root;
        public Image CardBackground;
        public TMP_Text TitleText;
        public TMP_Text StateText;
        public TMP_InputField NameInput;
        public TMP_Dropdown ClassDropdown;
        public Button RemoveButton;
    }

    [SerializeField] private SlotView[] slotViews;
    [SerializeField] private Button addPlayerButton;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button backButton;
    [SerializeField] private TMP_Text helperText;
    [SerializeField] private Color emptySlotColor = default;
    [SerializeField] private Color activeSlotColor = default;

    private readonly List<SlotView> _runtimeSlotViews = new();
    private readonly List<string> _classLabels = new();

    private SlotView _templateSlotView;
    private Transform _slotContainer;
    private string _titlePath;
    private string _stateTextPath;
    private string _nameInputPath;
    private string _classDropdownPath;
    private string _removeButtonPath;

    private void Reset()
    {
        emptySlotColor = MenuPalette.SlotEmptyColor;
        activeSlotColor = MenuPalette.SlotActiveColor;
    }

    private void Awake()
    {
        if (emptySlotColor == default)
        {
            emptySlotColor = MenuPalette.SlotEmptyColor;
        }

        if (activeSlotColor == default)
        {
            activeSlotColor = MenuPalette.SlotActiveColor;
        }

        GameSessionState.ResetLobby();
        CacheClassLabels();
        InitializeTemplateSlot();

        if (addPlayerButton != null)
        {
            addPlayerButton.onClick.AddListener(AddPlayerSlot);
        }

        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(StartGame);
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(BackToMainMenu);
        }

        RefreshAllSlots(syncFields: true);
    }

    private void CacheClassLabels()
    {
        _classLabels.Clear();
        for (int i = 0; i < GameSessionState.AvailableClasses.Count; i++)
        {
            _classLabels.Add(GameSessionState.AvailableClasses[i].Label);
        }
    }

    private void InitializeTemplateSlot()
    {
        if (slotViews == null || slotViews.Length == 0 || slotViews[0] == null || slotViews[0].Root == null)
        {
            return;
        }

        _templateSlotView = slotViews[0];
        _slotContainer = _templateSlotView.Root.transform.parent;
        _titlePath = GetRelativePath(_templateSlotView.Root.transform, _templateSlotView.TitleText);
        _stateTextPath = GetRelativePath(_templateSlotView.Root.transform, _templateSlotView.StateText);
        _nameInputPath = GetRelativePath(_templateSlotView.Root.transform, _templateSlotView.NameInput);
        _classDropdownPath = GetRelativePath(_templateSlotView.Root.transform, _templateSlotView.ClassDropdown);
        _removeButtonPath = GetRelativePath(_templateSlotView.Root.transform, _templateSlotView.RemoveButton);

        _templateSlotView.Root.SetActive(false);

        for (int i = 1; i < slotViews.Length; i++)
        {
            if (slotViews[i]?.Root != null)
            {
                slotViews[i].Root.SetActive(false);
            }
        }
    }

    private void RefreshAllSlots(bool syncFields)
    {
        for (int i = 0; i < _runtimeSlotViews.Count; i++)
        {
            RefreshSlot(_runtimeSlotViews[i], i, syncFields);
        }

        RefreshStartButton();
    }

    private void RefreshSlot(SlotView slotView, int index, bool syncFields)
    {
        if (slotView == null)
        {
            return;
        }

        if (slotView.Root != null)
        {
            slotView.Root.SetActive(true);
            slotView.Root.name = $"SlotCard_{index + 1}";
        }

        if (slotView.TitleText != null)
        {
            slotView.TitleText.text = $"Игрок {index + 1}";
        }

        LobbyCharacterSlot slot = GameSessionState.GetSlot(index);
        bool isEnabled = slot.IsEnabled;

        if (slotView.CardBackground != null)
        {
            slotView.CardBackground.color = isEnabled ? activeSlotColor : emptySlotColor;
        }

        if (slotView.StateText != null)
        {
            slotView.StateText.text = isEnabled
                ? "Готов к старту."
                : "Если имя пустое, возьмём название класса.";
        }

        if (slotView.NameInput != null)
        {
            slotView.NameInput.interactable = true;
            if (syncFields)
            {
                slotView.NameInput.SetTextWithoutNotify(slot.Name ?? string.Empty);
            }
        }

        if (slotView.ClassDropdown != null)
        {
            slotView.ClassDropdown.interactable = true;
            if (syncFields)
            {
                slotView.ClassDropdown.SetValueWithoutNotify(GameSessionState.GetClassIndex(slot.ClassId));
            }
        }

        if (slotView.RemoveButton != null)
        {
            slotView.RemoveButton.gameObject.SetActive(true);
        }
    }

    private void RefreshStartButton()
    {
        bool canStart = GameSessionState.HasReadyCharacters();
        if (startGameButton != null)
        {
            startGameButton.interactable = canStart;
        }

        if (helperText != null)
        {
            helperText.text = canStart
                ? "Отряд готов. Можно начинать забег."
                : "Добавьте хотя бы один слот, чтобы начать игру.";
        }

        if (addPlayerButton != null)
        {
            bool canAddPlayer = _runtimeSlotViews.Count < GameSessionState.MaxSlots;
            addPlayerButton.interactable = canAddPlayer;
            addPlayerButton.gameObject.SetActive(canAddPlayer);
        }
    }

    private void AddPlayerSlot()
    {
        if (_templateSlotView == null || _templateSlotView.Root == null || _slotContainer == null)
        {
            return;
        }

        if (_runtimeSlotViews.Count >= GameSessionState.MaxSlots)
        {
            return;
        }

        int slotIndex = _runtimeSlotViews.Count;
        GameObject slotRoot = Instantiate(_templateSlotView.Root, _slotContainer, false);
        slotRoot.name = $"SlotCard_{slotIndex + 1}";
        slotRoot.SetActive(true);

        SlotView slotView = CreateSlotViewFromRoot(slotRoot);
        ConfigureSlotView(slotView);
        _runtimeSlotViews.Add(slotView);

        GameSessionState.SetSlot(slotIndex, true, string.Empty, GameSessionState.GetClassIdByIndex(0));
        RefreshAllSlots(syncFields: true);
    }

    private void ConfigureSlotView(SlotView slotView)
    {
        if (slotView == null)
        {
            return;
        }

        if (slotView.ClassDropdown != null)
        {
            slotView.ClassDropdown.ClearOptions();
            slotView.ClassDropdown.AddOptions(_classLabels);
            slotView.ClassDropdown.onValueChanged.RemoveAllListeners();
            slotView.ClassDropdown.onValueChanged.AddListener(_ => OnSlotEdited(slotView));
        }

        if (slotView.NameInput != null)
        {
            slotView.NameInput.onValueChanged.RemoveAllListeners();
            slotView.NameInput.onValueChanged.AddListener(_ => OnSlotEdited(slotView));
        }

        if (slotView.RemoveButton != null)
        {
            slotView.RemoveButton.onClick.RemoveAllListeners();
            slotView.RemoveButton.onClick.AddListener(() => RemoveSlot(slotView));
        }
    }

    private void OnSlotEdited(SlotView slotView)
    {
        int slotIndex = _runtimeSlotViews.IndexOf(slotView);
        if (slotIndex < 0)
        {
            return;
        }

        LobbyCharacterSlot currentSlot = GameSessionState.GetSlot(slotIndex);
        if (!currentSlot.IsEnabled)
        {
            return;
        }

        string slotName = slotView.NameInput != null ? slotView.NameInput.text : currentSlot.Name;
        int classIndex = slotView.ClassDropdown != null ? slotView.ClassDropdown.value : 0;
        string classId = GameSessionState.GetClassIdByIndex(classIndex);

        GameSessionState.SetSlot(slotIndex, true, slotName, classId);
        RefreshSlot(slotView, slotIndex, syncFields: false);
        RefreshStartButton();
    }

    private void RemoveSlot(SlotView slotView)
    {
        int slotIndex = _runtimeSlotViews.IndexOf(slotView);
        if (slotIndex < 0)
        {
            return;
        }

        for (int i = slotIndex; i < _runtimeSlotViews.Count - 1; i++)
        {
            LobbyCharacterSlot nextSlot = GameSessionState.GetSlot(i + 1);
            GameSessionState.SetSlot(i, true, nextSlot.Name, nextSlot.ClassId);
        }

        GameSessionState.ClearSlot(_runtimeSlotViews.Count - 1);
        _runtimeSlotViews.RemoveAt(slotIndex);

        if (slotView.Root != null)
        {
            Destroy(slotView.Root);
        }

        RefreshAllSlots(syncFields: true);
    }

    public void BackToMainMenu()
    {
        SceneManager.LoadScene(GameSceneNames.MainMenuScene);
    }

    public void StartGame()
    {
        if (!GameSessionState.HasReadyCharacters())
        {
            return;
        }

        GameSessionState.ClearWinner();
        SceneManager.LoadScene(GameSceneNames.GameScene);
    }

    private SlotView CreateSlotViewFromRoot(GameObject root)
    {
        Transform rootTransform = root.transform;
        return new SlotView
        {
            Root = root,
            CardBackground = root.GetComponent<Image>(),
            TitleText = ResolveRelativeComponent<TMP_Text>(rootTransform, _titlePath),
            StateText = ResolveRelativeComponent<TMP_Text>(rootTransform, _stateTextPath),
            NameInput = ResolveRelativeComponent<TMP_InputField>(rootTransform, _nameInputPath),
            ClassDropdown = ResolveRelativeComponent<TMP_Dropdown>(rootTransform, _classDropdownPath),
            RemoveButton = ResolveRelativeComponent<Button>(rootTransform, _removeButtonPath)
        };
    }

    private static T ResolveRelativeComponent<T>(Transform root, string relativePath) where T : Component
    {
        if (root == null)
        {
            return null;
        }

        if (string.IsNullOrEmpty(relativePath))
        {
            return root.GetComponent<T>();
        }

        Transform target = root.Find(relativePath);
        return target != null ? target.GetComponent<T>() : null;
    }

    private static string GetRelativePath(Transform root, Component component)
    {
        if (root == null || component == null)
        {
            return null;
        }

        Transform target = component.transform;
        if (target == root)
        {
            return string.Empty;
        }

        var pathSegments = new List<string>();
        while (target != null && target != root)
        {
            pathSegments.Add(target.name);
            target = target.parent;
        }

        if (target != root)
        {
            return null;
        }

        pathSegments.Reverse();
        return string.Join("/", pathSegments);
    }
}
