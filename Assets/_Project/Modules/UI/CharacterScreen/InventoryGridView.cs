using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryGridView : MonoBehaviour
{
    [SerializeField] private RectTransform gridRoot;
    [SerializeField] private InventorySlotView slotTemplate;
    [SerializeField] private InventoryDragIconView dragIcon;

    private readonly List<InventorySlotView> _slots = new();
    private Inventory _inventory;
    private int _dragIndex = -1;
    private bool _isDragging;

    private void Awake()
    {
        if (slotTemplate != null)
        {
            slotTemplate.gameObject.SetActive(false);
        }

        if (dragIcon != null)
        {
            dragIcon.Hide();
        }
    }

    public void InitializeSlots(int capacity)
    {
        if (gridRoot == null || slotTemplate == null)
        {
            Debug.LogError("[InventoryGridView] Grid root or slot template is not assigned.");
            return;
        }

        foreach (var slot in _slots)
        {
            if (slot != null)
            {
                Destroy(slot.gameObject);
            }
        }

        _slots.Clear();

        for (int i = 0; i < capacity; i++)
        {
            var slot = Instantiate(slotTemplate, gridRoot);
            slot.gameObject.SetActive(true);
            slot.Initialize(this, i);
            _slots.Add(slot);
        }
    }

    public void BindInventory(Inventory inventory)
    {
        _inventory = inventory;
        if (_inventory != null)
        {
            if (_slots.Count != _inventory.Capacity)
            {
                InitializeSlots(_inventory.Capacity);
            }

            Refresh();
        }
        else
        {
            Clear();
        }
    }

    public void Refresh()
    {
        if (_inventory == null)
        {
            Clear();
            return;
        }

        for (int i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            var data = _inventory.Slots[i];
            slot.SetData(data.Definition, data.Count);
        }
    }

    public void HandleBeginDrag(int index, PointerEventData eventData)
    {
        if (_inventory == null || index < 0 || index >= _inventory.Slots.Count)
        {
            return;
        }

        var slot = _inventory.Slots[index];
        if (slot.IsEmpty)
        {
            return;
        }

        _dragIndex = index;
        _isDragging = true;
        if (dragIcon != null)
        {
            dragIcon.Show(slot.Definition.Icon);
            dragIcon.UpdatePosition(eventData);
        }

        _slots[index].SetDragHidden(true);
    }

    public void HandleDrag(PointerEventData eventData)
    {
        if (!_isDragging)
        {
            return;
        }

        dragIcon?.UpdatePosition(eventData);
    }

    public void HandleDrop(int targetIndex)
    {
        if (!_isDragging || _inventory == null)
        {
            return;
        }

        if (targetIndex < 0 || targetIndex >= _inventory.Slots.Count)
        {
            return;
        }

        _inventory.SwapSlots(_dragIndex, targetIndex);
        _dragIndex = -1;
        _isDragging = false;
        dragIcon?.Hide();
        Refresh();
    }

    public void HandleEndDrag()
    {
        if (!_isDragging)
        {
            return;
        }

        _dragIndex = -1;
        _isDragging = false;
        dragIcon?.Hide();
        Refresh();
    }

    public void Clear()
    {
        foreach (var slot in _slots)
        {
            slot.SetData(null, 0);
        }
    }
}
