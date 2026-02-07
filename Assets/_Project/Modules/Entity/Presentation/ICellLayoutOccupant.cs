using UnityEngine;

public interface ICellLayoutOccupant
{
    Entity Entity { get; }
    void MoveToPosition(Vector3 targetPosition, float speed);
    void SetWorldVisible(bool isVisible);
}

public readonly struct EntityClicked
{
    public ICellLayoutOccupant Occupant { get; }

    public EntityClicked(ICellLayoutOccupant occupant)
    {
        Occupant = occupant;
    }
}

public class EntityClickHandler : MonoBehaviour
{
    private ICellLayoutOccupant _occupant;
    private IEventBus _eventBus;

    public void Initialize(ICellLayoutOccupant occupant, IEventBus eventBus)
    {
        _occupant = occupant;
        _eventBus = eventBus;
    }

    private void OnMouseDown()
    {
        if (_occupant == null || _eventBus == null)
        {
            return;
        }

        _eventBus.Publish(new EntityClicked(_occupant));
    }
}
