using VContainer.Unity;

public class FinishScreenPresenter : IStartable
{
    private readonly FinishScreenView _view;
    private readonly ISessionSceneRouter _sceneRouter;
    
    public FinishScreenPresenter(FinishScreenView view, ISessionSceneRouter sceneRouter)
    {
        _view = view;
        _sceneRouter = sceneRouter;
    }
    
    public void Start()
    {
        _view.PlayAgainButton.onClick.AddListener(OnFinishClicked);
    }

    private void OnFinishClicked()
    {
        _ = _sceneRouter.LoadGameOverAsync();
    }
}
