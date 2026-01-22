using UnityEngine;
using UnityEngine.UI;

public class CharacterScreenView : MonoBehaviour
{
    [SerializeField] private Button closeButton;

    public Button CloseButton => closeButton;

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
