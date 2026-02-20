using UnityEngine;
using VContainer;
using VContainer.Unity;

// TODO: везде сделать корректные интерфейсы
public class AppScope : LifetimeScope
{
    private const string MultiplayerConfigResourcePath = "MultiplayerConfig";

    [SerializeField] private MultiplayerConfig multiplayerConfig;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<IEventBus, EventBus>(Lifetime.Singleton);
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
