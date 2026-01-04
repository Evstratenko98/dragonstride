using UnityEngine;
using VContainer;
using VContainer.Unity;

public class GameScope : LifetimeScope
{   
    //Config
    [Header("Configs")]
    [SerializeField] private ConfigScriptableObject _config;
    [SerializeField] private ItemConfig _itemConfig;

    //FieldModule
    [SerializeField] private CellView cellViewPrefab;
    [SerializeField] private LinkView linkView;
    [SerializeField] private CellColorTheme colorTheme;

    //CharacterModule
    [SerializeField] private CharacterView[] characterPrefabs;

    protected override void Configure(IContainerBuilder builder)
    {
        //Config
        builder.RegisterInstance(_config);
        builder.RegisterInstance(_itemConfig);
        
        //FieldModule
        builder.RegisterInstance(colorTheme);
        builder.RegisterComponent(cellViewPrefab);
        builder.RegisterComponent(linkView).As<ILinkView>();
        builder.Register<CellModel>(Lifetime.Transient);
        builder.Register<LinkModel>(Lifetime.Transient);
        builder.Register<IFieldService, FieldService>(Lifetime.Singleton);
        builder.Register<IMazeGenerator, MazeGenerator>(Lifetime.Singleton);

        //CharacterModule
        builder.RegisterInstance(characterPrefabs).As<CharacterView[]>();
        builder.Register<CharacterModel>(Lifetime.Transient);
        builder.Register<ICharacterInstance, CharacterInstance>(Lifetime.Transient);
        builder.Register<ICharacterInput, CharacterInput>(Lifetime.Singleton);
        builder.Register<ICharacterFactory, CharacterFactory>(Lifetime.Singleton);
        builder.Register<ICharacterService, CharacterService>(Lifetime.Singleton);

        //ItemModule
        builder.Register<ItemService>(Lifetime.Singleton).As<IItemService>();

        // Controllers
        builder.Register<FieldController>(Lifetime.Singleton);
        // TODO: исправить эту хуйню
        builder.Register<CharacterController>(Lifetime.Singleton);

        builder.RegisterEntryPoint<CharacterController>();
        builder.RegisterEntryPoint<TurnController>();
        builder.RegisterEntryPoint<GameController>();
    }
}
