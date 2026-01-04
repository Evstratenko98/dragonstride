using UnityEngine;

public interface ILinkView
{
    void CreateVisualLink(ILinkModel linkModel, ICellView aView, ICellView bView);

    void ClearLinks();
}
