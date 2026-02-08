using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "Configs/Item")]
public class ItemDefinition : ScriptableObject
{
    [Header("Базовая информация")]
    public string Id;
    public string DisplayName;
    [TextArea] public string Description;

    [Header("Тип и редкость")]
    public ItemType Type;
    public ItemRarity Rarity;

    [Header("Настройки предмета")]
    public bool Stackable;
    public int MaxStack = 1;

    [Header("Модификаторы характеристик")]
    public int HealthModifier;
    public int AttackModifier;
    public int ArmorModifier;
    public float DodgeModifier;
    public int InitiativeModifier;
    public int SpeedModifier;
    public float LuckModifier;

    [Header("Эффекты расходуемых предметов")]
    public int HealAmount;

    [Header("Предметы для будущего UI/3D")]
    public Sprite Icon;
    public GameObject WorldPrefab;
}
