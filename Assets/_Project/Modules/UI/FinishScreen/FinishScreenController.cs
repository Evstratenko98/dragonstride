using System;
using VContainer.Unity;

public class FinishScreenController: IStartable, IDisposable
{
    private readonly IEventBus _eventBus;
    private readonly FinishScreenView _view;
    
    public FinishScreenController(IEventBus eventBus, FinishScreenView view)
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
        _eventBus.Publish(new ResetButtonPressedMessage());
    }
}
