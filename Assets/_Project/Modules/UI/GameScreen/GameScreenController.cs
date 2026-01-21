using System;
using VContainer.Unity;

public class GameScreenController : IStartable, IDisposable
{
    private readonly GameScreenView _view;

    public GameScreenController(GameScreenView view)
    {
        _view = view;
    }

    public void Start()
    {
        // _view.CharacaterButton.onClick.AddListener(_view.Open);
    }

    public void Dispose()
    {
        // _view.CharacaterButton.onClick.RemoveAllListeners();
    }
}
