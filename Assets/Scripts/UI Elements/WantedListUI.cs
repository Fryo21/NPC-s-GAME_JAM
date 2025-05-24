using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class WantedListUI : MonoBehaviour//, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    [Header("UI References")]
    [SerializeField] private Transform content;            // The Content object inside Viewport
    [SerializeField] private GameObject wantedPersonPrefab; // Prefab for each wanted person
    [SerializeField] private TextMeshProUGUI titleText;    // The TitleText component
    [SerializeField] private RectTransform titleBar;       // The TitleBar RectTransform (for dragging)
    [SerializeField] private RectTransform backgroundPanel; // The BackgroundPanel RectTransform

    // State variables
    private List<GameObject> wantedEntries = new List<GameObject>();


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


    private void OnDestroy()
    {
        // Unsubscribe from events
        if (WantedListManager.Instance != null)
        {
            WantedListManager.Instance.OnWantedListUpdated -= UpdateWantedList;
        }
    }
}