using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameScreenView : MonoBehaviour
{
    [SerializeField] private Button characaterButton;
    [SerializeField] private Button diceButton;
    [SerializeField] private TMP_Text currentPlayerText;
    [SerializeField] private TMP_Text turnStateText;
    [SerializeField] private TMP_Text stepsText;
    [SerializeField] private TMP_Text diceButtonText;

    public Button CharacaterButton => characaterButton;
    public Button DiceButton => diceButton;

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

    public void SetDiceButtonLabel(string label)
    {
        var targetText = diceButtonText != null
            ? diceButtonText
            : diceButton != null ? diceButton.GetComponentInChildren<TMP_Text>() : null;

        if (targetText == null)
        {
            return;
        }

        targetText.text = label;
    }

    public void SetDiceButtonInteractable(bool interactable)
    {
        if (diceButton == null)
        {
            return;
        }

        diceButton.interactable = interactable;
    }
}
