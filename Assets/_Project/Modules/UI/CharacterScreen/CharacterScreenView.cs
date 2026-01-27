using UnityEngine;
using UnityEngine.UI;

public class CharacterScreenView : MonoBehaviour
{
    [SerializeField] private Button closeButton;
    [SerializeField] private InventoryGridView inventoryGridView;
    [SerializeField] private EquipmentGridView equipmentGridView;

    public Button CloseButton => closeButton;
    public InventoryGridView InventoryGridView => inventoryGridView;
    public EquipmentGridView EquipmentGridView => equipmentGridView;

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

    public void BindEquipment(CharacterEquipment equipment)
    {
        equipmentGridView?.BindEquipment(equipment);
    }
}
