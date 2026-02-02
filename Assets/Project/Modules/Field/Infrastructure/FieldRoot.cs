using UnityEngine;

public sealed class FieldRoot
{
    public Transform Root { get; private set; }

    public Transform EnsureRoot()
    {
        if (Root == null)
        {
            var rootObject = new GameObject("Field");
            Root = rootObject.transform;
        }

        return Root;
    }
}
