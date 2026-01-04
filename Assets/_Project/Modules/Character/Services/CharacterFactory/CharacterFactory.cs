public class CharacterFactory : ICharacterFactory
{
    private readonly ConfigScriptableObject _config;
    private readonly IEventBus _eventBus;
    private readonly CharacterView[] _prefabs;
    private readonly IItemService _itemService;

    public CharacterFactory(ConfigScriptableObject config, IEventBus eventBus, CharacterView[] prefabs, IItemService itemService)
    {
        _config = config;
        _eventBus = eventBus;
        _prefabs = prefabs;
        _itemService = itemService;
    }

    public ICharacterInstance Create(string name, int prefabIndex, ICharacterClass characterClass)
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

        ICharacterInstance instance = new CharacterInstance(_config, model, _prefabs[prefabIndex], name, _eventBus);

        return instance;
    }
}
