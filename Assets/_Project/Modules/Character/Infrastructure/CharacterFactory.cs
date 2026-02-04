using System.Linq;
using UnityEngine;

public class CharacterFactory
{
    private readonly ConfigScriptableObject _config;
    private readonly IEventBus _eventBus;
    private readonly CharacterView[] _prefabs;
    private readonly ItemFactory _itemFactory;
    private readonly FieldRoot _fieldRootService;

    public CharacterFactory(
        ConfigScriptableObject config,
        IEventBus eventBus,
        CharacterView[] prefabs,
        ItemFactory itemFactory,
        FieldRoot fieldRootService)
    {
        _config = config;
        _eventBus = eventBus;
        _prefabs = prefabs;
        _itemFactory = itemFactory;
        _fieldRootService = fieldRootService;
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

        Character model = new Character();
        model.SetName(name);
        model.InitializeInventory(_config.INVENTORY_CAPACITY);
        model.InitializeEquipment();
        characterClass.Apply(model);
        
        Item sword = _itemFactory.CreateItem("sword_common");
        Item healthFlaskSmall = _itemFactory.CreateItem("health_flask_small");
        model.Inventory.AddItem(sword.Definition);
        model.Inventory.AddItem(healthFlaskSmall.Definition);

        CharacterInstance instance = new CharacterInstance(_config, model, prefab, name, _eventBus, _fieldRootService);

        return instance;
    }
}
