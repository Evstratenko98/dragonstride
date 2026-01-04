using UnityEngine;
using VContainer;
using VContainer.Unity;

// TODO: везде сделать корректные интерфейсы
public class AppScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<IEventBus, EventBus>(Lifetime.Singleton);
    }
}
