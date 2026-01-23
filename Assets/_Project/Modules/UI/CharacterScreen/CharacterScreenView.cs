using UnityEngine;
using UnityEngine.UI;

public class CharacterScreenView : MonoBehaviour
{
    [SerializeField] private Button closeButton;
    [SerializeField] private InventoryGridView inventoryGridView;

    public Button CloseButton => closeButton;
    public InventoryGridView InventoryGridView => inventoryGridView;

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void BindInventory(Inventory inventory)
    {
        inventoryGridView?.BindInventory(inventory);
    }
}
