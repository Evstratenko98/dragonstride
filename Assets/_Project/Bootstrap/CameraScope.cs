using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class CameraScope : LifetimeScope
{
    [SerializeField] private ConfigScriptableObject _config;

    protected override void Configure(IContainerBuilder builder)
    {
        if (_config == null)
        {
            throw new InvalidOperationException($"{nameof(CameraScope)} requires a {nameof(ConfigScriptableObject)} reference.");
        }
        
        builder.RegisterInstance(_config);
        builder.RegisterInstance(Camera.main);
        builder.Register<CameraFocusState>(Lifetime.Singleton);
        builder.RegisterEntryPoint<CameraFollowDriver>();
    }

    protected override LifetimeScope FindParent()
    {
        LifetimeScope parent = AppScope.Instance;
        if (parent != null)
        {
            return parent;
        }

        throw new InvalidOperationException(
            "[CameraScope] AppScope parent was not found. Ensure AppScopeRuntimeBootstrap is active and AppScope exists before loading GameScene.");
    }
} 
