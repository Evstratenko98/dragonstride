using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class ItemService
{
    private readonly ItemConfig _config;

    // Отслеживаем созданные уникальные предметы
    private readonly HashSet<string> _spawnedUniqueIds = new();

    public ItemService(ItemConfig config)
    {
        _config = config;
    }

    // ------------------------------------------------------------
    // 1. СОЗДАНИЕ ПРЕДМЕТА
    // ------------------------------------------------------------
    public ItemModel CreateItem(string id)
    {
        var def = _config.AllItems.FirstOrDefault(i => i.Id == id);

        if (def == null)
        {
            Debug.LogError($"[ItemService] Нет предмета с Id={id}");
            return null;
        }

        // Если предмет уникальный, но уже существует
        if (def.Rarity == ItemRarity.Unique && _spawnedUniqueIds.Contains(def.Id))
        {
            Debug.LogWarning($"[ItemService] Уникальный предмет {def.Id} уже существует. Создаю редкий предмет вместо него.");

            return CreateRandomItemByRarity(ItemRarity.Rare);
        }

        var model = new ItemModel(def);

        if (def.Rarity == ItemRarity.Unique)
            _spawnedUniqueIds.Add(def.Id);

        return model;
    }

    // ------------------------------------------------------------
    // 2. СОЗДАНИЕ ЛУТА ИЗ СУНДУКА
    // ------------------------------------------------------------
    public ItemModel CreateRandomChestLoot()
    {
        var rarity = RollRarity();
        return CreateRandomItemByRarity(rarity);
    }

    // ------------------------------------------------------------
    // 3. СОЗДАНИЕ СЛУЧАЙНОГО ПРЕДМЕТА ПО РЕДКОСТИ
    // ------------------------------------------------------------
    private ItemModel CreateRandomItemByRarity(ItemRarity rarity)
    {
        var items = _config.AllItems
            .Where(i => i.Rarity == rarity)
            .Where(i => i.Rarity != ItemRarity.Unique || !_spawnedUniqueIds.Contains(i.Id))
            .ToList();

        if (items.Count == 0)
        {
            Debug.LogWarning($"[ItemService] Не нашел доступных предметов редкости {rarity}. Пытаюсь создать Common.");
            return CreateRandomItemByRarity(ItemRarity.Common);
        }

        var def = items[Random.Range(0, items.Count)];
        return CreateItem(def.Id);
    }

    // ------------------------------------------------------------
    // 4. УДАЛЕНИЕ ПРЕДМЕТА ИЗ ИГРЫ
    // ------------------------------------------------------------
    public void DeleteItem(ItemModel item)
    {
        if (item == null || item.Definition == null)
            return;

        if (item.Definition.Rarity == ItemRarity.Unique)
        {
            // возвращаем возможность вновь создать этот предмет
            _spawnedUniqueIds.Remove(item.Definition.Id);
        }
    }

    // ------------------------------------------------------------
    // ДОПОЛНИТЕЛЬНЫЕ ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
    // ------------------------------------------------------------
    public bool IsUniqueSpawned(string id) => _spawnedUniqueIds.Contains(id);

    private ItemRarity RollRarity()
    {
        float totalWeight = _config.ChestDropTable.Sum(r => r.Weight);
        float roll = Random.value * totalWeight;
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
