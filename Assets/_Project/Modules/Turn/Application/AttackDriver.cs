using System;
using UnityEngine;
using VContainer.Unity;

public class AttackDriver : IPostInitializable, IDisposable
{
    private readonly IEventBus _eventBus;
    private readonly IRandomSource _randomSource;
    private readonly TurnFlow _turnFlow;

    private IDisposable _attackRequestedSubscription;
    private IDisposable _characterClickedSubscription;
    private IDisposable _turnPhaseSubscription;
    private IDisposable _turnEndedSubscription;

    private CharacterInstance _currentCharacter;
    private bool _awaitingTarget;

    public AttackDriver(IEventBus eventBus, IRandomSource randomSource, TurnFlow turnFlow)
    {
        _eventBus = eventBus;
        _randomSource = randomSource;
        _turnFlow = turnFlow;
    }

    public void PostInitialize()
    {
        _attackRequestedSubscription = _eventBus.Subscribe<AttackRequested>(OnAttackRequested);
        _characterClickedSubscription = _eventBus.Subscribe<CharacterClicked>(OnCharacterClicked);
        _turnPhaseSubscription = _eventBus.Subscribe<TurnPhaseChanged>(OnTurnPhaseChanged);
        _turnEndedSubscription = _eventBus.Subscribe<TurnEnded>(OnTurnEnded);
    }

    public void Dispose()
    {
        _attackRequestedSubscription?.Dispose();
        _characterClickedSubscription?.Dispose();
        _turnPhaseSubscription?.Dispose();
        _turnEndedSubscription?.Dispose();
    }

    private void OnTurnPhaseChanged(TurnPhaseChanged msg)
    {
        if (msg.Character != _currentCharacter)
        {
            _awaitingTarget = false;
        }

        _currentCharacter = msg.Character;

        if (msg.State == TurnState.End || msg.State == TurnState.None)
        {
            _awaitingTarget = false;
        }
    }

    private void OnTurnEnded(TurnEnded msg)
    {
        _awaitingTarget = false;
        _currentCharacter = null;
    }

    private void OnAttackRequested(AttackRequested msg)
    {
        if (_currentCharacter == null)
        {
            return;
        }

        if (_turnFlow.State != TurnState.ActionSelection)
        {
            return;
        }

        _awaitingTarget = true;
    }

    private void OnCharacterClicked(CharacterClicked msg)
    {
        if (!_awaitingTarget)
        {
            return;
        }

        var target = msg.Character;
        Debug.Log($"[Attack] Target selection attempted: {DescribeCharacter(_currentCharacter)} -> {DescribeCharacter(target)}");
        if (!IsValidTarget(target))
        {
            return;
        }

        Debug.Log($"[Attack] Target selected: {DescribeCharacter(_currentCharacter)} -> {DescribeCharacter(target)}");
        _awaitingTarget = false;

        if (!_turnFlow.TryAttack())
        {
            return;
        }

        PerformAttack(_currentCharacter, target);
    }

    private bool IsValidTarget(CharacterInstance target)
    {
        if (target == null || _currentCharacter == null)
        {
            return false;
        }

        if (target == _currentCharacter)
        {
            return false;
        }

        var attackerCell = _currentCharacter.Model?.CurrentCell;
        var defenderCell = target.Model?.CurrentCell;
        if (attackerCell == null || defenderCell == null)
        {
            return false;
        }

        return attackerCell == defenderCell;
    }

    private void PerformAttack(CharacterInstance attacker, CharacterInstance defender)
    {
        if (attacker?.Model == null || defender?.Model == null)
        {
            return;
        }

        float dodgeRoll = _randomSource.Range(0, 100);
        float dodgeThreshold = defender.Model.DodgeChance * 100f;
        if (dodgeRoll < dodgeThreshold)
        {
            Debug.Log($"[Attack] {DescribeCharacter(defender)} dodged the attack from {DescribeCharacter(attacker)}.");
            return;
        }

        int damage = Mathf.Max(0, attacker.Model.Attack - defender.Model.Armor);
        if (damage <= 0)
        {
            return;
        }

        int newHealth = Mathf.Max(0, defender.Model.Health - damage);
        defender.Model.SetHealth(newHealth);
        Debug.Log($"[Attack] {DescribeCharacter(defender)} took {damage} damage from {DescribeCharacter(attacker)}. Health: {newHealth}.");
    }

    private string DescribeCharacter(CharacterInstance character)
    {
        return character?.Name ?? "Unknown";
    }
}
