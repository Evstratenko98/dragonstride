using UnityEngine;
using UnityEngine.Serialization;
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
    [FormerlySerializedAs("slimePrefab")]
    [SerializeField] private GameObject slimePrefab;
    [SerializeField] private GameObject wolfPrefab;
    [SerializeField] private GameObject bossPrefab;

    [Header("Enemy Spawn Chances (%)")]
    [SerializeField] private int slimeSpawnChancePercent = 60;
    [SerializeField] private int wolfSpawnChancePercent = 40;

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
        builder.Register<CharacterInputReader>(Lifetime.Singleton);
        builder.Register<CharacterFactory>(Lifetime.Singleton);
        builder.Register<CharacterLifecycleService>(Lifetime.Singleton);
        builder.Register<CharacterLayout>(Lifetime.Singleton);
        builder.Register<CharacterRoster>(Lifetime.Singleton);
        builder.RegisterInstance(new EnemyPrefabs(
            slimePrefab,
            wolfPrefab,
            bossPrefab,
            slimeSpawnChancePercent,
            wolfSpawnChancePercent));
        builder.Register<EnemySpawner>(Lifetime.Singleton);
        builder.RegisterEntryPoint<EnemyTurnDriver>(Lifetime.Singleton).AsSelf();
        builder.Register<CellOpenService>(Lifetime.Singleton);
        builder.Register<CommonCellOpenHandler>(Lifetime.Singleton).As<ICellOpenHandler>();
        builder.Register<StartCellOpenHandler>(Lifetime.Singleton).As<ICellOpenHandler>();
        builder.Register<BossCellOpenHandler>(Lifetime.Singleton).As<ICellOpenHandler>();
        builder.Register<LootCellOpenHandler>(Lifetime.Singleton).As<ICellOpenHandler>();
        builder.Register<FightCellOpenHandler>(Lifetime.Singleton).As<ICellOpenHandler>();
        builder.Register<TeleportCellOpenHandler>(Lifetime.Singleton).As<ICellOpenHandler>();

        builder.Register<ItemFactory>(Lifetime.Singleton);
        builder.Register<ConsumableItemUseService>(Lifetime.Singleton);
        builder.Register<CrownOwnershipService>(Lifetime.Singleton);

        builder.Register<IRandomSource, UnityRandomSource>(Lifetime.Singleton);
        builder.Register<TurnActorRegistry>(Lifetime.Singleton);
        builder.Register<FieldPresenter>(Lifetime.Singleton);
        builder.RegisterEntryPoint<FogOfWarPresenter>(Lifetime.Singleton).AsSelf();
        builder.RegisterEntryPoint<CharacterMovementDriver>(Lifetime.Singleton).AsSelf();
        builder.RegisterEntryPoint<CellOpenDriver>(Lifetime.Singleton).AsSelf();
        builder.RegisterEntryPoint<ActionPanelAvailabilityDriver>(Lifetime.Singleton).AsSelf();
        builder.RegisterEntryPoint<AttackDriver>(Lifetime.Singleton).AsSelf();
        builder.RegisterEntryPoint<TurnFlow>(Lifetime.Singleton).AsSelf();
        builder.RegisterEntryPoint<GameFlow>();
    }
}
