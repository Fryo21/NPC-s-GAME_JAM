using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WantedListUIController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform content;            // The Content object inside Viewport (renamed from wantedListParent)
    [SerializeField] private GameObject wantedPersonPrefab; // Prefab for each wanted person (renamed from wantedPersonEntryPrefab)
    [SerializeField] private TextMeshProUGUI titleText;    // The TitleText component
    [SerializeField] private RectTransform titleBar;       // The TitleBar RectTransform (for dragging)
    [SerializeField] private RectTransform backgroundPanel; // The BackgroundPanel RectTransform

    [Header("Debug")]
    [SerializeField] private bool showDebugMessages = true;

    // Keep track of UI entries
    private Dictionary<NPCData, WantedPersonEntry> activeEntries = new Dictionary<NPCData, WantedPersonEntry>();

    private void Start()
    {
        // Set up initial title
        if (titleText != null)
        {
            titleText.text = "WANTED";
        }

        // Subscribe to wanted list manager events
        if (WantedListManager.Instance != null)
        {
            WantedListManager.Instance.OnWantedListUpdated += UpdateWantedListUI;
            WantedListManager.Instance.OnSuspectApprehended += OnSuspectApprehended;
        }
        else
        {
            Debug.LogError("[WantedListUIController] WantedListManager not found!");
        }
    }

    /// <summary>
    /// Called when the wanted list is updated (new round, etc.)
    /// </summary>
    private void UpdateWantedListUI(List<NPCData> newWantedList)
    {
        LogDebug($"Updating wanted list UI with {newWantedList.Count} entries");

        // Update title text
        if (titleText != null)
        {
            titleText.text = newWantedList.Count > 0 ? $"WANTED ({newWantedList.Count})" : "ALL SUSPECTS APPREHENDED";
        }

        // Only clear if this is a completely new list (like a new round)
        // Check if this is a new round by seeing if any current entries exist in the new list
        bool isNewRound = true;
        if (activeEntries.Count > 0)
        {
            foreach (var existingNPC in activeEntries.Keys)
            {
                if (newWantedList.Contains(existingNPC))
                {
                    isNewRound = false;
                    break;
                }
            }
        }

        if (isNewRound)
        {
            LogDebug("Detected new round - clearing all entries");
            // Clear existing entries only for new rounds
            ClearAllEntries();

            // Create new entries
            foreach (NPCData npcData in newWantedList)
            {
                CreateWantedPersonEntry(npcData);
            }
        }
        else
        {
            LogDebug("Detected individual suspect removal - not clearing entries");
            // For individual removals, just create any missing entries
            // (This handles edge cases but shouldn't normally be needed)
            foreach (NPCData npcData in newWantedList)
            {
                if (!activeEntries.ContainsKey(npcData))
                {
                    CreateWantedPersonEntry(npcData);
                }
            }
        }
    }

    /// <summary>
    /// Called when a suspect has been apprehended and should be crossed out
    /// </summary>
    private void OnSuspectApprehended(NPCData apprehendedNPC)
    {
        LogDebug($"Suspect apprehended: {apprehendedNPC.npcName}");
        LogDebug($"Current active entries count: {activeEntries.Count}");

        if (activeEntries.ContainsKey(apprehendedNPC))
        {
            WantedPersonEntry entry = activeEntries[apprehendedNPC];
            LogDebug($"Found entry for {apprehendedNPC.npcName}, starting animation");
            entry.MarkAsApprehended();
        }
        else
        {
            Debug.LogWarning($"[WantedListUIController] No UI entry found for apprehended suspect: {apprehendedNPC.npcName}");
            // List all active entries for debugging
            LogDebug("Current active entries:");
            foreach (var kvp in activeEntries)
            {
                LogDebug($"  - {kvp.Key.npcName}");
            }
        }
    }

    /// <summary>
    /// Creates a UI entry for a wanted person
    /// </summary>
    private void CreateWantedPersonEntry(NPCData npcData)
    {
        if (wantedPersonPrefab == null || content == null)
        {
            Debug.LogError("[WantedListUIController] Missing prefab or content references!");
            return;
        }

        // Check if entry already exists to prevent duplicates
        if (activeEntries.ContainsKey(npcData))
        {
            LogDebug($"Entry already exists for {npcData.npcName} - skipping creation");
            return;
        }

        // Instantiate the prefab
        GameObject entryObject = Instantiate(wantedPersonPrefab, content);
        WantedPersonEntry entry = entryObject.GetComponent<WantedPersonEntry>();

        if (entry != null)
        {
            // Initialize the entry
            entry.Initialize(npcData);

            // Subscribe to animation completion
            entry.OnAnimationComplete += OnEntryAnimationComplete;

            // Store reference
            activeEntries[npcData] = entry;

            LogDebug($"Created wanted person entry for {npcData.npcName}");
        }
        else
        {
            Debug.LogError("[WantedListUIController] WantedPersonEntry component not found on prefab!");
            Destroy(entryObject);
        }
    }

    /// <summary>
    /// Called when a wanted person entry has finished its animation and should be removed
    /// </summary>
    private void OnEntryAnimationComplete(WantedPersonEntry entry)
    {
        NPCData npcData = entry.GetAssociatedNPCData();
        LogDebug($"Animation complete for {npcData?.npcName}");

        // Unsubscribe from the event
        entry.OnAnimationComplete -= OnEntryAnimationComplete;

        // Remove from our tracking
        if (npcData != null && activeEntries.ContainsKey(npcData))
        {
            activeEntries.Remove(npcData);
        }

        // Tell the wanted list manager that the removal is complete
        if (WantedListManager.Instance != null && npcData != null)
        {
            WantedListManager.Instance.CompleteSuspectRemoval(npcData);
        }

        // Destroy the UI entry
        Destroy(entry.gameObject);
    }

    /// <summary>
    /// Clears all wanted person entries
    /// </summary>
    private void ClearAllEntries()
    {
        foreach (var kvp in activeEntries)
        {
            WantedPersonEntry entry = kvp.Value;
            if (entry != null)
            {
                // Unsubscribe from events
                entry.OnAnimationComplete -= OnEntryAnimationComplete;

                // Force remove the entry
                entry.ForceRemove();
            }
        }

        activeEntries.Clear();
    }

    private void LogDebug(string message)
    {
        if (showDebugMessages)
        {
            Debug.Log($"[WantedListUIController] {message}");
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (WantedListManager.Instance != null)
        {
            WantedListManager.Instance.OnWantedListUpdated -= UpdateWantedListUI;
            WantedListManager.Instance.OnSuspectApprehended -= OnSuspectApprehended;
        }

        // Clean up any remaining entries
        ClearAllEntries();
    }
}