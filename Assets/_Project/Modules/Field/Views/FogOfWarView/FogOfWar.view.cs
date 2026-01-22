using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class FogOfWarView : MonoBehaviour
{
    [SerializeField] private float heightOffset = 5f;
    [SerializeField] private Color32 unseenColor = new Color32(0, 0, 0, 255);
    [SerializeField] private Color32 seenColor = new Color32(0, 0, 0, 160);
    [SerializeField] private Color32 visibleColor = new Color32(0, 0, 0, 0);

    private Renderer _renderer;
    private Texture2D _texture;
    private int _width;
    private int _height;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
    }

    public void Initialize(FieldService fieldService, float cellSize)
    {
        if (fieldService == null)
            return;

        if (_renderer == null)
            _renderer = GetComponent<Renderer>();

        if (_renderer == null)
        {
            Debug.LogWarning("[FogOfWarView] Renderer is missing.");
            return;
        }

        _width = fieldService.Width;
        _height = fieldService.Height;

        if (_texture == null || _texture.width != _width || _texture.height != _height)
        {
            _texture = new Texture2D(_width, _height, TextureFormat.ARGB32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            _renderer.material.mainTexture = _texture;
        }

        UpdateTransform(cellSize);
    }

    public void Render(FieldService fieldService)
    {
        if (fieldService == null || _texture == null)
            return;

        foreach (var cell in fieldService.GetAllCells())
        {
            _texture.SetPixel(cell.X, cell.Y, GetColor(cell.FogState));
        }

        _texture.Apply();
    }

    private void UpdateTransform(float cellSize)
    {
        transform.localPosition = new Vector3(0, heightOffset, 0);
        transform.localScale = new Vector3(_width * cellSize + 10f, _height * cellSize + 10f, 1f);
    }

    private Color32 GetColor(FogVisibilityState state)
    {
        return state switch
        {
            FogVisibilityState.Unseen => unseenColor,
            FogVisibilityState.Seen => seenColor,
            FogVisibilityState.Visible => visibleColor,
            _ => unseenColor
        };
    }
}
