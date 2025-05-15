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

    // Event for when the wanted list changes
    public event Action<List<NPCData>> OnWantedListUpdated;

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

        LogDebug($"Generated wanted list for round {roundNumber} with {currentWantedList.Count} suspects");
        foreach (NPCData npc in currentWantedList)
        {
            LogDebug($"Wanted: {npc.npcName} (Class: {npc.nPCClass}, SubClass: {npc.npcSubClass})");
        }

        // Notify listeners that the wanted list has been updated
        OnWantedListUpdated?.Invoke(currentWantedList);
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

    public Dictionary<NPCClass, int> GetWantedClassCounts()
    {
        return new Dictionary<NPCClass, int>(wantedClassCounts);
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