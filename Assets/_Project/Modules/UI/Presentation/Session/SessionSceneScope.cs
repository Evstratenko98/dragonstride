using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public sealed class SessionSceneScope : LifetimeScope
{
    [SerializeField] private MainMenuView mainMenuView;
    [SerializeField] private LobbyView lobbyView;
    [SerializeField] private CharacterSelectView characterSelectView;
    [SerializeField] private GameOverSceneView gameOverSceneView;

    protected override void Configure(IContainerBuilder builder)
    {
        mainMenuView ??= GetComponentInChildren<MainMenuView>(true);
        lobbyView ??= GetComponentInChildren<LobbyView>(true);
        characterSelectView ??= GetComponentInChildren<CharacterSelectView>(true);
        gameOverSceneView ??= GetComponentInChildren<GameOverSceneView>(true);

        int presenterCount = 0;

        if (mainMenuView != null)
        {
            builder.RegisterComponent(mainMenuView);
            builder.RegisterEntryPoint<MainMenuPresenter>(Lifetime.Singleton).AsSelf();
            presenterCount++;
        }

        if (lobbyView != null)
        {
            builder.RegisterComponent(lobbyView);
            builder.RegisterEntryPoint<LobbyPresenter>(Lifetime.Singleton).AsSelf();
            presenterCount++;
        }

        if (characterSelectView != null)
        {
            builder.RegisterComponent(characterSelectView);
            builder.RegisterEntryPoint<CharacterSelectPresenter>(Lifetime.Singleton).AsSelf();
            presenterCount++;
        }

        if (gameOverSceneView != null)
        {
            builder.RegisterComponent(gameOverSceneView);
            builder.RegisterEntryPoint<GameOverScenePresenter>(Lifetime.Singleton).AsSelf();
            presenterCount++;
        }

        if (presenterCount == 0)
        {
            throw new InvalidOperationException(
                "[SessionSceneScope] Scene has no session view. Attach MainMenuView, LobbyView, CharacterSelectView, or GameOverSceneView.");
        }

        if (presenterCount > 1)
        {
            throw new InvalidOperationException(
                "[SessionSceneScope] Scene has multiple session views. Only one session screen presenter is allowed per scene.");
        }
    }

    protected override LifetimeScope FindParent()
    {
        LifetimeScope parent = AppScope.Instance;
        if (parent != null)
        {
            return parent;
        }

        throw new InvalidOperationException(
            "[SessionSceneScope] AppScope parent was not found. Ensure AppScopeRuntimeBootstrap is active before loading session scenes.");
    }
}
