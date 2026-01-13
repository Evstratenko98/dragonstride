using UnityEngine;
using UnityEngine.UI;
using VContainer;

public class UIScope : MonoBehaviour
{
    [SerializeField] private CharacterMenuView characterMenuView;
    [SerializeField] private FinishGameView finishGameView;
    
    public void Install(IContainerBuilder builder)
    {
        builder.RegisterComponent(characterMenuView);
        builder.RegisterComponent(finishGameView);

        builder.RegisterEntryPoint<CharacterMenuController>();
        builder.RegisterEntryPoint<FinishGameController>();
    }
}
