using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DroneManager : MonoBehaviour
{
    public static DroneManager Instance { get; private set; }

    [Header("Drone Settings")]
    [SerializeField] private GameObject droneUIPopupPrefab;
    [SerializeField] private Transform droneUIParent;
    [SerializeField] private float baseDroneCost = 75f;
    [SerializeField] private float droneCostMultiplier = 1.5f;
    [SerializeField] private float droneAnalysisInterval = 8f; // How often drones scan for suspects

    [Header("Accuracy Settings")]
    [Range(0f, 1f)][SerializeField] private float baseAccuracy = 0.75f;
    [Range(0f, 1f)][SerializeField] private float accuracyDecreasePerDrone = 0.1f; // Each drone reduces overall accuracy

    // List of active drones and NPCs
    private List<DroneController> activeDrones = new List<DroneController>();
    private List<NPCDataHolder> activeNPCs = new List<NPCDataHolder>();

    // Event for UI to subscribe to
    public delegate void DroneUIEvent(DroneController drone, NPCDataHolder target, NPCData reportedAs, float timeLimit);
    public static event DroneUIEvent OnDroneIdentification;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Subscribe to round events
        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.OnRoundStarted += OnRoundStarted;
            RoundManager.Instance.OnRoundEnded += OnRoundEnded;
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.OnRoundStarted -= OnRoundStarted;
            RoundManager.Instance.OnRoundEnded -= OnRoundEnded;
        }
    }

    private void OnRoundStarted(int roundNumber)
    {
        // Resume all drones
        foreach (var drone in activeDrones)
        {
            drone.StartScanning();
        }
    }

    private void OnRoundEnded()
    {
        // Pause all drones
        foreach (var drone in activeDrones)
        {
            drone.StopScanning();
        }
    }

    // Used by NPCs to register themselves when spawned
    public void RegisterNPC(NPCDataHolder npc)
    {
        if (npc != null && !activeNPCs.Contains(npc))
        {
            activeNPCs.Add(npc);
        }
    }

    // Used by NPCs to unregister when destroyed
    public void UnregisterNPC(NPCDataHolder npc)
    {
        if (npc != null && activeNPCs.Contains(npc))
        {
            activeNPCs.Remove(npc);
        }
    }

    // Get NPCs by their class (used by drones)
    public List<NPCDataHolder> GetNPCsByClass(NPCClass targetClass)
    {
        return activeNPCs
            .Where(n => n != null && n.nPCData != null && n.nPCData.nPCClass == targetClass)
            .ToList();
    }

    // Get all active NPCs
    public List<NPCDataHolder> GetAllActiveNPCs()
    {
        return new List<NPCDataHolder>(activeNPCs);
    }

    // Purchase a new drone
    public bool PurchaseDrone()
    {
        // Calculate cost based on current drone count
        float cost = baseDroneCost * Mathf.Pow(droneCostMultiplier, activeDrones.Count);

        // Check if player can afford
        if (MoneyManager.Instance != null && MoneyManager.Instance.CanAffordDrone())
        {
            // Deduct money
            MoneyManager.Instance.SubtractMoney(cost);

            // Create drone UI
            CreateDroneUI();

            return true;
        }

        Debug.Log($"Cannot afford drone. Cost: ${cost}");
        return false;
    }

    // Create a new drone UI element
    private void CreateDroneUI()
    {
        if (droneUIPopupPrefab == null)
        {
            Debug.LogError("Drone UI Popup prefab not assigned!");
            return;
        }

        // Create the drone UI GameObject
        GameObject droneUI = Instantiate(droneUIPopupPrefab, droneUIParent);

        // Get or add the drone controller
        DroneController droneController = droneUI.GetComponent<DroneController>();
        if (droneController == null)
        {
            droneController = droneUI.AddComponent<DroneController>();
        }

        // Initialize the drone with decreasing accuracy for each drone
        float droneAccuracy = Mathf.Max(0.3f, baseAccuracy - (activeDrones.Count * accuracyDecreasePerDrone));
        droneController.Initialize(activeDrones.Count + 1, droneAccuracy, droneAnalysisInterval);

        // Add to active drones list
        activeDrones.Add(droneController);

        Debug.Log($"Drone #{activeDrones.Count} created with accuracy {droneAccuracy:P0}");
    }

    public void NotifyTargetArrested(NPCDataHolder arrestedNPC)
    {
        // Notify all drones if their current target was arrested
        foreach (var drone in activeDrones)
        {
            drone.CheckIfTargetWasArrested(arrestedNPC);
        }

        // Check if all wanted suspects have been arrested
        if (WantedListManager.Instance != null)
        {
            var wantedList = WantedListManager.Instance.GetCurrentWantedList();
            if (wantedList.Count == 0)
            {
                // All suspects arrested - end the round
                Debug.Log("[DroneManager] All suspects arrested! Ending round early.");
            }
        }
    }

    // Called by DroneController when it has identified a suspect
    public void ReportDroneIdentification(DroneController drone, NPCDataHolder target, NPCData reportedAs, float timeLimit)
    {
        // Trigger event for UI to handle
        OnDroneIdentification?.Invoke(drone, target, reportedAs, timeLimit);
    }

    // Called when player responds to a drone identification
    public void ProcessDroneIdentificationResponse(DroneController drone, NPCDataHolder target, bool playerConfirmed)
    {
        if (target == null) return;

        // Check if the NPC was actually on the wanted list
        bool isActuallyWanted = WantedListManager.Instance.IsWanted(target.nPCData);

        if (playerConfirmed)
        {
            // Player confirmed the arrest - check if correct
            if (isActuallyWanted)
            {
                // Correct arrest
                if (RoundManager.Instance != null)
                {
                    RoundManager.Instance.ReportArrest(true);
                }

                // Notify other drones that this target was arrested
                NotifyTargetArrested(target);

                // Remove NPC
                Destroy(target.gameObject);

                // Remove from wanted list
                var currentList = WantedListManager.Instance.GetCurrentWantedList();
                currentList.Remove(target.nPCData);
                WantedListManager.Instance.UpdateWantedList(currentList);
            }
            else
            {
                // Incorrect arrest
                if (RoundManager.Instance != null)
                {
                    RoundManager.Instance.ReportArrest(false);
                }
            }
        }
        else
        {
            // Player denied the arrest - no consequences
            Debug.Log("Player denied the drone identification");
        }
    }


    // Get current number of drones
    public int GetDroneCount()
    {
        return activeDrones.Count;
    }

    // Get the cost of the next drone
    public float GetNextDroneCost()
    {
        return baseDroneCost * Mathf.Pow(droneCostMultiplier, activeDrones.Count);
    }
}