using UnityEngine;

public class CharacterClickHandler : MonoBehaviour
{
    private CharacterInstance _character;
    private IEventBus _eventBus;
    private Collider _collider;

    public void Initialize(CharacterInstance character, IEventBus eventBus)
    {
        _character = character;
        _eventBus = eventBus;
    }

    private void Awake()
    {
        EnsureCollider();
    }

    private void OnEnable()
    {
        EnsureCollider();
    }

    private void Update()
    {
        if (_character == null || _eventBus == null)
        {
            return;
        }

        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        var camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hitInfo, 500f))
        {
            return;
        }

        if (hitInfo.collider != null && (hitInfo.collider == _collider || hitInfo.collider.transform.IsChildOf(transform)))
        {
            _eventBus.Publish(new CharacterClicked(_character));
        }
    }

    private void OnMouseDown()
    {
        if (_character == null || _eventBus == null)
        {
            return;
        }

        _eventBus.Publish(new CharacterClicked(_character));
    }

    private void EnsureCollider()
    {
        if (_collider != null)
        {
            return;
        }

        _collider = GetComponent<Collider>();
        if (_collider != null)
        {
            return;
        }

        var renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        var boxCollider = gameObject.AddComponent<BoxCollider>();
        Vector3 localCenter = transform.InverseTransformPoint(bounds.center);
        boxCollider.center = localCenter;
        boxCollider.size = bounds.size;
        _collider = boxCollider;
    }
}
