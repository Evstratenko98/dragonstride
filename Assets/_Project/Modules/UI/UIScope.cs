using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;

public class UIScope : LifetimeScope
{
    [SerializeField] private GameScreenView gameScreenView;
    [SerializeField] private FinishScreenView finishScreenView;
    [SerializeField] private UIScreenView screenView;
    
    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterComponent(gameScreenView);
        builder.RegisterComponent(finishScreenView);
        builder.RegisterComponent(screenView);
        
        builder.RegisterEntryPoint<GameScreenController>();
        builder.RegisterEntryPoint<FinishScreenController>();
        builder.RegisterEntryPoint<UIScreenController>();
    }
}
