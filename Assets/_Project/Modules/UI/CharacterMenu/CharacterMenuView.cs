using UnityEngine;
using UnityEngine.UI;

public class CharacterMenuView : MonoBehaviour
{
    [SerializeField] private GameObject modalBackground;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;

    public Button OpenButton => openButton;
    public Button CloseButton => closeButton;

    public void Open() => modalBackground.SetActive(true);
    public void Close() => modalBackground.SetActive(false);
}