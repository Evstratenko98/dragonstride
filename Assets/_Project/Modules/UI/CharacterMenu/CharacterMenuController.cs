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
        _view.CloseButton.onClick.AddListener(_view.Close);
    }

    public void Dispose()
    {
        _view.OpenButton.onClick.RemoveAllListeners();
        _view.CloseButton.onClick.RemoveAllListeners();
    }
}