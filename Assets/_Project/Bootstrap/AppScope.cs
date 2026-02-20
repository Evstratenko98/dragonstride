using UnityEngine;
using VContainer;
using VContainer.Unity;

// TODO: везде сделать корректные интерфейсы
public class AppScope : LifetimeScope
{
    private static AppScope _instance;
    private const string MultiplayerConfigResourcePath = "MultiplayerConfig";

    [SerializeField] private MultiplayerConfig multiplayerConfig;

    public static AppScope Instance => _instance;

    protected override void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning("[AppScope] Duplicate AppScope detected. Destroying duplicate instance.");
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
        builder.Register<IMultiplayerBootstrapService, MpsBootstrapService>(Lifetime.Singleton);
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
}
