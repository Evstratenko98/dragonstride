using System.Collections.Generic;
using UnityEngine;

public class FogOfWarView : MonoBehaviour
{
    [SerializeField] private Material fogMaterial;
    [SerializeField, Range(0f, 1f)] private float seenAlpha = 0.55f;
    [SerializeField, Range(0f, 1f)] private float unseenAlpha = 1f;
    [SerializeField] private float heightOffset = 0.5f;

    private readonly Dictionary<Cell, Renderer> _tiles = new();
    private MaterialPropertyBlock _propertyBlock;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private void Awake()
    {
        _propertyBlock = new MaterialPropertyBlock();

        if (fogMaterial == null)
        {
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                fogMaterial = renderer.sharedMaterial;
                renderer.enabled = false;
            }
        }
        else
        {
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }
    }

    public void Build(FieldMap fieldMap, float cellSize)
    {
        Clear();

        if (fieldMap == null || fogMaterial == null)
            return;

        foreach (var cell in fieldMap.GetAllCells())
        {
            var tile = GameObject.CreatePrimitive(PrimitiveType.Quad);
            tile.name = $"Fog_{cell.X}_{cell.Y}";
            tile.transform.SetParent(transform, false);
            tile.transform.localPosition = new Vector3(cell.X * cellSize, heightOffset, cell.Y * cellSize);
            tile.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            tile.transform.localScale = Vector3.one * cellSize;

            if (tile.TryGetComponent<Collider>(out var collider))
            {
                Destroy(collider);
            }

            var renderer = tile.GetComponent<Renderer>();
            renderer.sharedMaterial = fogMaterial;
            _tiles[cell] = renderer;
        }
    }

    public void Clear()
    {
        foreach (var tile in _tiles.Values)
        {
            if (tile != null)
                Destroy(tile.gameObject);
        }

        _tiles.Clear();
    }

    public void ApplyVisibility(Cell cell)
    {
        if (cell == null || !_tiles.TryGetValue(cell, out var renderer))
            return;

        switch (cell.Visibility)
        {
            case CellVisibility.Visible:
                renderer.enabled = false;
                break;
            case CellVisibility.Seen:
                renderer.enabled = true;
                SetTileAlpha(renderer, seenAlpha);
                break;
            case CellVisibility.Unseen:
                renderer.enabled = true;
                SetTileAlpha(renderer, unseenAlpha);
                break;
        }
    }

    private void SetTileAlpha(Renderer renderer, float alpha)
    {
        var color = new Color(0f, 0f, 0f, alpha);
        renderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetColor(BaseColorId, color);
        _propertyBlock.SetColor(ColorId, color);
        renderer.SetPropertyBlock(_propertyBlock);
    }
}
