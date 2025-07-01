using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class WantedListManager : MonoBehaviour
{
    public static WantedListManager Instance { get; private set; }

    [Header("Wanted List Settings")]
    [SerializeField] private int baseWantedCount = 3;          // Number of wanted persons in first round
    [SerializeField] private int wantedCountIncreasePerRound = 2;  // Increase in each subsequent round

    [Header("Debug")]
    [SerializeField] private bool showDebugMessages = true;

    // All available NPC data assets
    private List<NPCData> allNPCData = new List<NPCData>();

    // Current wanted list for the round
    public List<NPCData> currentWantedList = new List<NPCData>();

    // Class to count mapping (for UI and logic)
    private Dictionary<NPCClass, int> wantedClassCounts = new Dictionary<NPCClass, int>();

    // Track which NPCs are pending removal (animation playing)
    private HashSet<NPCData> pendingRemovalNPCs = new HashSet<NPCData>();

    // Event for when the wanted list changes
    public event Action<List<NPCData>> OnWantedListUpdated;

    // Event for when a suspect should be marked as apprehended (for UI)
    public event Action<NPCData> OnSuspectApprehended;

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Subscribe to round manager events
        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.OnRoundStarted += GenerateWantedListForRound;
        }
        else
        {
            Debug.LogError("RoundManager not found in the scene!");
        }
    }

    public void Initialize(List<NPCData> availableNPCData)
    {
        allNPCData = availableNPCData;
        LogDebug($"WantedListManager initialized with {allNPCData.Count} NPC data assets.");
    }

    public void GenerateWantedListForRound(int roundNumber)
    {
        // Clear previous wanted list
        currentWantedList.Clear();
        wantedClassCounts.Clear();
        pendingRemovalNPCs.Clear();

        // Calculate how many wanted persons for this round
        int wantedCount = baseWantedCount + (roundNumber - 1) * wantedCountIncreasePerRound;

        // Group NPC data by class
        var npcDataByClass = allNPCData.GroupBy(npc => npc.nPCClass)
                                        .ToDictionary(g => g.Key, g => g.ToList());

        // Get unique classes
        List<NPCClass> availableClasses = npcDataByClass.Keys.ToList();

        // Ensure we have enough classes
        if (availableClasses.Count < wantedCount)
        {
            LogDebug($"Warning: Not enough unique NPC classes available. Wanted: {wantedCount}, Available: {availableClasses.Count}");
            wantedCount = Mathf.Min(wantedCount, availableClasses.Count);
        }

        // Randomly select classes for wanted list
        List<NPCClass> selectedClasses = new List<NPCClass>();
        for (int i = 0; i < wantedCount; i++)
        {
            if (availableClasses.Count == 0) break;

            int randomIndex = UnityEngine.Random.Range(0, availableClasses.Count);
            NPCClass selectedClass = availableClasses[randomIndex];
            selectedClasses.Add(selectedClass);
            availableClasses.RemoveAt(randomIndex);
        }

        // For each selected class, pick a random NPC data of that class
        foreach (NPCClass npcClass in selectedClasses)
        {
            List<NPCData> npcsOfClass = npcDataByClass[npcClass];
            if (npcsOfClass.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, npcsOfClass.Count);
                NPCData selectedNPC = npcsOfClass[randomIndex];
                currentWantedList.Add(selectedNPC);

                // Update class counts for UI
                if (wantedClassCounts.ContainsKey(npcClass))
                    wantedClassCounts[npcClass]++;
                else
                    wantedClassCounts[npcClass] = 1;
            }
        }

        // Update RoundManager with the total suspects for this round
        if (RoundManager.Instance != null)
        {
            // Using reflection to set the private field
            var totalSuspectsField = typeof(RoundManager).GetProperty("TotalSuspectsForThisRound");
            if (totalSuspectsField != null)
            {
                totalSuspectsField.SetValue(RoundManager.Instance, currentWantedList.Count);
            }
        }

        // Notify listeners that the wanted list has been updated
        OnWantedListUpdated?.Invoke(currentWantedList);
    }

    /// <summary>
    /// Call this when a suspect has been arrested to start the apprehension process
    /// This will trigger the animation and then remove them from the list
    /// </summary>
    public void ApprehendSuspect(NPCData arrestedNPC)
    {
        if (arrestedNPC == null || !currentWantedList.Contains(arrestedNPC))
        {
            LogDebug($"Attempted to apprehend NPC that's not on wanted list: {arrestedNPC?.npcName}");
            return;
        }

        if (pendingRemovalNPCs.Contains(arrestedNPC))
        {
            LogDebug($"NPC {arrestedNPC.npcName} is already being processed for removal");
            return;
        }

        LogDebug($"Starting apprehension process for {arrestedNPC.npcName}");

        // Mark as pending removal so we don't process them again
        pendingRemovalNPCs.Add(arrestedNPC);

        // Immediately update stats (player gets credit right away)
        UpdatePlayerStats(arrestedNPC);

        // Trigger the UI animation
        OnSuspectApprehended?.Invoke(arrestedNPC);

        // The actual removal from the list will happen when the animation completes
        // This should be called by the UI after the animation finishes
    }

    /// <summary>
    /// Call this method when the animation has finished and the suspect should be fully removed
    /// This should be called by the UI component after the cross-out animation completes
    /// </summary>
    public void CompleteSuspectRemoval(NPCData arrestedNPC)
    {
        if (arrestedNPC == null || !pendingRemovalNPCs.Contains(arrestedNPC))
        {
            LogDebug($"Attempted to complete removal for NPC not pending removal: {arrestedNPC?.npcName}");
            return;
        }

        LogDebug($"Completing removal of {arrestedNPC.npcName} from wanted list");

        // Remove from both the pending list and the actual wanted list
        pendingRemovalNPCs.Remove(arrestedNPC);
        currentWantedList.Remove(arrestedNPC);

        // Update class counts
        if (wantedClassCounts.ContainsKey(arrestedNPC.nPCClass))
        {
            wantedClassCounts[arrestedNPC.nPCClass]--;
            if (wantedClassCounts[arrestedNPC.nPCClass] <= 0)
            {
                wantedClassCounts.Remove(arrestedNPC.nPCClass);
            }
        }

        // Check if all suspects have been apprehended
        if (currentWantedList.Count <= 0)
        {
            LogDebug("All suspects apprehended! Ending round early.");
            if (RoundManager.Instance != null)
            {
                RoundManager.Instance.EndRoundEarly();
            }
        }

        // Update UI
        if (GameUIController.Instance != null)
        {
            GameUIController.Instance.UpdateArrestQuotaUI();
        }

        // DON'T notify OnWantedListUpdated here - let the UI handle individual removals
        // OnWantedListUpdated?.Invoke(GetCurrentWantedList());
    }

    /// <summary>
    /// Updates player stats when a suspect is apprehended
    /// </summary>
    private void UpdatePlayerStats(NPCData arrestedNPC)
    {
        // Add your player stats update logic here
        // For example, increase score, update arrest count, etc.
        LogDebug($"Updated player stats for arresting {arrestedNPC.npcName}");

        // Example: Notify other systems about the successful arrest
        // ScoreManager.Instance?.AddArrestScore(arrestedNPC);
        // PlayerStatsManager.Instance?.IncrementArrestCount();
    }

    public void UpdateWantedList(List<NPCData> updatedList)
    {
        // Update the current wanted list
        currentWantedList = new List<NPCData>(updatedList);

        // Recalculate class counts
        wantedClassCounts.Clear();
        foreach (NPCData npc in currentWantedList)
        {
            if (wantedClassCounts.ContainsKey(npc.nPCClass))
                wantedClassCounts[npc.nPCClass]++;
            else
                wantedClassCounts[npc.nPCClass] = 1;
        }

        // Notify listeners
        OnWantedListUpdated?.Invoke(currentWantedList);

        if (wantedClassCounts.Count <= 0)
        {
            // End the round early if everyone is arrested
            if (RoundManager.Instance != null)
            {
                RoundManager.Instance.EndRoundEarly();
            }
        }

        if (GameUIController.Instance != null)
        {
            GameUIController.Instance.UpdateArrestQuotaUI();
        }
        else
        {
            LogDebug("GameUIController.Instance is null. Cannot update Arrest Quota UI.");
        }
    }

    public bool IsWanted(NPCData npcData)
    {
        return currentWantedList.Contains(npcData);
    }

    public bool IsSameClass(NPCData npc1, NPCData npc2)
    {
        return npc1.nPCClass == npc2.nPCClass;
    }

    public bool IsSamePerson(NPCData npc1, NPCData npc2)
    {
        // Check if both the class and subclass match (or if it's the same scriptable object)
        return npc1 == npc2 || (npc1.nPCClass == npc2.nPCClass && npc1.npcSubClass == npc2.npcSubClass);
    }

    public List<NPCData> GetCurrentWantedList()
    {
        return new List<NPCData>(currentWantedList);
    }

    /// <summary>
    /// Get the wanted list including suspects that are pending removal (still animating)
    /// </summary>
    public List<NPCData> GetWantedListIncludingPending()
    {
        var fullList = new List<NPCData>(currentWantedList);
        fullList.AddRange(pendingRemovalNPCs);
        return fullList;
    }

    public Dictionary<NPCClass, int> GetWantedClassCounts()
    {
        return new Dictionary<NPCClass, int>(wantedClassCounts);
    }

    /// <summary>
    /// Check if a suspect is currently being processed for removal (animation playing)
    /// </summary>
    public bool IsPendingRemoval(NPCData npcData)
    {
        return pendingRemovalNPCs.Contains(npcData);
    }

    private void LogDebug(string message)
    {
        if (showDebugMessages)
        {
            Debug.Log($"[WantedListManager] {message}");
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.OnRoundStarted -= GenerateWantedListForRound;
        }
    }
}