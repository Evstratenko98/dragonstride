using UnityEngine;
using System.Collections.Generic;

public class LinkView : MonoBehaviour
{
    [SerializeField] private Material defaultMaterial;

    private readonly List<(LinkModel model, LineRenderer lr)> _visualLinks = new();

    public void CreateVisualLink(LinkModel linkModel, CellView aView, CellView bView)
    {
        LineRenderer lr = CreateLineRenderer(aView.transform, bView.transform);
        _visualLinks.Add(((LinkModel)linkModel, lr));
    }

    private LineRenderer CreateLineRenderer(Transform start, Transform end)
    {
        GameObject lineObj = new GameObject("Link");
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
        if (!TryGetBounds(from, out Bounds bounds))
            return from.position;

        Vector3 center = bounds.center;
        Vector3 direction = toward - center;
        if (direction == Vector3.zero)
            return center;

        direction.Normalize();
        Vector3 extents = bounds.extents;

        float tx = Mathf.Abs(direction.x) > 0.0001f ? extents.x / Mathf.Abs(direction.x) : float.PositiveInfinity;
        float ty = Mathf.Abs(direction.y) > 0.0001f ? extents.y / Mathf.Abs(direction.y) : float.PositiveInfinity;
        float tz = Mathf.Abs(direction.z) > 0.0001f ? extents.z / Mathf.Abs(direction.z) : float.PositiveInfinity;
        float distance = Mathf.Min(tx, ty, tz);

        return center + direction * distance;
    }

    private static bool TryGetBounds(Transform target, out Bounds bounds)
    {
        Collider collider = target.GetComponentInChildren<Collider>();
        if (collider != null)
        {
            bounds = collider.bounds;
            return true;
        }

        Renderer renderer = target.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            bounds = renderer.bounds;
            return true;
        }

        bounds = new Bounds(target.position, Vector3.zero);
        return false;
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
