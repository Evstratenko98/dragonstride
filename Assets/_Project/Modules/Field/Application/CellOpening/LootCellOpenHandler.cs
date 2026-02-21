using System.Collections.Generic;

public sealed class LootCellOpenHandler : ICellOpenHandler
{
    private readonly ItemFactory _itemFactory;
    private readonly IEventBus _eventBus;
    private readonly IRandomSource _randomSource;
    private readonly IActorIdentityService _actorIdentityService;
    private readonly ILootSyncService _lootSyncService;
    private readonly IMatchRuntimeRoleService _runtimeRoleService;

    public CellType CellType => CellType.Loot;

    public LootCellOpenHandler(
        ItemFactory itemFactory,
        IEventBus eventBus,
        IRandomSource randomSource,
        IActorIdentityService actorIdentityService,
        ILootSyncService lootSyncService,
        IMatchRuntimeRoleService runtimeRoleService)
    {
        _itemFactory = itemFactory;
        _eventBus = eventBus;
        _randomSource = randomSource;
        _actorIdentityService = actorIdentityService;
        _lootSyncService = lootSyncService;
        _runtimeRoleService = runtimeRoleService;
    }

    public bool TryOpen(ICellLayoutOccupant actor, Cell cell)
    {
        if (actor is not CharacterInstance character)
        {
            return false;
        }

        bool isOnlineHost = _runtimeRoleService != null &&
                            _runtimeRoleService.IsOnlineMatch &&
                            _runtimeRoleService.IsHostAuthority;
        if (isOnlineHost)
        {
            int actorId = _actorIdentityService != null ? _actorIdentityService.GetId(character) : 0;
            if (actorId <= 0)
            {
                return false;
            }

            MultiplayerOperationResult<IReadOnlyList<LootItemSnapshot>> generateResult =
                _lootSyncService.GenerateLootForCell(actorId, cell.X, cell.Y);
            return generateResult.IsSuccess;
        }

        int lootCount = _randomSource.Range(1, 4);
        var loot = new List<ItemDefinition>(lootCount);

        for (int i = 0; i < lootCount; i++)
        {
            var item = _itemFactory.CreateRandomChestLoot();
            if (item?.Definition != null)
            {
                loot.Add(item.Definition);
            }
        }

        if (loot.Count > 0)
        {
            _eventBus.Publish(new ChestLootOpened(character, loot));
        }

        return true;
    }
}
