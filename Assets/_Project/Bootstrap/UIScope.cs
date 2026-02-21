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
    [SerializeField] private CharacterRosterPanelView characterRosterPanelView;
    
    protected override void Configure(IContainerBuilder builder)
    {
        if (config == null)
        {
            throw new InvalidOperationException($"{nameof(UIScope)} requires a {nameof(ConfigScriptableObject)} reference.");
        }

        builder.RegisterInstance(config);
        builder.RegisterComponent(EnsureComponent(gameScreenView, nameof(gameScreenView)));
        builder.RegisterComponent(EnsureComponent(finishScreenView, nameof(finishScreenView)));
        UIScreenView resolvedScreenView = EnsureComponent(screenView, nameof(screenView));
        builder.RegisterComponent(resolvedScreenView);
        CharacterScreenView resolvedCharacterScreenView = EnsureComponent(characterScreenView, nameof(characterScreenView));
        ChestLootView resolvedChestLootView = EnsureComponent(chestLootView, nameof(chestLootView));
        builder.RegisterComponent(resolvedCharacterScreenView);
        builder.RegisterComponent(resolvedChestLootView);
        builder.RegisterComponent(EnsureComponent(characterRosterPanelView, nameof(characterRosterPanelView)));

        // Fail-safe baseline for GameScene startup: extra windows start hidden even if presenters init late.
        resolvedScreenView.ShowGameScreen();

        if (resolvedCharacterScreenView.gameObject.activeSelf)
        {
            resolvedCharacterScreenView.Hide();
        }

        if (resolvedChestLootView.gameObject.activeSelf)
        {
            resolvedChestLootView.Hide();
        }

        builder.RegisterEntryPoint<GameScreenPresenter>();
        builder.RegisterEntryPoint<FinishScreenPresenter>();
        builder.RegisterEntryPoint<UIScreenPresenter>();
        builder.RegisterEntryPoint<CharacterScreenPresenter>();
        builder.RegisterEntryPoint<ChestLootPresenter>();
        builder.RegisterEntryPoint<CharacterRosterPanelPresenter>();
    }

    protected override LifetimeScope FindParent()
    {
        LifetimeScope parent = AppScope.Instance;
        if (parent != null)
        {
            return parent;
        }

        throw new InvalidOperationException(
            "[UIScope] AppScope parent was not found. Ensure AppScopeRuntimeBootstrap is active and AppScope exists before loading GameScene.");
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
