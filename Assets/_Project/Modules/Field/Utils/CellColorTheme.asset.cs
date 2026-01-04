using UnityEngine;

[CreateAssetMenu(
    fileName = "CellColorTheme",
    menuName = "Field/Cell Color Theme")]
public class CellColorTheme : ScriptableObject
{
    public Material startMaterial;
    public Material commonMaterial;
    public Material endMaterial;
}
