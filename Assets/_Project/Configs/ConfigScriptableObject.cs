using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "GameConfig", menuName = "Configs/Game Config")]
public class ConfigScriptableObject : ScriptableObject
{
    [Header("Field settings")]
    [FormerlySerializedAs("CELL_SIZE")]
    public float CELL_DISTANCE = 20f;
    public int FIELD_WIDTH = 15;
    public int FIELD_HEIGHT = 15;
    public float EXTRA_CONNECTION_CHANCE = 0.2f;

    [Header("Characters")]
    [FormerlySerializedAs("CHARACTER_SPEED")]
    public float ENTITY_SPEED = 2.5f;
    public float CHARACTER_HEIGHT = 1.1f;
    [FormerlySerializedAs("CHARACTER_LAYOUT_SPEED")]
    public float ENTITY_LAYOUT_SPEED = 12f;
    [FormerlySerializedAs("CHARACTER_LAYOUT_RADIUS")]
    public float ENTITY_LAYOUT_RADIUS = 1.5f;

    [Header("UI")]
    public int INVENTORY_CAPACITY = 30;

    [Header("Camera")]
    public float CAMERA_PAN_SPEED = 12f;
    public float CAMERA_EDGE_THRESHOLD = 16f;
    public float CAMERA_ZOOM_SPEED = 120f;
    public float CAMERA_ZOOM_MIN_DISTANCE = 10f;
    public float CAMERA_ZOOM_MAX_DISTANCE = 35f;

    public float CellDistance => Mathf.Max(0.01f, CELL_DISTANCE);
}
