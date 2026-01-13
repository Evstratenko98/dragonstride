using UnityEngine;
using VContainer;
using VContainer.Unity;

public class CharacterScopeInstaller : MonoBehaviour
{
    [SerializeField] private CharacterView[] characterPrefabs;

    public void Install(IContainerBuilder builder)
    {
        builder.RegisterInstance(characterPrefabs).As<CharacterView[]>();
        builder.Register<CharacterModel>(Lifetime.Transient);
        builder.Register<ICharacterInstance, CharacterInstance>(Lifetime.Transient);
        builder.Register<ICharacterInput, CharacterInput>(Lifetime.Singleton);
        builder.Register<ICharacterFactory, CharacterFactory>(Lifetime.Singleton);
        builder.Register<ICharacterService, CharacterService>(Lifetime.Singleton);
        builder.RegisterEntryPoint<CharacterController>().As<ICharacterController>();
    }
}
