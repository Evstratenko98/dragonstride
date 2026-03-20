using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MainMenuController : MonoBehaviour
{
    [SerializeField] private Button openLobbyButton;
    [SerializeField] private Button quitButton;

    private void Awake()
    {
        if (openLobbyButton != null)
        {
            openLobbyButton.onClick.AddListener(OpenLobby);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitGame);
        }
    }

    public void OpenLobby()
    {
        GameSessionState.ClearWinner();
        SceneManager.LoadScene(GameSceneNames.LobbyScene);
    }

    public void QuitGame()
    {
        Debug.Log("[MainMenuController] Quit requested from main menu.");
        Application.Quit();
    }
}
