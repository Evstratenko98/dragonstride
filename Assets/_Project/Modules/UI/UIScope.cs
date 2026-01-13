using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;

public class UIScope : LifetimeScope
{
    [SerializeField] private CharacterMenuView characterMenuView;
    [SerializeField] private FinishGameView finishGameView;
    
    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterComponent(characterMenuView);
        builder.RegisterComponent(finishGameView);
        
        builder.RegisterEntryPoint<CharacterMenuController>();
        builder.RegisterEntryPoint<FinishGameController>();
    }
}
