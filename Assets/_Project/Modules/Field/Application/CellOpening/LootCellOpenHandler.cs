using System.Collections.Generic;

public sealed class LootCellOpenHandler : ICellOpenHandler
{
    private readonly ItemFactory _itemFactory;
    private readonly IEventBus _eventBus;

    public CellType CellType => CellType.Loot;

    public LootCellOpenHandler(ItemFactory itemFactory, IEventBus eventBus)
    {
        _itemFactory = itemFactory;
        _eventBus = eventBus;
    }

    public bool TryOpen(ICellLayoutOccupant actor, Cell cell)
    {
        if (actor is not CharacterInstance character)
        {
            return false;
        }

        int lootCount = UnityEngine.Random.Range(1, 4);
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
