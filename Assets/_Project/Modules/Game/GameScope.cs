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
    [SerializeField] private LinkView linkViewPrefab;
    [SerializeField] private CellColorTheme colorTheme;
    [SerializeField] private FogOfWarView fogOfWarViewPrefab;

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
        builder.RegisterInstance(linkViewPrefab);
        builder.RegisterInstance(fogOfWarViewPrefab);
        builder.Register<FieldRoot>(Lifetime.Singleton);
        builder.Register<FieldViewFactory>(Lifetime.Singleton);
        builder.Register<FieldState>(Lifetime.Singleton);
        builder.Register<FieldGenerator>(Lifetime.Singleton);

        //CharacterModule
        builder.RegisterInstance(characterPrefabs).As<CharacterView[]>();
        builder.Register<CharacterModel>(Lifetime.Transient);
        builder.Register<CharacterInstance>(Lifetime.Transient);
        builder.Register<CharacterInput>(Lifetime.Singleton);
        builder.Register<CharacterFactory>(Lifetime.Singleton);
        builder.Register<CharacterCellLayoutService>(Lifetime.Singleton);
        builder.Register<CharacterService>(Lifetime.Singleton);

        //ItemModule
        builder.Register<ItemService>(Lifetime.Singleton);

        // Controllers
        builder.Register<FieldPresenter>(Lifetime.Singleton);
        builder.RegisterEntryPoint<FogOfWarPresenter>(Lifetime.Singleton).AsSelf();
        builder.RegisterEntryPoint<CharacterController>(Lifetime.Singleton).AsSelf();
        builder.RegisterEntryPoint<TurnController>(Lifetime.Singleton).AsSelf();
        builder.RegisterEntryPoint<GameController>();
    }
}
