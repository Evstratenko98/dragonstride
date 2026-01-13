public class CharacterFactory
{
    private readonly ConfigScriptableObject _config;
    private readonly EventBus _eventBus;
    private readonly CharacterView[] _prefabs;
    private readonly ItemService _itemService;

    public CharacterFactory(ConfigScriptableObject config, EventBus eventBus, CharacterView[] prefabs, ItemService itemService)
    {
        _config = config;
        _eventBus = eventBus;
        _prefabs = prefabs;
        _itemService = itemService;
    }

    public CharacterInstance Create(string name, int prefabIndex, CharacterClass characterClass)
    {
        CharacterModel model = new CharacterModel();
        model.InitializeInventory(30);
        characterClass.Apply(model);
        
        // 4. Создаём стартовый предмет
        string startItem = "sword_common";
        ItemModel sword = _itemService.CreateItem(startItem);
        if (sword != null)
        {
            model.Inventory.AddItem(sword.Definition);
        }

        CharacterInstance instance = new CharacterInstance(_config, model, _prefabs[prefabIndex], name, _eventBus);

        return instance;
    }
}
