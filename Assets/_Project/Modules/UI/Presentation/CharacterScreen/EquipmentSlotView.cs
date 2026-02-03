using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EquipmentSlotView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text countLabel;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Color validDropColor = new(0.2f, 0.85f, 0.3f, 0.8f);
    [SerializeField] private Color invalidDropColor = new(0.9f, 0.2f, 0.2f, 0.8f);

    private EquipmentGridView _grid;
    private int _index;
    private bool _hasItem;
    private Color _defaultBackgroundColor;
    private bool _isPreviewActive;

    private void Awake()
    {
        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>();
        }

        if (backgroundImage != null)
        {
            _defaultBackgroundColor = backgroundImage.color;
        }
    }

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

    public void OnPointerEnter(PointerEventData eventData)
    {
        _grid?.HandlePointerEnter(_index);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ClearDropPreview();
    }

    public void ShowDropPreview(bool isValid)
    {
        if (backgroundImage == null)
        {
            return;
        }

        backgroundImage.color = isValid ? validDropColor : invalidDropColor;
        _isPreviewActive = true;
    }

    public void ClearDropPreview()
    {
        if (backgroundImage == null || !_isPreviewActive)
        {
            return;
        }

        backgroundImage.color = _defaultBackgroundColor;
        _isPreviewActive = false;
    }
}
