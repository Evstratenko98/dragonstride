using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemConfig", menuName = "Configs/Item Config")]
public class ItemConfig : ScriptableObject
{
    [Header("Список всех предметов в игре")]
    public List<ItemDefinition> AllItems = new List<ItemDefinition>();

    [Header("Шансы выпадения по редкости из сундука")]
    public List<RarityDropSettings> ChestDropTable = new List<RarityDropSettings>();
}

[System.Serializable]
public class RarityDropSettings
{
    public ItemRarity Rarity;
    public float Weight; // относительный вес (например: Common=80, Rare=15, Unique=5)
}