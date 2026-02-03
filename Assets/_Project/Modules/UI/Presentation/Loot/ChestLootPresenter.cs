using System;
using System.Collections.Generic;
using VContainer.Unity;

public class ChestLootPresenter : IPostInitializable, IDisposable
{
    private readonly IEventBus _eventBus;
    private readonly ChestLootView _view;
    private IDisposable _lootSubscription;
    private CharacterInstance _currentCharacter;
    private readonly List<ItemDefinition> _currentLoot = new();

    public ChestLootPresenter(IEventBus eventBus, ChestLootView view)
    {
        _eventBus = eventBus;
        _view = view;
    }

    public void PostInitialize()
    {
        _view.Hide();
        _lootSubscription = _eventBus.Subscribe<ChestLootOpened>(OnChestLootOpened);

        if (_view.TakeButton != null)
        {
            _view.TakeButton.onClick.AddListener(OnTakeClicked);
        }
    }

    public void Dispose()
    {
        _lootSubscription?.Dispose();

        if (_view.TakeButton != null)
        {
            _view.TakeButton.onClick.RemoveListener(OnTakeClicked);
        }
    }

    private void OnChestLootOpened(ChestLootOpened msg)
    {
        _currentCharacter = msg.Character;
        _currentLoot.Clear();
        if (msg.Loot != null)
        {
            _currentLoot.AddRange(msg.Loot);
        }

        if (_view.LootGridView != null)
        {
            _view.LootGridView.SetItems(_currentLoot);
        }

        _view.Show();
    }

    private void OnTakeClicked()
    {
        if (_currentCharacter?.Model?.Inventory != null)
        {
            foreach (var item in _currentLoot)
            {
                if (item != null)
                {
                    _currentCharacter.Model.Inventory.AddItem(item);
                }
            }
        }

        _currentLoot.Clear();
        _view.LootGridView?.Clear();
        _view.Hide();
    }
}
