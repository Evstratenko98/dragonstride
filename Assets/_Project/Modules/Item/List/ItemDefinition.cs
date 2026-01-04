using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "Configs/Item")]
public class ItemDefinition : ScriptableObject
{
    [Header("Базовая информация")]
    public string Id;           // Уникальный ID (например "potion_heal_small")
    public string DisplayName;  // Имя для UI
    [TextArea] public string Description;

    [Header("Тип и редкость")]
    public ItemType Type;
    public ItemRarity Rarity;

    [Header("Настройки предмета")]
    public bool Stackable;      // Можно ли стакать (зелья - да, меч - нет)
    public int MaxStack = 1;    // Максимальный стак

    [Header("Предметы для будущего UI/3D")]
    public Sprite Icon;
    public GameObject WorldPrefab; // Моделька в мире (на потом)
}