using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameScreenView : MonoBehaviour
{
    [SerializeField] private Button characaterButton;
    [SerializeField] private TMP_Text currentPlayerText;
    [SerializeField] private TMP_Text turnStateText;
    [SerializeField] private TMP_Text stepsText;

    public Button CharacaterButton => characaterButton;

    public void SetCurrentPlayer(string playerName)
    {
        if (currentPlayerText == null)
        {
            return;
        }

        currentPlayerText.text = $"Ход игрока: {playerName}";
    }

    public void SetTurnState(string state)
    {
        if (turnStateText == null)
        {
            return;
        }

        turnStateText.text = $"Стадия: {state}";
    }

    public void SetSteps(int remaining, int total)
    {
        if (stepsText == null)
        {
            return;
        }

        stepsText.text = $"Перемещения: {remaining}/{total}";
    }
}
