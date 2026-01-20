using UnityEngine;

public class UIScreenView : MonoBehaviour
{
    [SerializeField] private GameObject gameScreen;
    [SerializeField] private GameObject finishScreen;

    public void ShowGameScreen()
    {
        if (gameScreen != null)
        {
            gameScreen.SetActive(true);
        }

        if (finishScreen != null)
        {
            finishScreen.SetActive(false);
        }
    }

    public void ShowFinishScreen()
    {
        if (gameScreen != null)
        {
            gameScreen.SetActive(false);
        }

        if (finishScreen != null)
        {
            finishScreen.SetActive(true);
        }
    }
}
