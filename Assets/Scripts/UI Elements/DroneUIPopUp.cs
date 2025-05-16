using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class DroneUIPopup : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    [Header("UI References")]
    [SerializeField] private RectTransform dragHandle; // Assign the HeaderBar
    [SerializeField] private RectTransform backgroundPanel;

    // Drag state
    private Canvas parentCanvas;
    private RectTransform canvasRectTransform;
    private Vector2 dragOffset;

    [Header("Appearance")]
    [SerializeField] private Color headerColor = new Color(0.3f, 0.3f, 0.7f); // Blue-ish for drones
    [SerializeField] private float edgePadding = 20f;

    private void Awake()
    {
        // Get parent canvas
        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null)
        {
            Debug.LogError("DroneUIPopup must be a child of a Canvas!");
        }
        else
        {
            canvasRectTransform = parentCanvas.GetComponent<RectTransform>();
        }

        // Apply header color
        if (dragHandle != null)
        {
            Image headerImage = dragHandle.GetComponent<Image>();
            if (headerImage != null)
            {
                headerImage.color = headerColor;
            }
        }
    }

    // IDragHandler implementation
    public void OnDrag(PointerEventData eventData)
    {
        if (parentCanvas == null) return;

        // Move panel with cursor
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint);

        transform.position = parentCanvas.transform.TransformPoint(localPoint + dragOffset);

        // Keep in bounds
        KeepInBounds();
    }

    // IBeginDragHandler implementation
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (parentCanvas == null) return;

        // Only allow dragging from the header/drag handle
        if (dragHandle != null && !RectTransformUtility.RectangleContainsScreenPoint(
            dragHandle, eventData.position, eventData.pressEventCamera))
        {
            eventData.pointerDrag = null;
            return;
        }

        // Calculate drag offset
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint);

        dragOffset = (Vector2)transform.position - (Vector2)parentCanvas.transform.TransformPoint(localPoint);

        // Bring to front when dragging starts
        transform.SetAsLastSibling();
    }

    // IEndDragHandler implementation
    public void OnEndDrag(PointerEventData eventData)
    {
        // Keep in bounds after drag ends
        KeepInBounds();
    }

    private void KeepInBounds()
    {
        if (parentCanvas == null || canvasRectTransform == null || backgroundPanel == null) return;

        // Get canvas and panel dimensions
        Vector2 canvasSize = canvasRectTransform.rect.size;
        Vector2 panelSize = backgroundPanel.rect.size * backgroundPanel.localScale;

        // Calculate canvas edges in world space
        Vector3[] canvasCorners = new Vector3[4];
        canvasRectTransform.GetWorldCorners(canvasCorners);

        // Get panel position
        Vector3[] panelCorners = new Vector3[4];
        backgroundPanel.GetWorldCorners(panelCorners);

        // Get panel min/max bounds
        float minX = panelCorners[0].x;
        float maxX = panelCorners[2].x;
        float minY = panelCorners[0].y;
        float maxY = panelCorners[2].y;

        // Get canvas min/max bounds
        float canvasMinX = canvasCorners[0].x + edgePadding;
        float canvasMaxX = canvasCorners[2].x - edgePadding;
        float canvasMinY = canvasCorners[0].y + edgePadding;
        float canvasMaxY = canvasCorners[2].y - edgePadding;

        // Calculate clamped position
        Vector3 newPos = transform.position;

        // Clamp X position
        if (minX < canvasMinX) newPos.x += (canvasMinX - minX);
        if (maxX > canvasMaxX) newPos.x -= (maxX - canvasMaxX);

        // Clamp Y position
        if (minY < canvasMinY) newPos.y += (canvasMinY - minY);
        if (maxY > canvasMaxY) newPos.y -= (maxY - canvasMaxY);

        // Apply clamped position
        transform.position = newPos;
    }
}