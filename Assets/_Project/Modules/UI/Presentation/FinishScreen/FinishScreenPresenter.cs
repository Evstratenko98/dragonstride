using UnityEngine.SceneManagement;
using VContainer.Unity;

public class FinishScreenPresenter : IStartable
{
    private readonly FinishScreenView _view;
    
    public FinishScreenPresenter(FinishScreenView view)
    {
        _view = view;
    }
    
    public void Start()
    {
        _view.PlayAgainButton.onClick.AddListener(OnFinishClicked);
    }

    private void OnFinishClicked()
    {
        SceneManager.LoadScene(SessionSceneNames.GameOver);
    }
}
