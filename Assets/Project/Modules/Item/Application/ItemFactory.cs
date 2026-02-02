using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public class ItemFactory
{
    private readonly ItemConfig _config;
    private readonly Random _random;

    private readonly HashSet<string> _spawnedUniqueIds = new();

    public ItemFactory(ItemConfig config)
    {
        _config = config;
        _random = new Random();
    }

    public Item CreateItem(string id)
    {
        var def = _config.AllItems.FirstOrDefault(i => i.Id == id);

        if (def == null)
        {
            Debug.WriteLine($"[ItemFactory] Нет предмета с Id={id}");
            return null;
        }

        if (def.Rarity == ItemRarity.Unique && _spawnedUniqueIds.Contains(def.Id))
        {
            Debug.WriteLine($"[ItemFactory] Уникальный предмет {def.Id} уже существует. Создаю редкий предмет вместо него.");

            return CreateRandomItemByRarity(ItemRarity.Rare);
        }

        var model = new Item(def);

        if (def.Rarity == ItemRarity.Unique)
            _spawnedUniqueIds.Add(def.Id);

        return model;
    }

    public Item CreateRandomChestLoot()
    {
        var rarity = RollRarity();
        return CreateRandomItemByRarity(rarity);
    }

    private Item CreateRandomItemByRarity(ItemRarity rarity)
    {
        var items = _config.AllItems
            .Where(i => i.Rarity == rarity)
            .Where(i => i.Rarity != ItemRarity.Unique || !_spawnedUniqueIds.Contains(i.Id))
            .ToList();

        if (items.Count == 0)
        {
            Debug.WriteLine($"[ItemFactory] Не нашел доступных предметов редкости {rarity}. Пытаюсь создать Common.");
            return CreateRandomItemByRarity(ItemRarity.Common);
        }

        var def = items[_random.Next(items.Count)];
        return CreateItem(def.Id);
    }

    public void DeleteItem(Item item)
    {
        if (item == null || item.Definition == null)
            return;

        if (item.Definition.Rarity == ItemRarity.Unique)
        {
            _spawnedUniqueIds.Remove(item.Definition.Id);
        }
    }

    public bool IsUniqueSpawned(string id) => _spawnedUniqueIds.Contains(id);

    private ItemRarity RollRarity()
    {
        float totalWeight = _config.ChestDropTable.Sum(r => r.Weight);
        float roll = (float)(_random.NextDouble() * totalWeight);
        float current = 0f;

        foreach (var entry in _config.ChestDropTable)
        {
            current += entry.Weight;
            if (roll <= current)
                return entry.Rarity;
        }

        return ItemRarity.Common;
    }
}
