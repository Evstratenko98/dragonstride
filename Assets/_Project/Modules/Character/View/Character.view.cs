using UnityEngine;

public class CharacterView : MonoBehaviour
{
    public void SetPosition(Vector3 cellPos)
    {
        transform.position = cellPos;
    }

    public void SetPositionAndScale(Vector3 position, float scale)
    {
        transform.position = position;
        transform.localScale = Vector3.one * scale;
    }
}
