using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "Configs/Game Config")]
public class ConfigScriptableObject : ScriptableObject
{
    [Header("Field settings")]
    public float CELL_SIZE = 20f;
    public int FIELD_WIDTH = 15;
    public int FIELD_HEIGHT = 15;
    public float EXTRA_CONNECTION_CHANCE = 0.2f;

    [Header("Characters")]
    public float CHARACTER_SPEED = 2.5f;
    public float CHARACTER_HEIGHT = 1.1f;
    public float CHARACTER_LAYOUT_SPEED = 12f;

    [Header("UI")]
    public int INVENTORY_CAPACITY = 30;

    [Header("Camera")]
    public float CAMERA_PAN_SPEED = 12f;
    public float CAMERA_EDGE_THRESHOLD = 16f;
}
