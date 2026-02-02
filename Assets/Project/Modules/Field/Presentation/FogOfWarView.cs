using System.Collections.Generic;
using Project.Modules.Field.Domain;
using UnityEngine;

namespace Project.Modules.Field.Presentation;

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

        var renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            if (fogMaterial == null)
            {
                fogMaterial = renderer.sharedMaterial;
            }

            renderer.enabled = false;
        }
    }

    public void Build(FieldGrid field, float cellSize)
    {
        Clear();

        if (field == null || fogMaterial == null)
        {
            return;
        }

        foreach (var cell in field.GetAllCells())
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

            var tileRenderer = tile.GetComponent<Renderer>();
            tileRenderer.sharedMaterial = fogMaterial;
            _tiles[cell] = tileRenderer;
        }
    }

    public void Clear()
    {
        foreach (var tile in _tiles.Values)
        {
            if (tile != null)
            {
                Destroy(tile.gameObject);
            }
        }

        _tiles.Clear();
    }

    public void ApplyVisibility(Cell cell)
    {
        if (cell == null || !_tiles.TryGetValue(cell, out var renderer))
        {
            return;
        }

        switch (cell.VisibilityState)
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
