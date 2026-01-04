using UnityEngine;

public class CharacterView : MonoBehaviour
{
    public void SetPosition(Vector3 cellPos)
    {
        transform.position = cellPos;
    }
}
