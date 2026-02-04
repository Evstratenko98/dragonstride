using System;
using VContainer.Unity;

public class CharacterRosterPanelPresenter : IPostInitializable, IDisposable
{
    private readonly IEventBus _eventBus;
    private readonly CharacterRosterPanelView _view;
    private IDisposable _subscription;

    public CharacterRosterPanelPresenter(IEventBus eventBus, CharacterRosterPanelView view)
    {
        _eventBus = eventBus;
        _view = view;
    }

    public void PostInitialize()
    {
        _subscription = _eventBus.Subscribe<CharacterRosterUpdated>(OnRosterUpdated);
    }

    public void Dispose()
    {
        _subscription?.Dispose();
    }

    private void OnRosterUpdated(CharacterRosterUpdated msg)
    {
        _view.SetCharacters(msg.Characters);
    }
}
