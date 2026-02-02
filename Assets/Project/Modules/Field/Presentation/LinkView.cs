using System.Collections.Generic;
using UnityEngine;

public class LinkView : MonoBehaviour
{
    [SerializeField] private Material defaultMaterial;

    private readonly List<(Link model, LineRenderer renderer)> _visualLinks = new();

    public void CreateVisualLink(Link linkModel, CellView aView, CellView bView)
    {
        var lineRenderer = CreateLineRenderer(aView.transform, bView.transform);
        _visualLinks.Add((linkModel, lineRenderer));
    }

    private LineRenderer CreateLineRenderer(Transform start, Transform end)
    {
        GameObject lineObject = new GameObject("Link");
        lineObject.transform.SetParent(transform);

        var lineRenderer = lineObject.AddComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        lineRenderer.startWidth = 0.15f;
        lineRenderer.endWidth = 0.15f;
        lineRenderer.material = defaultMaterial;

        Vector3 p1 = GetEdgePoint(start, end.position) + Vector3.up * 0.05f;
        Vector3 p2 = GetEdgePoint(end, start.position) + Vector3.up * 0.05f;

        lineRenderer.SetPosition(0, p1);
        lineRenderer.SetPosition(1, p2);

        return lineRenderer;
    }

    private static Vector3 GetEdgePoint(Transform from, Vector3 toward)
    {
        var collider = from.GetComponentInChildren<Collider>();
        if (collider != null)
        {
            return collider.ClosestPoint(toward);
        }

        var collider2D = from.GetComponentInChildren<Collider2D>();
        if (collider2D != null)
        {
            return collider2D.ClosestPoint(toward);
        }

        var renderer = from.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            return renderer.bounds.ClosestPoint(toward);
        }

        return from.position;
    }

    public void ClearLinks()
    {
        foreach (var (_, renderer) in _visualLinks)
        {
            if (renderer != null)
            {
                Destroy(renderer.gameObject);
            }
        }

        _visualLinks.Clear();
    }
}
