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
