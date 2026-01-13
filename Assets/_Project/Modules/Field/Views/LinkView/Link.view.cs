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

        Vector3 p1 = start.position + Vector3.up * 0.05f;
        Vector3 p2 = end.position + Vector3.up * 0.05f;

        lr.SetPosition(0, p1);
        lr.SetPosition(1, p2);

        return lr;
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
