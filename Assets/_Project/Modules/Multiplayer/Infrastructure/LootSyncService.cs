using System;
using System.Collections.Generic;

public sealed class LootSyncService : ILootSyncService
{
    private readonly IActorIdentityService _actorIdentityService;
    private readonly ItemFactory _itemFactory;
    private readonly ItemConfig _itemConfig;
    private readonly IInventorySnapshotService _inventorySnapshotService;

    private readonly Dictionary<string, ItemDefinition> _itemById = new();
    private readonly Dictionary<int, PendingLoot> _pendingLootByActor = new();
    private readonly List<ActionEventEnvelope> _pendingTimelineEvents = new();

    public LootSyncService(
        IActorIdentityService actorIdentityService,
        ItemFactory itemFactory,
        ItemConfig itemConfig,
        IInventorySnapshotService inventorySnapshotService)
    {
        _actorIdentityService = actorIdentityService;
        _itemFactory = itemFactory;
        _itemConfig = itemConfig;
        _inventorySnapshotService = inventorySnapshotService;
        BuildItemLookup();
    }

    public bool HasPendingLootForActor(int actorId)
    {
        return actorId > 0 && _pendingLootByActor.ContainsKey(actorId);
    }

    public MultiplayerOperationResult<IReadOnlyList<LootItemSnapshot>> GenerateLootForCell(int actorId, int cellX, int cellY)
    {
        if (!TryResolveCharacter(actorId, out CharacterInstance character))
        {
            return MultiplayerOperationResult<IReadOnlyList<LootItemSnapshot>>.Failure(
                "actor_not_character",
                "Loot can be generated only for a character actor.");
        }

        var loot = RollLoot();
        _pendingLootByActor[actorId] = new PendingLoot(
            actorId,
            character.PlayerId ?? string.Empty,
            loot,
            cellX,
            cellY);

        _pendingTimelineEvents.Add(new ActionEventEnvelope(
            ActionEventType.LootGenerated,
            actorId,
            0,
            cellX,
            cellY,
            cellX,
            cellY,
            loot.Count,
            0,
            false,
            false,
            character.PlayerId ?? string.Empty,
            TimelinePayloadSerializer.SerializeLoot(loot),
            0));

        return MultiplayerOperationResult<IReadOnlyList<LootItemSnapshot>>.Success(loot);
    }

    public MultiplayerOperationResult<CharacterInventorySnapshot> ConfirmTakeLoot(int actorId)
    {
        if (!_pendingLootByActor.TryGetValue(actorId, out PendingLoot pending))
        {
            return MultiplayerOperationResult<CharacterInventorySnapshot>.Failure(
                "no_pending_loot",
                "No pending loot for actor.");
        }

        if (!TryResolveCharacter(actorId, out CharacterInstance character))
        {
            return MultiplayerOperationResult<CharacterInventorySnapshot>.Failure(
                "actor_not_character",
                "Pending loot owner is not a character.");
        }

        if (character.Model?.Inventory == null)
        {
            return MultiplayerOperationResult<CharacterInventorySnapshot>.Failure(
                "inventory_missing",
                "Character inventory is not initialized.");
        }

        for (int i = 0; i < pending.Loot.Count; i++)
        {
            LootItemSnapshot lootItem = pending.Loot[i];
            if (string.IsNullOrWhiteSpace(lootItem.ItemId))
            {
                continue;
            }

            if (!_itemById.TryGetValue(lootItem.ItemId, out ItemDefinition definition) || definition == null)
            {
                continue;
            }

            character.Model.Inventory.AddItem(definition, Math.Max(1, lootItem.Count));
        }

        _pendingLootByActor.Remove(actorId);
        CharacterInventorySnapshot inventorySnapshot = _inventorySnapshotService.Capture(actorId);

        _pendingTimelineEvents.Add(new ActionEventEnvelope(
            ActionEventType.LootTaken,
            actorId,
            0,
            pending.CellX,
            pending.CellY,
            pending.CellX,
            pending.CellY,
            pending.Loot.Count,
            0,
            false,
            false,
            pending.OwnerPlayerId,
            TimelinePayloadSerializer.SerializeLoot(pending.Loot),
            0));

        _pendingTimelineEvents.Add(new ActionEventEnvelope(
            ActionEventType.InventoryUpdated,
            actorId,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            false,
            false,
            pending.OwnerPlayerId,
            TimelinePayloadSerializer.SerializeInventory(inventorySnapshot),
            0));

        return MultiplayerOperationResult<CharacterInventorySnapshot>.Success(inventorySnapshot);
    }

    public MultiplayerOperationResult<CharacterInventorySnapshot> AutoTakeLootOnEndTurn(int actorId)
    {
        if (!_pendingLootByActor.ContainsKey(actorId))
        {
            return MultiplayerOperationResult<CharacterInventorySnapshot>.Failure(
                "no_pending_loot",
                "No pending loot for actor.");
        }

        return ConfirmTakeLoot(actorId);
    }

    public IReadOnlyList<ActionEventEnvelope> DrainPendingTimelineEvents()
    {
        if (_pendingTimelineEvents.Count == 0)
        {
            return Array.Empty<ActionEventEnvelope>();
        }

        var copy = new List<ActionEventEnvelope>(_pendingTimelineEvents);
        _pendingTimelineEvents.Clear();
        return copy;
    }

    private bool TryResolveCharacter(int actorId, out CharacterInstance character)
    {
        character = null;
        if (actorId <= 0 || _actorIdentityService == null ||
            !_actorIdentityService.TryGetActor(actorId, out ICellLayoutOccupant actor) ||
            actor is not CharacterInstance characterInstance)
        {
            return false;
        }

        character = characterInstance;
        return true;
    }

    private List<LootItemSnapshot> RollLoot()
    {
        var loot = new List<LootItemSnapshot>();
        if (_itemFactory == null)
        {
            return loot;
        }

        int lootCount = UnityEngine.Random.Range(1, 4);
        for (int i = 0; i < lootCount; i++)
        {
            Item generated = _itemFactory.CreateRandomChestLoot();
            string itemId = generated?.Definition?.Id;
            if (string.IsNullOrWhiteSpace(itemId))
            {
                continue;
            }

            loot.Add(new LootItemSnapshot(itemId, 1));
        }

        return loot;
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

    private readonly struct PendingLoot
    {
        public int ActorId { get; }
        public string OwnerPlayerId { get; }
        public IReadOnlyList<LootItemSnapshot> Loot { get; }
        public int CellX { get; }
        public int CellY { get; }

        public PendingLoot(int actorId, string ownerPlayerId, IReadOnlyList<LootItemSnapshot> loot, int cellX, int cellY)
        {
            ActorId = actorId;
            OwnerPlayerId = ownerPlayerId ?? string.Empty;
            Loot = loot ?? Array.Empty<LootItemSnapshot>();
            CellX = cellX;
            CellY = cellY;
        }
    }
}
