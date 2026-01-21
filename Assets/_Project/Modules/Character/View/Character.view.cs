using UnityEngine;

public class CharacterView : MonoBehaviour
{
    private Coroutine _moveRoutine;

    public void SetPosition(Vector3 cellPos)
    {
        transform.position = cellPos;
    }

    public void MoveToPosition(Vector3 targetPosition, float speed)
    {
        if (_moveRoutine != null)
        {
            StopCoroutine(_moveRoutine);
        }

        _moveRoutine = StartCoroutine(MoveRoutine(targetPosition, speed));
    }

    private System.Collections.IEnumerator MoveRoutine(Vector3 targetPosition, float speed)
    {
        Vector3 start = transform.position;
        float distance = Vector3.Distance(start, targetPosition);
        float duration = speed > 0f ? distance / speed : 0f;

        if (duration <= 0f)
        {
            transform.position = targetPosition;
            _moveRoutine = null;
            yield break;
        }

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            transform.position = Vector3.Lerp(start, targetPosition, t);
            yield return null;
        }

        transform.position = targetPosition;
        _moveRoutine = null;
    }
}
