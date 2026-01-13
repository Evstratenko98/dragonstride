using System;
using VContainer.Unity;

public class CharacterMenuController : IStartable, IDisposable
{
    private readonly CharacterMenuView _view;

    public CharacterMenuController(CharacterMenuView view)
    {
        _view = view;
    }

    public void Start()
    {
        _view.OpenButton.onClick.AddListener(_view.Open);
        if (_view.BackgroundButton != null)
        {
            _view.BackgroundButton.onClick.AddListener(_view.Close);
        }

        if (_view.CloseButton != null)
        {
            _view.CloseButton.onClick.AddListener(_view.Close);
        }
    }

    public void Dispose()
    {
        _view.OpenButton.onClick.RemoveAllListeners();
        if (_view.BackgroundButton != null)
        {
            _view.BackgroundButton.onClick.RemoveAllListeners();
        }

        if (_view.CloseButton != null)
        {
            _view.CloseButton.onClick.RemoveAllListeners();
        }
    }
}
