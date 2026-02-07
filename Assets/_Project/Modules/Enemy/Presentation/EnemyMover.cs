using UnityEngine;
using System.Collections.Generic;

public sealed class EnemyMover : MonoBehaviour
{
    private Coroutine _moveRoutine;
    private readonly Queue<MovementRequest> _queue = new();

    private readonly struct MovementRequest
    {
        public Vector3 TargetPosition { get; }
        public float Speed { get; }

        public MovementRequest(Vector3 targetPosition, float speed)
        {
            TargetPosition = targetPosition;
            Speed = speed;
        }
    }

    public void MoveToPosition(Vector3 targetPosition, float speed)
    {
        _queue.Enqueue(new MovementRequest(targetPosition, speed));

        if (_moveRoutine == null)
        {
            _moveRoutine = StartCoroutine(ProcessQueueRoutine());
        }
    }

    private System.Collections.IEnumerator ProcessQueueRoutine()
    {
        while (_queue.Count > 0)
        {
            MovementRequest request = _queue.Dequeue();
            yield return MoveRoutine(request.TargetPosition, request.Speed);
        }

        _moveRoutine = null;
    }

    private System.Collections.IEnumerator MoveRoutine(Vector3 targetPosition, float speed)
    {
        Vector3 start = transform.position;
        float distance = Vector3.Distance(start, targetPosition);
        float duration = speed > 0f ? distance / speed : 0f;

        if (duration > 0f)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.position = Vector3.Lerp(start, targetPosition, t);
                yield return null;
            }
        }

        transform.position = targetPosition;
    }
}
