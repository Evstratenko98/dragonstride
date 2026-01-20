using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;

public class UIScope : LifetimeScope
{
    [SerializeField] private CharacterMenuView characterMenuView;
    [SerializeField] private FinishGameView finishGameView;
    [SerializeField] private UIScreenView screenView;
    
    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterComponent(characterMenuView);
        builder.RegisterComponent(finishGameView);
        builder.RegisterComponent(screenView);
        
        builder.RegisterEntryPoint<CharacterMenuController>();
        builder.RegisterEntryPoint<FinishGameController>();
        builder.RegisterEntryPoint<UIScreenController>();
    }
}
