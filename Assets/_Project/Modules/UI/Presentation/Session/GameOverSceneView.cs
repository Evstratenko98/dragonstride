using UnityEngine;
using UnityEngine.UI;

public sealed class GameOverSceneView : MonoBehaviour
{
    [SerializeField] private Button returnToMainMenuButton;
    [SerializeField] private Text resultText;

    public Button ReturnToMainMenuButton => returnToMainMenuButton;

    private void Awake()
    {
        returnToMainMenuButton ??= FindButton("ReturnToMainMenuButton");
        resultText ??= FindText("ResultText");
    }

    public void SetResultText(string text)
    {
        if (resultText != null)
        {
            resultText.text = text;
        }
    }

    private Button FindButton(string objectName)
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (button != null && button.gameObject.name == objectName)
            {
                return button;
            }
        }

        Debug.LogError($"[GameOverSceneView] Button '{objectName}' was not found in scene hierarchy.");
        return null;
    }

    private Text FindText(string objectName)
    {
        Text[] texts = GetComponentsInChildren<Text>(true);
        foreach (Text text in texts)
        {
            if (text != null && text.gameObject.name == objectName)
            {
                return text;
            }
        }

        Debug.LogError($"[GameOverSceneView] Text '{objectName}' was not found in scene hierarchy.");
        return null;
    }
}
