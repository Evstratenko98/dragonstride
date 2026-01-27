using System.Linq;
using UnityEngine;

public class CharacterFactory
{
    private readonly ConfigScriptableObject _config;
    private readonly IEventBus _eventBus;
    private readonly CharacterView[] _prefabs;
    private readonly ItemService _itemService;

    public CharacterFactory(ConfigScriptableObject config, IEventBus eventBus, CharacterView[] prefabs, ItemService itemService)
    {
        _config = config;
        _eventBus = eventBus;
        _prefabs = prefabs;
        _itemService = itemService;
    }

    public CharacterInstance Create(string name, int prefabIndex, CharacterClass characterClass)
    {
        if (_prefabs == null || _prefabs.Length == 0)
        {
            Debug.LogError("[CharacterFactory] Character prefabs are not configured.");
            return null;
        }

        if (prefabIndex < 0 || prefabIndex >= _prefabs.Length)
        {
            Debug.LogError($"[CharacterFactory] Character prefab index {prefabIndex} is out of range (0-{_prefabs.Length - 1}).");
            return null;
        }

        CharacterView prefab = _prefabs[prefabIndex];
        if (prefab == null)
        {
            Debug.LogError($"[CharacterFactory] Character prefab at index {prefabIndex} is not assigned.");
            return null;
        }

        CharacterModel model = new CharacterModel();
        model.InitializeInventory(_config.INVENTORY_CAPACITY);
        model.InitializeEquipment();
        characterClass.Apply(model);
        
        // 4. Создаём стартовый предмет
        string startItem = "sword_common";
        ItemModel sword = _itemService.CreateItem(startItem);
        if (sword != null)
        {
            bool added = model.Inventory.AddItem(sword.Definition);
            bool hasSword = added && model.Inventory.Slots.Any(slot => slot.Definition == sword.Definition && slot.Count > 0);
            string swordName = !string.IsNullOrEmpty(sword.Definition.DisplayName)
                ? sword.Definition.DisplayName
                : sword.Definition.Id;
            if (hasSword)
            {
                Debug.Log($"[CharacterFactory] Игрок \"{name}\" имеет предмет \"{swordName}\" в инвентаре.");
            }
            else
            {
                Debug.LogWarning($"[CharacterFactory] Игрок \"{name}\" не получил предмет \"{swordName}\" в инвентарь.");
            }
        }

        CharacterInstance instance = new CharacterInstance(_config, model, prefab, name, _eventBus);

        return instance;
    }
}
