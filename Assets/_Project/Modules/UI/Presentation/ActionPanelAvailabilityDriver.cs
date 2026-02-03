using System;
using VContainer.Unity;

public class ActionPanelAvailabilityDriver : IPostInitializable, IDisposable
{
    private readonly CharacterRoster _characterRoster;
    private readonly IEventBus _eventBus;
    private IDisposable _turnStateSubscription;
    private IDisposable _characterMovedSubscription;

    private CharacterInstance _currentCharacter;

    public ActionPanelAvailabilityDriver(CharacterRoster characterRoster, IEventBus eventBus)
    {
        _characterRoster = characterRoster;
        _eventBus = eventBus;
    }

    public void PostInitialize()
    {
        _turnStateSubscription = _eventBus.Subscribe<TurnPhaseChanged>(OnTurnStateChanged);
        _characterMovedSubscription = _eventBus.Subscribe<CharacterMoved>(OnCharacterMoved);
        PublishAvailability();
    }

    public void Dispose()
    {
        _turnStateSubscription?.Dispose();
        _characterMovedSubscription?.Dispose();
    }

    private void OnTurnStateChanged(TurnPhaseChanged msg)
    {
        if (msg.Character != null)
        {
            _currentCharacter = msg.Character;
        }

        PublishAvailability();
    }

    private void OnCharacterMoved(CharacterMoved msg)
    {
        PublishAvailability();
    }

    private void PublishAvailability()
    {
        if (_currentCharacter?.Model?.CurrentCell == null)
        {
            _eventBus.Publish(new AttackAvailabilityChanged(false));
            return;
        }

        var currentCell = _currentCharacter.Model.CurrentCell;
        if (currentCell.Type == CellType.Start)
        {
            _eventBus.Publish(new AttackAvailabilityChanged(false));
            return;
        }

        bool hasTarget = false;
        var characters = _characterRoster.AllCharacters;
        for (int i = 0; i < characters.Count; i++)
        {
            var character = characters[i];
            if (character == null || character == _currentCharacter)
            {
                continue;
            }

            if (character.Model?.CurrentCell == currentCell)
            {
                hasTarget = true;
                break;
            }
        }

        _eventBus.Publish(new AttackAvailabilityChanged(hasTarget));
    }
}
