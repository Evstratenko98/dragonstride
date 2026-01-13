using UnityEngine;
using VContainer;
using VContainer.Unity;

public class GameScope : LifetimeScope
{   
    //Config
    [Header("Configs")]
    [SerializeField] private ConfigScriptableObject _config;
    [SerializeField] private ItemConfig _itemConfig;

    // Installers
    [SerializeField] private FieldScopeInstaller _fieldScope;
    [SerializeField] private CharacterScopeInstaller _characterScope;
    [SerializeField] private UIScope _uiScope;

    protected override void Configure(IContainerBuilder builder)
    {
        //Config
        builder.RegisterInstance(_config);
        builder.RegisterInstance(_itemConfig);

        _fieldScope.Install(builder);
        _characterScope.Install(builder);
        _uiScope.Install(builder);

        //ItemModule
        builder.Register<ItemService>(Lifetime.Singleton).As<IItemService>();
        builder.RegisterEntryPoint<TurnController>();
        builder.RegisterEntryPoint<GameController>();
    }
}
