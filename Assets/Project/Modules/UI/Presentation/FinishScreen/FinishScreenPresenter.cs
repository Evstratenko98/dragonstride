using System;
using VContainer.Unity;

public class FinishScreenPresenter : IStartable, IDisposable
{
    private readonly IEventBus _eventBus;
    private readonly FinishScreenView _view;
    
    public FinishScreenPresenter(IEventBus eventBus, FinishScreenView view)
    {
        _eventBus = eventBus;
        _view = view;
    }
    
    public void Start()
    {
        _view.PlayAgainButton.onClick.AddListener(OnFinishClicked);
    }

    public void Dispose()
    {
        _view.PlayAgainButton.onClick.RemoveListener(OnFinishClicked);
    }

    private void OnFinishClicked()
    {
        _eventBus.Publish(new ResetRequested());
    }
}
