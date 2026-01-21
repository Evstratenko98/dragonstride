using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameScreenView : MonoBehaviour
{
    [SerializeField] private Button characaterButton;
    [SerializeField] private TMP_Text roundText;
    [SerializeField] private TMP_Text turnStageText;
    [SerializeField] private TMP_Text activeCharacterText;
    [SerializeField] private TMP_Text diceRollText;

    private GameHudData _hudData;

    public Button CharacaterButton => characaterButton;

    public void UpdateHud(GameHudData data)
    {
        if (roundText == null || turnStageText == null || activeCharacterText == null || diceRollText == null)
        {
            return;
        }

        if (data.RoundNumber != _hudData.RoundNumber)
        {
            roundText.SetText($"Раунд {data.RoundNumber}");
        }

        if (data.TurnStage != _hudData.TurnStage)
        {
            turnStageText.SetText($"Стадия: {data.TurnStage}");
        }

        if (data.ActiveCharacterName != _hudData.ActiveCharacterName)
        {
            activeCharacterText.SetText($"Ходит: {data.ActiveCharacterName}");
        }

        if (data.DiceRoll != _hudData.DiceRoll)
        {
            diceRollText.SetText($"Бросок: {data.DiceRoll}");
        }

        _hudData = data;
    }

    [System.Serializable]
    public struct GameHudData
    {
        public int RoundNumber;
        public string TurnStage;
        public string ActiveCharacterName;
        public string DiceRoll;
    }
}
