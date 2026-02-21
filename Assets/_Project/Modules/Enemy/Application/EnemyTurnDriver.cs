using System;
using UnityEngine;
using VContainer.Unity;

public sealed class EnemyTurnDriver : IPostInitializable, ITickable, IDisposable
{
    private readonly IEventBus _eventBus;
    private readonly IRandomSource _randomSource;
    private readonly TurnFlow _turnFlow;
    private readonly CharacterRoster _characterRoster;
    private readonly CrownOwnershipService _crownOwnershipService;
    private readonly IMatchRuntimeRoleService _runtimeRoleService;

    private IDisposable _turnPhaseSubscription;
    private EnemyInstance _pendingEnemyTurn;

    public EnemyTurnDriver(
        IEventBus eventBus,
        IRandomSource randomSource,
        TurnFlow turnFlow,
        CharacterRoster characterRoster,
        CrownOwnershipService crownOwnershipService,
        IMatchRuntimeRoleService runtimeRoleService
    )
    {
        _eventBus = eventBus;
        _randomSource = randomSource;
        _turnFlow = turnFlow;
        _characterRoster = characterRoster;
        _crownOwnershipService = crownOwnershipService;
        _runtimeRoleService = runtimeRoleService;
    }

    public void PostInitialize()
    {
        _turnPhaseSubscription = _eventBus.Subscribe<TurnPhaseChanged>(OnTurnPhaseChanged);
    }

    public void Dispose()
    {
        _turnPhaseSubscription?.Dispose();
    }

    private void OnTurnPhaseChanged(TurnPhaseChanged message)
    {
        if (_runtimeRoleService != null && !_runtimeRoleService.CanMutateWorld)
        {
            return;
        }

        if (message.State != TurnState.ActionSelection)
        {
            return;
        }

        if (message.Actor is not EnemyInstance enemy)
        {
            return;
        }

        _pendingEnemyTurn = enemy;
    }

    public void Tick()
    {
        if (_runtimeRoleService != null && !_runtimeRoleService.CanMutateWorld)
        {
            return;
        }

        if (_pendingEnemyTurn == null)
        {
            return;
        }

        var enemy = _pendingEnemyTurn;
        _pendingEnemyTurn = null;
        PerformEnemyTurn(enemy);
    }

    private void PerformEnemyTurn(EnemyInstance enemy)
    {
        var model = enemy.EntityModel;
        if (model?.Behavior == null)
        {
            Debug.Log($"[EnemyTurn] {enemy?.Entity?.Name ?? "Unknown"} has no behavior. Turn ends.");
            _turnFlow.EndTurn();
            return;
        }

        Debug.Log($"[EnemyTurn] {enemy.Entity.Name} turn started.");
        model.Behavior.ExecuteTurn(enemy, _randomSource, _turnFlow, _characterRoster, _crownOwnershipService);
        Debug.Log($"[EnemyTurn] {enemy.Entity.Name} turn finished.");
    }
}
