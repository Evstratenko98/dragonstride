using System;
using VContainer.Unity;

public class CharacterLayoutController : IPostInitializable, IDisposable
{
    private readonly IEventBus _eventBus;
    private readonly CharacterService _characterService;
    private readonly CharacterLayoutService _layoutService;
    private IDisposable _moveSubscription;

    public CharacterLayoutController(
        IEventBus eventBus,
        CharacterService characterService,
        CharacterLayoutService layoutService)
    {
        _eventBus = eventBus;
        _characterService = characterService;
        _layoutService = layoutService;
    }

    public void PostInitialize()
    {
        _moveSubscription = _eventBus.Subscribe<CharacterMovedMessage>(OnCharacterMoved);
    }

    public void Dispose()
    {
        _moveSubscription?.Dispose();
    }

    private void OnCharacterMoved(CharacterMovedMessage msg)
    {
        if (msg.PreviousCell != null)
        {
            UpdateLayoutForCell(msg.PreviousCell);
        }

        if (msg.CurrentCell != null)
        {
            UpdateLayoutForCell(msg.CurrentCell);
        }
    }

    private void UpdateLayoutForCell(CellModel cell)
    {
        var characters = _characterService.GetCharactersInCell(cell);
        _layoutService.ApplyLayout(cell, characters);
    }
}
