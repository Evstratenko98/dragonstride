using System;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;

public class UIScope : LifetimeScope
{
    [SerializeField] private ConfigScriptableObject config;
    [SerializeField] private GameScreenView gameScreenView;
    [SerializeField] private FinishScreenView finishScreenView;
    [SerializeField] private UIScreenView screenView;
    [SerializeField] private CharacterScreenView characterScreenView;
    [SerializeField] private ChestLootView chestLootView;
    
    protected override void Configure(IContainerBuilder builder)
    {
        if (config == null)
        {
            throw new InvalidOperationException($"{nameof(UIScope)} requires a {nameof(ConfigScriptableObject)} reference.");
        }

        builder.RegisterInstance(config);
        builder.RegisterComponent(EnsureComponent(gameScreenView, nameof(gameScreenView)));
        builder.RegisterComponent(EnsureComponent(finishScreenView, nameof(finishScreenView)));
        builder.RegisterComponent(EnsureComponent(screenView, nameof(screenView)));
        builder.RegisterComponent(EnsureComponent(characterScreenView, nameof(characterScreenView)));
        builder.RegisterComponent(EnsureComponent(chestLootView, nameof(chestLootView)));

        builder.RegisterEntryPoint<GameScreenPresenter>();
        builder.RegisterEntryPoint<FinishScreenPresenter>();
        builder.RegisterEntryPoint<UIScreenPresenter>();
        builder.RegisterEntryPoint<CharacterScreenPresenter>();
        builder.RegisterEntryPoint<ChestLootPresenter>();
    }

    private T EnsureComponent<T>(T field, string fieldName) where T : Component
    {
        if (field != null)
        {
            return field;
        }

        var found = GetComponentInChildren<T>(true);
        if (found != null)
        {
            return found;
        }

        throw new InvalidOperationException(
            $"UIScope requires a {typeof(T).Name} reference for '{fieldName}', but none was found in children.");
    }
}
