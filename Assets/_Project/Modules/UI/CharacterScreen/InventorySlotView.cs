using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text countLabel;

    private InventoryGridView _grid;
    private int _index;
    private bool _hasItem;

    public void Initialize(InventoryGridView grid, int index)
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

    public void SetDragHidden(bool hidden)
    {
        if (!_hasItem)
        {
            return;
        }

        if (iconImage != null)
        {
            iconImage.enabled = !hidden && iconImage.sprite != null;
        }

        if (countLabel != null)
        {
            countLabel.enabled = !hidden && !string.IsNullOrEmpty(countLabel.text);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _grid?.HandleBeginDrag(_index, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        _grid?.HandleDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _grid?.HandleEndDrag();
    }

    public void OnDrop(PointerEventData eventData)
    {
        _grid?.HandleDrop(_index);
    }
}
