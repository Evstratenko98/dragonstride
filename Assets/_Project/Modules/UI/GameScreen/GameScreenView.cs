using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameScreenView : MonoBehaviour
{
    [SerializeField] private Button characaterButton;
    [SerializeField] private Button diceButton;
    [SerializeField] private TMP_Text diceButtonLabel;
    public Button CharacaterButton => characaterButton;
    public Button DiceButton => diceButton;
    public TMP_Text DiceButtonLabel => diceButtonLabel;
}
