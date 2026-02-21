using System;
using UnityEngine;

[Serializable]
public sealed class CharacterDefinition
{
    [SerializeField] private string id = "hero_01";
    [SerializeField] private string displayName = "Hero";
    [SerializeField] private int prefabIndex;

    [Header("Stat modifiers")]
    [SerializeField] private int healthBonus;
    [SerializeField] private int attackBonus;
    [SerializeField] private int armorBonus;
    [SerializeField] private int initiativeBonus;
    [SerializeField] private int speedBonus;
    [SerializeField] private float dodgeBonus;
    [SerializeField] private float luckBonus;

    public string Id => string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? "Hero" : displayName.Trim();
    public int PrefabIndex => Mathf.Max(0, prefabIndex);

    public int HealthBonus => healthBonus;
    public int AttackBonus => attackBonus;
    public int ArmorBonus => armorBonus;
    public int InitiativeBonus => initiativeBonus;
    public int SpeedBonus => speedBonus;
    public float DodgeBonus => dodgeBonus;
    public float LuckBonus => luckBonus;

    public bool HasValidId => !string.IsNullOrWhiteSpace(Id);

    public void ApplyTo(Character model)
    {
        if (model == null)
        {
            return;
        }

        if (healthBonus != 0)
        {
            model.AddHealth(healthBonus);
        }

        if (attackBonus != 0)
        {
            model.AddAttack(attackBonus);
        }

        if (armorBonus != 0)
        {
            model.AddArmor(armorBonus);
        }

        if (initiativeBonus != 0)
        {
            model.AddInitiative(initiativeBonus);
        }

        if (speedBonus != 0)
        {
            model.AddSpeed(speedBonus);
        }

        if (Mathf.Abs(dodgeBonus) > Mathf.Epsilon)
        {
            model.AddDodge(dodgeBonus);
        }

        if (Mathf.Abs(luckBonus) > Mathf.Epsilon)
        {
            model.AddLuck(luckBonus);
        }
    }
}
