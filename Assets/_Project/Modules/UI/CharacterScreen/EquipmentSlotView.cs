using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EquipmentSlotView : MonoBehaviour, IDropHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text countLabel;

    private EquipmentGridView _grid;
    private int _index;
    private bool _hasItem;

    public void Initialize(EquipmentGridView grid, int index)
    {
        _grid = grid;
        _index = index;
    }

    public void SetData(ItemDefinition definition, int count)
    {
        _hasItem = definition != null;

        if (iconImage != null)
        {
            iconImage.sprite = _hasItem ? definition.Icon : null;
            iconImage.enabled = _hasItem && definition.Icon != null;
        }

        if (countLabel != null)
        {
            countLabel.text = _hasItem && count > 1 ? count.ToString() : string.Empty;
            countLabel.enabled = _hasItem && count > 1;
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        _grid?.HandleDrop(_index);
    }
}
