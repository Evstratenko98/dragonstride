using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(
    fileName = "CellColorTheme",
    menuName = "Field/Cell Color Theme")]
public class CellColorTheme : ScriptableObject
{
    public Material hiddenMaterial;
    public Material startMaterial;
    public Material commonMaterial;
    public Material lootMaterial;
    public Material fightMaterial;
    public Material teleportMaterial;
    [FormerlySerializedAs("endMaterial")]
    public Material bossMaterial;
}
