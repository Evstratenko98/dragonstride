using UnityEngine;

public class CharacterClickHandler : MonoBehaviour
{
    private CharacterInstance _character;
    private IEventBus _eventBus;

    public void Initialize(CharacterInstance character, IEventBus eventBus)
    {
        _character = character;
        _eventBus = eventBus;
    }

    private void OnMouseDown()
    {
        if (_character == null || _eventBus == null)
        {
            return;
        }

        _eventBus.Publish(new CharacterClicked(_character));
    }
}
