using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public sealed class SessionSceneScope : LifetimeScope
{
    [SerializeField] private MainMenuView mainMenuView;
    [SerializeField] private LobbyView lobbyView;
    [SerializeField] private GameOverSceneView gameOverSceneView;

    protected override void Configure(IContainerBuilder builder)
    {
        mainMenuView ??= GetComponentInChildren<MainMenuView>(true);
        lobbyView ??= GetComponentInChildren<LobbyView>(true);
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

        if (gameOverSceneView != null)
        {
            builder.RegisterComponent(gameOverSceneView);
            builder.RegisterEntryPoint<GameOverScenePresenter>(Lifetime.Singleton).AsSelf();
            presenterCount++;
        }

        if (presenterCount == 0)
        {
            throw new InvalidOperationException(
                "[SessionSceneScope] Scene has no session view. Attach MainMenuView, LobbyView, or GameOverSceneView.");
        }

        if (presenterCount > 1)
        {
            throw new InvalidOperationException(
                "[SessionSceneScope] Scene has multiple session views. Only one session screen presenter is allowed per scene.");
        }
    }
}
