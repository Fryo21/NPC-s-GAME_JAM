using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class DroneAIManager : MonoBehaviour
{
    [Range(0f, 1f)]
    public float droneAccuracy = 0.75f; // Global drone accuracy setting

    public static DroneAIManager Instance { get; private set; }

    public delegate void DroneTargetAcquiredEvent(NPCDataHolder target, bool isWanted);
    public static event DroneTargetAcquiredEvent OnDroneTargetAcquired;

    private List<NPCClass> assignedClasses = new List<NPCClass>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void ActivateDrone()
    {
        List<NPCData> wantedList = WantedListManager.Instance.GetCurrentWantedList();

        if (wantedList == null || wantedList.Count == 0)
        {
            Debug.Log("Wanted list is empty. Drone cannot proceed.");
            return;
        }

        // Filter out already assigned classes
        var unassignedClasses = wantedList
            .Select(n => n.nPCClass)
            .Distinct()
            .Where(c => !assignedClasses.Contains(c))
            .ToList();

        // Get all wanted classes
        var wantedClasses = wantedList.Select(npc => npc.nPCClass).Distinct().ToList();

        // Randomly pick one class for this drone to scan
        NPCClass selectedClass = wantedClasses[Random.Range(0, wantedClasses.Count)];

        // Find all NPCs of that class currently in the scene
        List<NPCDataHolder> matchingNPCs = DroneManager.Instance.GetNPCsByClass(selectedClass);

        if (matchingNPCs.Count == 0)
        {
            Debug.Log($"No NPCs of class {selectedClass} found in the scene.");
            return;
        }

        // With chance based on accuracy, pick a correct or wrong NPC
        NPCDataHolder selectedTarget = SelectTargetWithAccuracy(matchingNPCs, wantedList);

        //- Show result (You can replace this with UI highlight or trigger event)
        bool isWanted = WantedListManager.Instance.IsWanted(selectedTarget.nPCData);
        Debug.Log($"Drone selected: {selectedTarget.nPCData.npcName} - IsWanted: {isWanted}");

        OnDroneTargetAcquired?.Invoke(selectedTarget, isWanted);

        // TODO: Show UI to player to decide to arrest or not
    }

    private NPCDataHolder SelectTargetWithAccuracy(List<NPCDataHolder> candidates, List<NPCData> wantedList)
    {
        bool chooseCorrectly = Random.value <= droneAccuracy;

        if (chooseCorrectly)
        {
            // Pick a correct NPC if any are in the scene
            var correctCandidates = candidates.Where(npc => wantedList.Contains(npc.nPCData)).ToList();
            if (correctCandidates.Count > 0)
                return correctCandidates[Random.Range(0, correctCandidates.Count)];
        }

        // If choosing incorrectly or no correct ones available, pick a random (possibly wrong) candidate
        return candidates[Random.Range(0, candidates.Count)];
    }
}
