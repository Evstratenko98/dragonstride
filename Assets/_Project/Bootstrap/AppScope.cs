using UnityEngine;
using VContainer;
using VContainer.Unity;

// TODO: везде сделать корректные интерфейсы
public class AppScope : LifetimeScope
{
    private static AppScope _instance;
    private const string MultiplayerConfigResourcePath = "MultiplayerConfig";
    private const string CharacterCatalogResourcePath = "CharacterCatalog";

    [SerializeField] private MultiplayerConfig multiplayerConfig;
    [SerializeField] private CharacterCatalog characterCatalog;

    public static AppScope Instance => _instance;

    protected override void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        base.Awake();
    }

    protected override void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }

        base.OnDestroy();
    }

    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<IEventBus, EventBus>(Lifetime.Singleton);
        builder.Register<ISessionSceneRouter, SessionSceneRouter>(Lifetime.Singleton);
        builder.RegisterInstance(ResolveMultiplayerConfig());
        builder.RegisterInstance(ResolveCharacterCatalog());
        builder.Register<IMultiplayerBootstrapService, MpsBootstrapService>(Lifetime.Singleton);
        builder.Register<IMultiplayerSessionService, MpsSessionService>(Lifetime.Singleton);
        builder.Register<ICharacterDraftService, MpsCharacterDraftService>(Lifetime.Singleton);
        builder.Register<IMatchSetupContextService, MatchSetupContextService>(Lifetime.Singleton);
        builder.Register<IMatchNetworkService, MpsMatchNetworkService>(Lifetime.Singleton);
        builder.RegisterEntryPoint<MultiplayerBootstrapEntryPoint>(Lifetime.Singleton).AsSelf();
    }

    private MultiplayerConfig ResolveMultiplayerConfig()
    {
        if (multiplayerConfig != null)
        {
            return multiplayerConfig;
        }

        multiplayerConfig = Resources.Load<MultiplayerConfig>(MultiplayerConfigResourcePath);
        if (multiplayerConfig != null)
        {
            return multiplayerConfig;
        }

        Debug.LogWarning(
            "[AppScope] MultiplayerConfig asset was not found in Resources. Runtime defaults will be used.");
        multiplayerConfig = ScriptableObject.CreateInstance<MultiplayerConfig>();
        return multiplayerConfig;
    }

    private CharacterCatalog ResolveCharacterCatalog()
    {
        if (characterCatalog != null)
        {
            return characterCatalog;
        }

        characterCatalog = Resources.Load<CharacterCatalog>(CharacterCatalogResourcePath);
        if (characterCatalog != null)
        {
            return characterCatalog;
        }

        Debug.LogError(
            "[AppScope] CharacterCatalog asset was not found in Resources. CharacterSelect and GameScene spawn will not work correctly.");
        characterCatalog = ScriptableObject.CreateInstance<CharacterCatalog>();
        return characterCatalog;
    }
}
