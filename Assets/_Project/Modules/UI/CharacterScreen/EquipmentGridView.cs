using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EquipmentGridView : MonoBehaviour
{
    [SerializeField] private RectTransform gridRoot;
    [SerializeField] private EquipmentSlotView slotTemplate;
    [SerializeField] private GridLayoutGroup gridLayout;

    private readonly List<EquipmentSlotView> _slots = new();
    private CharacterEquipment _equipment;
    private InventoryGridView _inventoryGridView;

    private void Awake()
    {
        if (slotTemplate != null)
        {
            slotTemplate.gameObject.SetActive(false);
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
        if (_equipment == null || _inventoryGridView == null)
        {
            return;
        }

        bool equipped = _inventoryGridView.HandleDropToEquipment(_equipment, targetIndex);
        if (equipped)
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
}
