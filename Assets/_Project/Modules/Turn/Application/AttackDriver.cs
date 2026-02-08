using System;
using UnityEngine;
using VContainer.Unity;

public class AttackDriver : IPostInitializable, IDisposable
{
    private readonly IEventBus _eventBus;
    private readonly IRandomSource _randomSource;
    private readonly TurnFlow _turnFlow;
    private readonly CharacterRoster _characterRoster;
    private readonly EnemySpawner _enemySpawner;
    private readonly CrownOwnershipService _crownOwnershipService;

    private IDisposable _attackRequestedSubscription;
    private IDisposable _entityClickedSubscription;
    private IDisposable _turnPhaseSubscription;
    private IDisposable _turnEndedSubscription;

    private ICellLayoutOccupant _currentActor;
    private bool _awaitingTarget;

    public AttackDriver(
        IEventBus eventBus,
        IRandomSource randomSource,
        TurnFlow turnFlow,
        CharacterRoster characterRoster,
        EnemySpawner enemySpawner,
        CrownOwnershipService crownOwnershipService)
    {
        _eventBus = eventBus;
        _randomSource = randomSource;
        _turnFlow = turnFlow;
        _characterRoster = characterRoster;
        _enemySpawner = enemySpawner;
        _crownOwnershipService = crownOwnershipService;
    }

    public void PostInitialize()
    {
        _attackRequestedSubscription = _eventBus.Subscribe<AttackRequested>(OnAttackRequested);
        _entityClickedSubscription = _eventBus.Subscribe<EntityClicked>(OnEntityClicked);
        _turnPhaseSubscription = _eventBus.Subscribe<TurnPhaseChanged>(OnTurnPhaseChanged);
        _turnEndedSubscription = _eventBus.Subscribe<TurnEnded>(OnTurnEnded);
    }

    public void Dispose()
    {
        _attackRequestedSubscription?.Dispose();
        _entityClickedSubscription?.Dispose();
        _turnPhaseSubscription?.Dispose();
        _turnEndedSubscription?.Dispose();
    }

    private void OnTurnPhaseChanged(TurnPhaseChanged msg)
    {
        if (msg.Actor != _currentActor)
        {
            _awaitingTarget = false;
        }

        _currentActor = msg.Actor;

        if (msg.State == TurnState.End || msg.State == TurnState.None)
        {
            _awaitingTarget = false;
        }
    }

    private void OnTurnEnded(TurnEnded msg)
    {
        _awaitingTarget = false;
        _currentActor = null;
    }

    private void OnAttackRequested(AttackRequested msg)
    {
        if (_currentActor?.Entity == null)
        {
            return;
        }

        if (_turnFlow.State != TurnState.ActionSelection)
        {
            return;
        }

        _awaitingTarget = true;
    }

    private void OnEntityClicked(EntityClicked msg)
    {
        if (!_awaitingTarget)
        {
            return;
        }

        var target = msg.Occupant;
        Debug.Log($"[Attack] Target selection attempted: {DescribeActor(_currentActor)} -> {DescribeActor(target)}");
        if (!IsValidTarget(target))
        {
            return;
        }

        Debug.Log($"[Attack] Target selected: {DescribeActor(_currentActor)} -> {DescribeActor(target)}");
        _awaitingTarget = false;

        if (!_turnFlow.TryAttack())
        {
            return;
        }

        PerformAttack(_currentActor, target);
    }

    private bool IsValidTarget(ICellLayoutOccupant target)
    {
        if (target == null || _currentActor?.Entity == null)
        {
            return false;
        }

        if (target == _currentActor)
        {
            return false;
        }

        var attackerCell = _currentActor.Entity.CurrentCell;
        var defenderCell = target.Entity?.CurrentCell;
        if (attackerCell == null || defenderCell == null)
        {
            return false;
        }

        if (attackerCell.Type == CellType.Start || defenderCell.Type == CellType.Start)
        {
            Debug.Log($"[Attack] Attack is blocked on Start cell: {DescribeActor(_currentActor)} -> {DescribeActor(target)}.");
            return false;
        }

        return attackerCell == defenderCell;
    }

    private void PerformAttack(ICellLayoutOccupant attacker, ICellLayoutOccupant defender)
    {
        if (attacker?.Entity == null || defender?.Entity == null)
        {
            return;
        }

        Cell attackerCell = attacker.Entity.CurrentCell;
        Cell defenderCell = defender.Entity.CurrentCell;
        if (attackerCell?.Type == CellType.Start || defenderCell?.Type == CellType.Start)
        {
            Debug.Log($"[Attack] Attack canceled because Start cell is a safe zone: {DescribeActor(attacker)} -> {DescribeActor(defender)}.");
            return;
        }

        float dodgeRoll = _randomSource.Range(0, 100);
        float dodgeThreshold = defender.Entity.DodgeChance * 100f;
        if (dodgeRoll < dodgeThreshold)
        {
            Debug.Log($"[Attack] {DescribeActor(defender)} dodged the attack from {DescribeActor(attacker)}.");
            return;
        }

        int damage = Mathf.Max(0, attacker.Entity.Attack - defender.Entity.Armor);
        if (damage <= 0)
        {
            return;
        }

        int newHealth = Mathf.Max(0, defender.Entity.Health - damage);
        defender.Entity.SetHealth(newHealth);
        Debug.Log($"[Attack] {DescribeActor(defender)} took {damage} damage from {DescribeActor(attacker)}. Health: {newHealth}.");

        if (newHealth == 0 && defender is CharacterInstance character)
        {
            _crownOwnershipService.OnEntityKilled(attacker, defender);
            bool reborn = _characterRoster.TryRebirthCharacter(character);
            if (reborn)
            {
                Debug.Log($"[Attack] {DescribeActor(defender)} has been reborn at the start cell.");
            }
        }

        if (newHealth == 0 && defender is EnemyInstance enemy)
        {
            _crownOwnershipService.OnEntityKilled(attacker, defender);
            bool removed = _enemySpawner.RemoveEnemy(enemy);
            if (removed)
            {
                Debug.Log($"[Attack] {DescribeActor(defender)} has been removed from the game.");

                if (attacker is CharacterInstance characterAttacker)
                {
                    bool leveledUp = characterAttacker.Model.TryLevelUp();
                    if (leveledUp)
                    {
                        Debug.Log($"[Attack] {DescribeActor(attacker)} reached level {characterAttacker.Model.Level}.");
                    }
                }
            }
        }
    }

    private string DescribeActor(ICellLayoutOccupant actor)
    {
        return actor?.Entity?.Name ?? "Unknown";
    }
}
