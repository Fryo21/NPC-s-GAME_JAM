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
    public Canvas parentCanvas;
    private RectTransform canvasRectTransform;
    private Vector2 dragOffset;

    [Header("Appearance")]
    //[SerializeField] private Color headerColor = new Color(0.3f, 0.3f, 0.7f); // Blue-ish for drones
    [SerializeField] private float edgePadding = 20f;

    private void Awake()
    {
        // Get parent canvas
        parentCanvas = transform.parent.GetComponentInParent<Canvas>();
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
            // if (headerImage != null)
            // {
            //     headerImage.color = headerColor;
            // }
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
        if (parentCanvas == null || backgroundPanel == null) return;

        // Get canvas and background panel rect transforms
        RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();

        // Get the canvas world corners
        Vector3[] canvasCorners = new Vector3[4];
        canvasRect.GetWorldCorners(canvasCorners);

        // Get the panel world corners
        Vector3[] panelCorners = new Vector3[4];
        backgroundPanel.GetWorldCorners(panelCorners);

        // Calculate bounds
        // canvasCorners[0] = bottom-left, canvasCorners[2] = top-right
        // panelCorners[0] = bottom-left, panelCorners[2] = top-right

        // Calculate min and max positions in world space
        float minX = canvasCorners[0].x + edgePadding;
        float maxX = canvasCorners[2].x - edgePadding;
        float minY = canvasCorners[0].y + edgePadding;
        float maxY = canvasCorners[2].y - edgePadding;

        // Panel dimensions
        float panelWidth = panelCorners[2].x - panelCorners[0].x;
        float panelHeight = panelCorners[2].y - panelCorners[0].y;

        // Current position
        Vector3 currentPos = transform.position;

        // Calculate clamped position to ensure panel stays in view
        // We need to check both left and right edges, top and bottom edges
        float leftEdge = panelCorners[0].x;
        float rightEdge = panelCorners[2].x;
        float bottomEdge = panelCorners[0].y;
        float topEdge = panelCorners[2].y;

        Vector3 newPos = currentPos;

        // Adjust X position if needed
        if (leftEdge < minX)
        {
            newPos.x += (minX - leftEdge);
        }
        else if (rightEdge > maxX)
        {
            newPos.x -= (rightEdge - maxX);
        }

        // Adjust Y position if needed
        if (bottomEdge < minY)
        {
            newPos.y += (minY - bottomEdge);
        }
        else if (topEdge > maxY)
        {
            newPos.y -= (topEdge - maxY);
        }

        // Apply position
        transform.position = newPos;
    }
}