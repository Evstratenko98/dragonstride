using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LootGridView : MonoBehaviour
{
    [SerializeField] private RectTransform gridRoot;
    [SerializeField] private InventorySlotView slotTemplate;
    [SerializeField] private GridLayoutGroup gridLayout;

    private readonly List<InventorySlotView> _slots = new();

    private void Awake()
    {
        if (slotTemplate != null)
        {
            slotTemplate.gameObject.SetActive(false);
        }
    }

    public void InitializeSlots(int capacity)
    {
        if (gridRoot == null || slotTemplate == null)
        {
            Debug.LogError("[LootGridView] Grid root or slot template is not assigned.");
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
            slot.Initialize(null, i);
            _slots.Add(slot);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(gridRoot);
    }

    public void SetItems(IReadOnlyList<ItemDefinition> items)
    {
        if (items == null)
        {
            Clear();
            return;
        }

        if (_slots.Count != items.Count)
        {
            InitializeSlots(items.Count);
        }

        for (int i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            var item = i < items.Count ? items[i] : null;
            slot.SetData(item, item != null ? 1 : 0);
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
