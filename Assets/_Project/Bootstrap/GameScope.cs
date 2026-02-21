using System;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;
using VContainer.Unity;

public class GameScope : LifetimeScope
{   
    [Header("Configs")]
    [SerializeField] private ConfigScriptableObject _config;
    [SerializeField] private ItemConfig _itemConfig;

    [SerializeField] private CellView cellViewPrefab;
    [SerializeField] private LinkView linkViewPrefab;
    [SerializeField] private CellColorTheme colorTheme;
    [SerializeField] private FogOfWarView fogOfWarViewPrefab;

    [SerializeField] private CharacterView[] characterPrefabs;
    [FormerlySerializedAs("slimePrefab")]
    [SerializeField] private GameObject slimePrefab;
    [SerializeField] private GameObject wolfPrefab;
    [SerializeField] private GameObject bossPrefab;

    [Header("Enemy Spawn Chances (%)")]
    [SerializeField] private int slimeSpawnChancePercent = 60;
    [SerializeField] private int wolfSpawnChancePercent = 40;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterInstance(_config);
        builder.RegisterInstance(_itemConfig);
        
        builder.RegisterInstance(colorTheme);
        builder.RegisterComponent(cellViewPrefab);
        builder.RegisterInstance(linkViewPrefab);
        builder.RegisterInstance(fogOfWarViewPrefab);
        builder.Register<FieldRoot>(Lifetime.Singleton);
        builder.Register<FieldViewFactory>(Lifetime.Singleton);
        builder.Register<FieldState>(Lifetime.Singleton);
        builder.Register<FieldGenerator>(Lifetime.Singleton);
        builder.Register<IFieldSnapshotService, FieldSnapshotService>(Lifetime.Singleton);

        builder.RegisterInstance(characterPrefabs).As<CharacterView[]>();
        builder.Register<IMatchRuntimeRoleService, MatchRuntimeRoleService>(Lifetime.Singleton);
        builder.Register<IMatchClientTurnStateService, FallbackMatchClientTurnStateService>(Lifetime.Singleton);
        builder.Register<IGameCommandPolicyService, GameCommandPolicyService>(Lifetime.Singleton);
        builder.Register<IActorIdentityService, ActorIdentityService>(Lifetime.Singleton);
        builder.Register<IInventorySnapshotService, InventorySnapshotService>(Lifetime.Singleton);
        builder.Register<ILootSyncService, LootSyncService>(Lifetime.Singleton);
        builder.Register<IClientActionPlaybackService, ClientActionPlaybackService>(Lifetime.Singleton);
        builder.Register<IMatchActionTimelineService, MatchActionTimelineService>(Lifetime.Singleton);
        builder.Register<CharacterInputReader>(Lifetime.Singleton);
        builder.Register<CharacterFactory>(Lifetime.Singleton);
        builder.Register<CharacterLifecycleService>(Lifetime.Singleton);
        builder.Register<EntityLayout>(Lifetime.Singleton);
        builder.Register<CharacterRoster>(Lifetime.Singleton);
        builder.RegisterInstance(new EnemyPrefabs(
            slimePrefab,
            wolfPrefab,
            bossPrefab,
            slimeSpawnChancePercent,
            wolfSpawnChancePercent));
        builder.Register<EnemySpawner>(Lifetime.Singleton);
        builder.RegisterEntryPoint<EnemyTurnDriver>(Lifetime.Singleton).AsSelf();
        builder.Register<CellOpenService>(Lifetime.Singleton);
        builder.Register<CommonCellOpenHandler>(Lifetime.Singleton).As<ICellOpenHandler>();
        builder.Register<StartCellOpenHandler>(Lifetime.Singleton).As<ICellOpenHandler>();
        builder.Register<BossCellOpenHandler>(Lifetime.Singleton).As<ICellOpenHandler>();
        builder.Register<LootCellOpenHandler>(Lifetime.Singleton).As<ICellOpenHandler>();
        builder.Register<FightCellOpenHandler>(Lifetime.Singleton).As<ICellOpenHandler>();
        builder.Register<TeleportCellOpenHandler>(Lifetime.Singleton).As<ICellOpenHandler>();

        builder.Register<ItemFactory>(Lifetime.Singleton);
        builder.Register<ConsumableItemUseService>(Lifetime.Singleton);
        builder.Register<CrownOwnershipService>(Lifetime.Singleton);

        builder.Register<IRandomSource, UnityRandomSource>(Lifetime.Singleton);
        builder.Register<IGameCommandExecutionService, GameCommandExecutionService>(Lifetime.Singleton);
        builder.Register<IMatchPauseService, MatchPauseService>(Lifetime.Singleton);
        builder.Register<ITurnAuthorityService, TurnAuthorityService>(Lifetime.Singleton);
        builder.Register<IMatchStatePublisher, MatchStatePublisher>(Lifetime.Singleton);
        builder.Register<IMatchStateApplier, MatchStateApplier>(Lifetime.Singleton);
        builder.Register<MpsGameCommandGateway>(Lifetime.Singleton);
        builder.Register<IGameCommandGateway, GameCommandGatewayFacade>(Lifetime.Singleton);
        builder.Register<TurnActorRegistry>(Lifetime.Singleton);
        builder.Register<FieldPresenter>(Lifetime.Singleton);
        builder.RegisterEntryPoint<FogOfWarPresenter>(Lifetime.Singleton).AsSelf();
        builder.RegisterEntryPoint<MpsGameCommandGatewayRunner>(Lifetime.Singleton).AsSelf();
        builder.RegisterEntryPoint<OnlineInputCommandForwarder>(Lifetime.Singleton).AsSelf();
        builder.RegisterEntryPoint<HostDisconnectRecoveryEntryPoint>(Lifetime.Singleton).AsSelf();
        builder.RegisterEntryPoint<DisconnectGraceDriver>(Lifetime.Singleton).AsSelf();
        builder.RegisterEntryPoint<CharacterMovementDriver>(Lifetime.Singleton).AsSelf();
        builder.RegisterEntryPoint<CellOpenDriver>(Lifetime.Singleton).AsSelf();
        builder.RegisterEntryPoint<ActionPanelAvailabilityDriver>(Lifetime.Singleton).AsSelf();
        builder.RegisterEntryPoint<AttackDriver>(Lifetime.Singleton).AsSelf();
        builder.RegisterEntryPoint<TurnFlow>(Lifetime.Singleton).AsSelf();
        builder.RegisterEntryPoint<GameFlow>(Lifetime.Singleton).AsSelf();
    }

    protected override LifetimeScope FindParent()
    {
        LifetimeScope parent = AppScope.Instance;
        if (parent != null)
        {
            return parent;
        }

        throw new InvalidOperationException(
            "[GameScope] AppScope parent was not found. Ensure AppScopeRuntimeBootstrap is active and AppScope exists before loading GameScene.");
    }

    // Local fallback keeps GameScope resilient to accidental file reverts.
    private sealed class FallbackMatchClientTurnStateService : IMatchClientTurnStateService
    {
        public bool HasInitialState { get; private set; }
        public bool IsLocalTurn { get; private set; }
        public TurnState CurrentTurnState { get; private set; } = TurnState.None;
        public string CurrentOwnerPlayerId { get; private set; } = string.Empty;

        public void UpdateFromSnapshot(MatchStateSnapshot snapshot, string localPlayerId)
        {
            CurrentTurnState = snapshot.TurnState;
            CurrentOwnerPlayerId = ResolveOwnerPlayerId(snapshot);
            IsLocalTurn = IsTurnBoundActionState(snapshot.TurnState) &&
                          !string.IsNullOrWhiteSpace(localPlayerId) &&
                          string.Equals(CurrentOwnerPlayerId, localPlayerId, StringComparison.Ordinal);
            HasInitialState = true;
        }

        public void Reset()
        {
            HasInitialState = false;
            IsLocalTurn = false;
            CurrentTurnState = TurnState.None;
            CurrentOwnerPlayerId = string.Empty;
        }

        private static string ResolveOwnerPlayerId(MatchStateSnapshot snapshot)
        {
            if (snapshot.Actors == null || snapshot.CurrentActorId <= 0)
            {
                return string.Empty;
            }

            for (int i = 0; i < snapshot.Actors.Count; i++)
            {
                ActorStateSnapshot actor = snapshot.Actors[i];
                if (actor.ActorId != snapshot.CurrentActorId)
                {
                    continue;
                }

                return actor.OwnerPlayerId ?? string.Empty;
            }

            return string.Empty;
        }

        private static bool IsTurnBoundActionState(TurnState state)
        {
            return state == TurnState.ActionSelection ||
                   state == TurnState.Movement ||
                   state == TurnState.Attack ||
                   state == TurnState.OpenCell ||
                   state == TurnState.Trade;
        }
    }
}
