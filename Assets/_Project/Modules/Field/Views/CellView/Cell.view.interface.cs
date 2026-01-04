using UnityEngine;

public interface ICellView
{
    Transform transform { get; }

    void Bind(ICellModel model);
}
