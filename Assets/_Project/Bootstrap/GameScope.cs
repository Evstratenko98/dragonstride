using UnityEngine;
using VContainer;
using VContainer.Unity;

public class GameScope : LifetimeScope
{   
    [Header("Configs")]
    [SerializeField] private ConfigScriptableObject _config;
    [SerializeField] private ItemConfig _itemConfig;

    [SerializeField] private CellView cellViewPrefab;
    [SerializeField] private LinkView linkViewPrefab;
    [SerializeField] private CellColorTheme colorTheme;
    [SerializeField] private FogOfWarView fogOfWarViewPrefab;

    [SerializeField] private CharacterView[] characterPrefabs;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterInstance(_config);
        builder.RegisterInstance(_itemConfig);
        
        builder.RegisterInstance(colorTheme);
        builder.RegisterComponent(cellViewPrefab);
        builder.RegisterInstance(linkViewPrefab);
        builder.RegisterInstance(fogOfWarViewPrefab);
        builder.Register<FieldRoot>(Lifetime.Singleton);
        builder.Register<FieldViewFactory>(Lifetime.Singleton);
        builder.Register<FieldState>(Lifetime.Singleton);
        builder.Register<FieldGenerator>(Lifetime.Singleton);

        builder.RegisterInstance(characterPrefabs).As<CharacterView[]>();
        builder.Register<Character>(Lifetime.Transient);
        builder.Register<CharacterInstance>(Lifetime.Transient);
        builder.Register<CharacterInputReader>(Lifetime.Singleton);
        builder.Register<CharacterFactory>(Lifetime.Singleton);
        builder.Register<CharacterLayout>(Lifetime.Singleton);
        builder.Register<CharacterRoster>(Lifetime.Singleton);

        builder.Register<ItemFactory>(Lifetime.Singleton);

        builder.Register<IRandomSource, UnityRandomSource>(Lifetime.Singleton);
        builder.Register<FieldPresenter>(Lifetime.Singleton);
        builder.RegisterEntryPoint<FogOfWarPresenter>(Lifetime.Singleton).AsSelf();
        builder.RegisterEntryPoint<CharacterMovementDriver>(Lifetime.Singleton).AsSelf();
        builder.RegisterEntryPoint<ActionPanelAvailabilityDriver>(Lifetime.Singleton).AsSelf();
        builder.RegisterEntryPoint<AttackDriver>(Lifetime.Singleton).AsSelf();
        builder.RegisterEntryPoint<TurnFlow>(Lifetime.Singleton).AsSelf();
        builder.RegisterEntryPoint<GameFlow>();
    }
}
