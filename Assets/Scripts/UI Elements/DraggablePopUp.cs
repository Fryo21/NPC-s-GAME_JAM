using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggablePopup : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Canvas canvas;
    private RectTransform rectTransform;
    private Vector2 dragOffset;
    private CanvasGroup canvasGroup;

    [SerializeField] private float edgePadding = 10f;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Record offset between mouse position and popup position
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out dragOffset);

        // Make it slightly transparent while dragging
        canvasGroup.alpha = 0.8f;

        // Bring to front
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (canvas == null) return;

        // Convert screen position to canvas position
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.GetComponent<RectTransform>(),
            eventData.position,
            eventData.pressEventCamera,
            out localPoint);

        // Update position
        rectTransform.anchoredPosition = localPoint - dragOffset;

        // Keep within canvas bounds
        KeepInBounds();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Restore original appearance
        canvasGroup.alpha = 1f;

        // Ensure popup is within bounds
        KeepInBounds();
    }

    private void KeepInBounds()
    {
        if (canvas == null) return;

        // Get canvas rect
        Rect canvasRect = canvas.GetComponent<RectTransform>().rect;

        // Get popup size
        Vector2 size = rectTransform.rect.size;

        // Calculate bounds
        float minX = -canvasRect.width / 2 + size.x / 2 + edgePadding;
        float maxX = canvasRect.width / 2 - size.x / 2 - edgePadding;
        float minY = -canvasRect.height / 2 + size.y / 2 + edgePadding;
        float maxY = canvasRect.height / 2 - size.y / 2 - edgePadding;

        // Clamp position
        Vector2 pos = rectTransform.anchoredPosition;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);

        // Apply clamped position
        rectTransform.anchoredPosition = pos;
    }
}