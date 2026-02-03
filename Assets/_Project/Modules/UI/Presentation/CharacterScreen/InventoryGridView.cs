using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryGridView : MonoBehaviour
{
    [SerializeField] private RectTransform gridRoot;
    [SerializeField] private InventorySlotView slotTemplate;
    [SerializeField] private InventoryDragIconView dragIcon;
    [SerializeField] private GridLayoutGroup gridLayout;

    private readonly List<InventorySlotView> _slots = new();
    private Inventory _inventory;
    private EquipmentGridView _equipmentGridView;
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

    public void InitializeSlots(int capacity)
    {
        if (gridRoot == null || slotTemplate == null)
        {
            Debug.LogError("[InventoryGridView] Grid root or slot template is not assigned.");
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

        int columns = gridLayout != null && gridLayout.constraint == GridLayoutGroup.Constraint.FixedColumnCount
            ? Mathf.Max(1, gridLayout.constraintCount)
            : Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(capacity)));

        var padding = gridLayout != null ? gridLayout.padding : new RectOffset();
        var cellSize = gridLayout != null ? gridLayout.cellSize : new Vector2(64f, 64f);
        var spacing = gridLayout != null ? gridLayout.spacing : Vector2.zero;

        for (int i = 0; i < capacity; i++)
        {
            var slot = Instantiate(slotTemplate, gridRoot);
            var slotTransform = slot.GetComponent<RectTransform>();
            slotTransform.anchorMin = new Vector2(0f, 1f);
            slotTransform.anchorMax = new Vector2(0f, 1f);
            slotTransform.pivot = new Vector2(0.5f, 0.5f);

            int column = i % columns;
            int row = i / columns;
            float x = padding.left + (cellSize.x + spacing.x) * column + cellSize.x * 0.5f;
            float y = -padding.top - (cellSize.y + spacing.y) * row - cellSize.y * 0.5f;
            slotTransform.anchoredPosition = new Vector2(x, y);
            slotTransform.sizeDelta = cellSize;

            slot.gameObject.SetActive(true);
            slot.Initialize(this, i);
            _slots.Add(slot);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(gridRoot);
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

    public void BindEquipmentGrid(EquipmentGridView equipmentGridView)
    {
        _equipmentGridView = equipmentGridView;
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
        if (_inventory == null)
        {
            return;
        }

        if (targetIndex < 0 || targetIndex >= _inventory.Slots.Count)
        {
            return;
        }

        if (!_isDragging)
        {
            if (_equipmentGridView != null && _equipmentGridView.IsDragging)
            {
                _equipmentGridView.HandleDropToInventory(targetIndex);
            }
            return;
        }

        _inventory.SwapSlots(_dragIndex, targetIndex);
        _dragIndex = -1;
        _isDragging = false;
        dragIcon?.Hide();
        _equipmentGridView?.ClearDropPreviews();
        Refresh();
    }

    public bool HandleDropToEquipment(CharacterEquipment equipment, int equipmentSlotIndex)
    {
        if (!_isDragging || _inventory == null || equipment == null)
        {
            return false;
        }

        bool equipped = equipment.EquipFromInventory(_inventory, _dragIndex, equipmentSlotIndex);
        _dragIndex = -1;
        _isDragging = false;
        dragIcon?.Hide();
        _equipmentGridView?.ClearDropPreviews();
        Refresh();
        return equipped;
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
        _equipmentGridView?.ClearDropPreviews();
        Refresh();
    }

    public void HandleDropFromEquipment(CharacterEquipment equipment, int equipmentSlotIndex, int inventorySlotIndex)
    {
        if (_inventory == null || equipment == null)
        {
            return;
        }

        bool moved = equipment.UnequipToInventorySlot(_inventory, equipmentSlotIndex, inventorySlotIndex);
        if (moved)
        {
            Refresh();
        }
    }

    public void Clear()
    {
        foreach (var slot in _slots)
        {
            slot.SetData(null, 0);
        }
    }

    public bool TryGetDraggedItemDefinition(out ItemDefinition definition)
    {
        definition = null;

        if (!_isDragging || _inventory == null || _dragIndex < 0 || _dragIndex >= _inventory.Slots.Count)
        {
            return false;
        }

        var slot = _inventory.Slots[_dragIndex];
        if (slot == null || slot.IsEmpty)
        {
            return false;
        }

        definition = slot.Definition;
        return definition != null;
    }
}
