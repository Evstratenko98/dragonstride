using UnityEngine;
using VContainer;
using VContainer.Unity;

public class CameraScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterInstance(Camera.main);
        builder.Register<ICameraService, CameraService>(Lifetime.Singleton);
        builder.RegisterEntryPoint<CameraController>();
    }
}
