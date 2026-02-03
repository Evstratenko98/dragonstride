using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryDragIconView : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Canvas parentCanvas;

    private RectTransform _rectTransform;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        if (parentCanvas == null)
        {
            parentCanvas = GetComponentInParent<Canvas>();
        }
    }

    public void Show(Sprite sprite)
    {
        if (iconImage != null)
        {
            iconImage.sprite = sprite;
            iconImage.enabled = sprite != null;
        }

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void UpdatePosition(PointerEventData eventData)
    {
        if (_rectTransform == null || parentCanvas == null)
        {
            return;
        }

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentCanvas.transform as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint))
        {
            _rectTransform.anchoredPosition = localPoint;
        }
    }
}
