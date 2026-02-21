using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VContainer.Unity;

public class ChestLootPresenter : IPostInitializable, IDisposable
{
    private readonly IEventBus _eventBus;
    private readonly ChestLootView _view;
    private readonly IMultiplayerSessionService _sessionService;
    private readonly IMatchNetworkService _matchNetworkService;
    private readonly ItemConfig _itemConfig;

    private readonly List<ItemDefinition> _currentLoot = new();
    private readonly Dictionary<string, ItemDefinition> _itemById = new();

    private IDisposable _offlineLootSubscription;
    private IDisposable _onlineLootGeneratedSubscription;
    private IDisposable _onlineLootTakenSubscription;

    private CharacterInstance _currentCharacter;
    private int _currentOnlineActorId;
    private string _currentOnlineOwnerPlayerId = string.Empty;
    private bool _isTakeInProgress;

    public ChestLootPresenter(
        IEventBus eventBus,
        ChestLootView view,
        IMultiplayerSessionService sessionService,
        IMatchNetworkService matchNetworkService,
        ItemConfig itemConfig)
    {
        _eventBus = eventBus;
        _view = view;
        _sessionService = sessionService;
        _matchNetworkService = matchNetworkService;
        _itemConfig = itemConfig;
    }

    public void PostInitialize()
    {
        BuildItemLookup();

        _view.Hide();
        _offlineLootSubscription = _eventBus.Subscribe<ChestLootOpened>(OnChestLootOpened);
        _onlineLootGeneratedSubscription = _eventBus.Subscribe<OnlineLootGenerated>(OnOnlineLootGenerated);
        _onlineLootTakenSubscription = _eventBus.Subscribe<OnlineLootTaken>(OnOnlineLootTaken);

        if (_view.TakeButton != null)
        {
            _view.TakeButton.onClick.AddListener(OnTakeClicked);
        }
    }

    public void Dispose()
    {
        _offlineLootSubscription?.Dispose();
        _onlineLootGeneratedSubscription?.Dispose();
        _onlineLootTakenSubscription?.Dispose();

        if (_view.TakeButton != null)
        {
            _view.TakeButton.onClick.RemoveListener(OnTakeClicked);
        }
    }

    private void OnChestLootOpened(ChestLootOpened msg)
    {
        if (_sessionService != null && _sessionService.HasActiveSession)
        {
            return;
        }

        _currentCharacter = msg.Character;
        _currentOnlineActorId = 0;
        _currentOnlineOwnerPlayerId = string.Empty;
        _isTakeInProgress = false;

        _currentLoot.Clear();
        if (msg.Loot != null)
        {
            _currentLoot.AddRange(msg.Loot);
        }

        RenderLootAndShow(canTake: true);
    }

    private void OnOnlineLootGenerated(OnlineLootGenerated msg)
    {
        _currentCharacter = null;
        _currentOnlineActorId = msg.ActorId;
        _currentOnlineOwnerPlayerId = msg.OwnerPlayerId ?? string.Empty;
        _isTakeInProgress = false;

        _currentLoot.Clear();
        IReadOnlyList<LootItemSnapshot> loot = msg.Loot;
        if (loot != null)
        {
            for (int i = 0; i < loot.Count; i++)
            {
                LootItemSnapshot item = loot[i];
                if (!_itemById.TryGetValue(item.ItemId, out ItemDefinition definition) || definition == null)
                {
                    continue;
                }

                int count = Math.Max(1, item.Count);
                for (int j = 0; j < count; j++)
                {
                    _currentLoot.Add(definition);
                }
            }
        }

        RenderLootAndShow(CanLocalPlayerTakeOnlineLoot());
    }

    private void OnOnlineLootTaken(OnlineLootTaken msg)
    {
        if (msg.ActorId <= 0 || msg.ActorId != _currentOnlineActorId)
        {
            return;
        }

        ClearAndHide();
    }

    private async void OnTakeClicked()
    {
        if (_sessionService != null && _sessionService.HasActiveSession)
        {
            await OnTakeClickedOnlineAsync();
            return;
        }

        OnTakeClickedOffline();
    }

    private async Task OnTakeClickedOnlineAsync()
    {
        if (_isTakeInProgress || !CanLocalPlayerTakeOnlineLoot())
        {
            return;
        }

        _isTakeInProgress = true;
        SetTakeButtonInteractable(false);
        _eventBus.Publish(new TakeLootRequested());
        _ = RestoreTakeButtonIfStillPendingAsync(_currentOnlineActorId);
        await Task.CompletedTask;
    }

    private void OnTakeClickedOffline()
    {
        if (_currentCharacter?.Model?.Inventory != null)
        {
            for (int i = 0; i < _currentLoot.Count; i++)
            {
                ItemDefinition item = _currentLoot[i];
                if (item != null)
                {
                    _currentCharacter.Model.Inventory.AddItem(item);
                }
            }
        }

        ClearAndHide();
    }

    private bool CanLocalPlayerTakeOnlineLoot()
    {
        if (_currentOnlineActorId <= 0 || string.IsNullOrWhiteSpace(_currentOnlineOwnerPlayerId))
        {
            return false;
        }

        string localPlayerId = _matchNetworkService != null ? _matchNetworkService.LocalPlayerId : string.Empty;
        return !string.IsNullOrWhiteSpace(localPlayerId) &&
               string.Equals(localPlayerId, _currentOnlineOwnerPlayerId, StringComparison.Ordinal);
    }

    private void RenderLootAndShow(bool canTake)
    {
        if (_view.LootGridView != null)
        {
            _view.LootGridView.SetItems(_currentLoot);
        }

        SetTakeButtonInteractable(canTake && !_isTakeInProgress);
        _view.Show();
    }

    private void ClearAndHide()
    {
        _currentCharacter = null;
        _currentOnlineActorId = 0;
        _currentOnlineOwnerPlayerId = string.Empty;
        _isTakeInProgress = false;

        _currentLoot.Clear();
        _view.LootGridView?.Clear();
        _view.Hide();
    }

    private void SetTakeButtonInteractable(bool isInteractable)
    {
        if (_view.TakeButton != null)
        {
            _view.TakeButton.interactable = isInteractable;
        }
    }

    private void BuildItemLookup()
    {
        _itemById.Clear();
        if (_itemConfig?.AllItems == null)
        {
            return;
        }

        for (int i = 0; i < _itemConfig.AllItems.Count; i++)
        {
            ItemDefinition definition = _itemConfig.AllItems[i];
            if (definition == null || string.IsNullOrWhiteSpace(definition.Id))
            {
                continue;
            }

            _itemById[definition.Id] = definition;
        }
    }

    private async Task RestoreTakeButtonIfStillPendingAsync(int actorId)
    {
        await Task.Delay(1200);
        if (!_isTakeInProgress || actorId != _currentOnlineActorId || !CanLocalPlayerTakeOnlineLoot())
        {
            return;
        }

        _isTakeInProgress = false;
        SetTakeButtonInteractable(true);
    }
}
