using System;
using VContainer.Unity;

public class CharacterScreenPresenter : IPostInitializable, IDisposable
{
    private readonly IEventBus _eventBus;
    private readonly ConfigScriptableObject _config;
    private readonly CharacterScreenView _view;
    private IDisposable _openSubscription;
    private IDisposable _turnStateSubscription;
    private CharacterInstance _currentCharacter;

    public CharacterScreenPresenter(IEventBus eventBus, ConfigScriptableObject config, CharacterScreenView view)
    {
        _eventBus = eventBus;
        _config = config;
        _view = view;
    }

    public void PostInitialize()
    {
        _view.Hide();
        _openSubscription = _eventBus.Subscribe<CharacterScreenRequested>(OnOpenRequested);
        _turnStateSubscription = _eventBus.Subscribe<TurnPhaseChanged>(OnTurnStateChanged);

        if (_view.InventoryGridView != null)
        {
            _view.InventoryGridView.InitializeSlots(_config.INVENTORY_CAPACITY);
        }

        if (_view.EquipmentGridView != null)
        {
            _view.EquipmentGridView.InitializeSlots(2);
            _view.EquipmentGridView.BindInventoryGrid(_view.InventoryGridView);
        }

        if (_view.InventoryGridView != null)
        {
            _view.InventoryGridView.BindEquipmentGrid(_view.EquipmentGridView);
        }

        if (_view.CloseButton != null)
        {
            _view.CloseButton.onClick.AddListener(OnCloseClicked);
        }
    }

    public void Dispose()
    {
        _openSubscription?.Dispose();
        _turnStateSubscription?.Dispose();

        if (_view.CloseButton != null)
        {
            _view.CloseButton.onClick.RemoveListener(OnCloseClicked);
        }
    }

    private void OnOpenRequested(CharacterScreenRequested message)
    {
        _view.Show();
        _view.BindInventory(_currentCharacter?.Model?.Inventory);
        _view.BindEquipment(_currentCharacter?.Model?.Equipment);
    }

    private void OnCloseClicked()
    {
        _view.Hide();
    }

    private void OnTurnStateChanged(TurnPhaseChanged message)
    {
        if (message.Character == null)
        {
            return;
        }

        _currentCharacter = message.Character;

        if (_view.gameObject.activeSelf)
        {
            _view.BindInventory(_currentCharacter.Model.Inventory);
            _view.BindEquipment(_currentCharacter.Model.Equipment);
        }
    }
}
