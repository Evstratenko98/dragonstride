using System;
using VContainer.Unity;

public class CharacterScreenController : IStartable, IDisposable
{
    private readonly IEventBus _eventBus;
    private readonly CharacterScreenView _view;
    private IDisposable _openSubscription;

    public CharacterScreenController(IEventBus eventBus, CharacterScreenView view)
    {
        _eventBus = eventBus;
        _view = view;
    }

    public void Start()
    {
        _view.Hide();
        _openSubscription = _eventBus.Subscribe<CharacterButtonPressedMessage>(OnOpenRequested);

        if (_view.CloseButton != null)
        {
            _view.CloseButton.onClick.AddListener(OnCloseClicked);
        }
    }

    public void Dispose()
    {
        _openSubscription?.Dispose();

        if (_view.CloseButton != null)
        {
            _view.CloseButton.onClick.RemoveListener(OnCloseClicked);
        }
    }

    private void OnOpenRequested(CharacterButtonPressedMessage message)
    {
        _view.Show();
    }

    private void OnCloseClicked()
    {
        _view.Hide();
    }
}
