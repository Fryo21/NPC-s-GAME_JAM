using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class WantedListUI : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    [Header("UI References")]
    [SerializeField] private Transform content;            // The Content object inside Viewport
    [SerializeField] private GameObject wantedPersonPrefab; // Prefab for each wanted person
    [SerializeField] private TextMeshProUGUI titleText;    // The TitleText component
    [SerializeField] private RectTransform titleBar;       // The TitleBar RectTransform (for dragging)
    [SerializeField] private RectTransform backgroundPanel; // The BackgroundPanel RectTransform

    [Header("Dragging Settings")]
    [SerializeField] private float edgePadding = 20f;     // Padding from screen edges

    // State variables
    private List<GameObject> wantedEntries = new List<GameObject>();
    private Vector2 dragOffset;
    private Canvas parentCanvas;
    private RectTransform canvasRectTransform;

    private void Awake()
    {
        // Get parent canvas
        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null)
        {
            Debug.LogError("WantedListUI must be a child of a Canvas!");
        }
        else
        {
            canvasRectTransform = parentCanvas.GetComponent<RectTransform>();
        }
    }

    private void Start()
    {
        // Set up initial title
        if (titleText != null)
        {
            titleText.text = "WANTED";
        }

        // Subscribe to wanted list updates
        if (WantedListManager.Instance != null)
        {
            WantedListManager.Instance.OnWantedListUpdated += UpdateWantedList;
        }
        else
        {
            Debug.LogWarning("WantedListManager not found!");
        }
    }

    public void UpdateWantedList(List<NPCData> wantedList)
    {
        // Clear existing entries
        ClearWantedEntries();

        // Update title
        if (titleText != null)
        {
            titleText.text = wantedList.Count > 0 ? $"WANTED ({wantedList.Count})" : "ALL SUSPECTS APPREHENDED";
        }

        // Create new entries for each wanted person
        foreach (NPCData npcData in wantedList)
        {
            CreateWantedEntry(npcData);
        }
    }

    private void CreateWantedEntry(NPCData npcData)
    {
        if (wantedPersonPrefab == null || content == null || npcData == null) return;

        // Instantiate entry
        GameObject entry = Instantiate(wantedPersonPrefab, content);

        // Initialize the entry using its WantedPersonEntry component
        WantedPersonEntry entryComponent = entry.GetComponent<WantedPersonEntry>();
        if (entryComponent != null)
        {
            entryComponent.Initialize(npcData);
        }
        else
        {
            Debug.LogWarning("WantedPersonEntry component not found on prefab!");
        }

        // Add to list
        wantedEntries.Add(entry);
    }

    private void ClearWantedEntries()
    {
        foreach (GameObject entry in wantedEntries)
        {
            if (entry != null)
            {
                Destroy(entry);
            }
        }
        wantedEntries.Clear();
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

        // Only allow dragging from the title bar
        if (titleBar != null && !RectTransformUtility.RectangleContainsScreenPoint(
            titleBar, eventData.position, eventData.pressEventCamera))
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

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (WantedListManager.Instance != null)
        {
            WantedListManager.Instance.OnWantedListUpdated -= UpdateWantedList;
        }
    }
}