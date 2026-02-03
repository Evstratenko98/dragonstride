using UnityEngine;
using UnityEngine.UI;

public class ChestLootView : MonoBehaviour
{
    [SerializeField] private Button takeButton;
    [SerializeField] private LootGridView lootGridView;

    public Button TakeButton => takeButton;
    public LootGridView LootGridView => lootGridView;

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
