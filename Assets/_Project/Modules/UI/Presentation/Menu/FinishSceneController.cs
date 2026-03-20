using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class FinishSceneController : MonoBehaviour
{
    [SerializeField] private TMP_Text winnerText;
    [SerializeField] private Button returnToMenuButton;

    private void Awake()
    {
        if (winnerText != null)
        {
            string winnerName = string.IsNullOrWhiteSpace(GameSessionState.WinnerName)
                ? "Неизвестный герой"
                : GameSessionState.WinnerName;
            winnerText.text = $"Победитель: {winnerName}";
        }

        if (returnToMenuButton != null)
        {
            returnToMenuButton.onClick.AddListener(ReturnToMainMenu);
        }
    }

    public void ReturnToMainMenu()
    {
        GameSessionState.ClearWinner();
        SceneManager.LoadScene(GameSceneNames.MainMenuScene);
    }
}
