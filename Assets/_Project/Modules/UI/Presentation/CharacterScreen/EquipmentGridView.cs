using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EquipmentGridView : MonoBehaviour
{
    [SerializeField] private RectTransform gridRoot;
    [SerializeField] private EquipmentSlotView slotTemplate;
    [SerializeField] private InventoryDragIconView dragIcon;
    [SerializeField] private GridLayoutGroup gridLayout;

    private readonly List<EquipmentSlotView> _slots = new();
    private CharacterEquipment _equipment;
    private InventoryGridView _inventoryGridView;
    private int _dragIndex = -1;
    private bool _isDragging;
    public bool IsDragging => _isDragging;

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

    public void BindInventoryGrid(InventoryGridView inventoryGridView)
    {
        _inventoryGridView = inventoryGridView;
    }

    public void InitializeSlots(int capacity)
    {
        if (gridRoot == null || slotTemplate == null)
        {
            Debug.LogError("[EquipmentGridView] Grid root or slot template is not assigned.");
            return;
        }

        if (gridLayout == null)
        {
            gridLayout = gridRoot.GetComponent<GridLayoutGroup>();
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

        LayoutRebuilder.ForceRebuildLayoutImmediate(gridRoot);
    }

    public void BindEquipment(CharacterEquipment equipment)
    {
        _equipment = equipment;
        if (_equipment != null)
        {
            if (_slots.Count != _equipment.Capacity)
            {
                InitializeSlots(_equipment.Capacity);
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
        if (_equipment == null)
        {
            Clear();
            return;
        }

        for (int i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            var data = _equipment.Slots[i];
            slot.SetData(data.Definition, data.IsEmpty ? 0 : 1);
        }
    }

    public void HandleDrop(int targetIndex)
    {
        if (_equipment == null)
        {
            return;
        }

        if (_isDragging)
        {
            bool swapped = _equipment.SwapSlots(_dragIndex, targetIndex);
            _dragIndex = -1;
            _isDragging = false;
            dragIcon?.Hide();
            Refresh();
            return;
        }

        if (_inventoryGridView == null)
        {
            return;
        }

        bool equipped = _inventoryGridView.HandleDropToEquipment(_equipment, targetIndex);
        ClearDropPreviews();
        if (equipped)
        {
            Refresh();
        }
    }

    public void HandleBeginDrag(int index, PointerEventData eventData)
    {
        if (_equipment == null || index < 0 || index >= _equipment.Slots.Count)
        {
            return;
        }

        var slot = _equipment.Slots[index];
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

    public void HandleDropToInventory(int inventorySlotIndex)
    {
        if (_equipment == null || _inventoryGridView == null || !_isDragging)
        {
            return;
        }

        _inventoryGridView.HandleDropFromEquipment(_equipment, _dragIndex, inventorySlotIndex);
        _dragIndex = -1;
        _isDragging = false;
        dragIcon?.Hide();
        Refresh();
    }

    public void HandlePointerEnter(int index)
    {
        if (_equipment == null || _inventoryGridView == null)
        {
            _slots[index].ClearDropPreview();
            return;
        }

        if (!_inventoryGridView.TryGetDraggedItemDefinition(out var itemDefinition))
        {
            _slots[index].ClearDropPreview();
            return;
        }

        bool canEquip = itemDefinition.Type == ItemType.Weapon && _equipment.Slots[index].IsEmpty;
        _slots[index].ShowDropPreview(canEquip);
    }

    public void ClearDropPreviews()
    {
        foreach (var slot in _slots)
        {
            slot.ClearDropPreview();
        }
    }

    public void Clear()
    {
        foreach (var slot in _slots)
        {
            slot.SetData(null, 0);
        }
    }
}
