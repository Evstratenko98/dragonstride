using System.Collections.Generic;
using UnityEngine;

public class LinkView : MonoBehaviour
{
    [SerializeField] private Material defaultMaterial;

    private readonly List<(Link model, LineRenderer lr)> _visualLinks = new();

    public void CreateVisualLink(Link linkModel, CellView aView, CellView bView)
    {
        LineRenderer lr = CreateLineRenderer(aView.transform, bView.transform);
        _visualLinks.Add((linkModel, lr));
    }

    private LineRenderer CreateLineRenderer(Transform start, Transform end)
    {
        var lineObj = new GameObject("Link");
        lineObj.transform.SetParent(transform);

        var lr = lineObj.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.useWorldSpace = true;
        lr.startWidth = 0.15f;
        lr.endWidth = 0.15f;
        lr.material = defaultMaterial;

        Vector3 p1 = GetEdgePoint(start, end.position) + Vector3.up * 0.05f;
        Vector3 p2 = GetEdgePoint(end, start.position) + Vector3.up * 0.05f;

        lr.SetPosition(0, p1);
        lr.SetPosition(1, p2);

        return lr;
    }

    private static Vector3 GetEdgePoint(Transform from, Vector3 toward)
    {
        Collider collider = from.GetComponentInChildren<Collider>();
        if (collider != null)
            return collider.ClosestPoint(toward);

        Collider2D collider2D = from.GetComponentInChildren<Collider2D>();
        if (collider2D != null)
            return collider2D.ClosestPoint(toward);

        Renderer renderer = from.GetComponentInChildren<Renderer>();
        if (renderer != null)
            return renderer.bounds.ClosestPoint(toward);

        return from.position;
    }

    public void ClearLinks()
    {
        foreach (var (_, lr) in _visualLinks)
        {
            if (lr != null)
                Destroy(lr.gameObject);
        }

        _visualLinks.Clear();
    }
}
