using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("NPC Spawning")]
    [SerializeField] private GameObject npcPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private NPCData[] allNPCDataAssets; // Assign all your NPCData scriptable objects here
    [SerializeField] private int npcSpawnCount = 30;     // How many NPCs to spawn in total

    [Header("References")]
    [SerializeField] private WantedListManager wantedListManager;
    [SerializeField] private RoundManager roundManager;

    [Header("Game Settings")]
    [SerializeField] private float moneyForCorrectArrest = 50f;
    [SerializeField] private float penaltyForWrongArrest = 30f;

    // List of spawned NPCs
    private List<GameObject> spawnedNPCs = new List<GameObject>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Initialize WantedListManager with all NPCData assets
        if (wantedListManager == null)
        {
            wantedListManager = WantedListManager.Instance;
        }

        if (wantedListManager != null)
        {
            wantedListManager.Initialize(new List<NPCData>(allNPCDataAssets));
        }
        else
        {
            Debug.LogError("WantedListManager not found!");
        }

        // Find RoundManager if not set
        if (roundManager == null)
        {
            roundManager = RoundManager.Instance;
        }

        // Subscribe to round events
        if (roundManager != null)
        {
            roundManager.OnRoundStarted += OnRoundStarted;
            roundManager.OnRoundEnded += OnRoundEnded;
        }
        else
        {
            Debug.LogError("RoundManager not found!");
        }

        // Initial NPC spawning
        SpawnNPCs();
    }

    private void OnRoundStarted(int roundNumber)
    {
        // Spawn NPCs when a new round starts
        SpawnNPCs();
    }

    private void OnRoundEnded()
    {
        // Clean up NPCs from previous round
        ClearNPCs();
    }

    private void SpawnNPCs()
    {
        ClearNPCs(); // Clear any existing NPCs first

        // Ensure we have valid data
        if (npcPrefab == null || allNPCDataAssets == null || allNPCDataAssets.Length == 0)
        {
            Debug.LogError("Missing NPC prefab or NPCData assets!");
            return;
        }

        // Choose which spawn points to use
        Transform[] selectedSpawnPoints = spawnPoints;
        if (npcSpawnCount < spawnPoints.Length)
        {
            // Randomly select spawn points if we have more points than NPCs to spawn
            selectedSpawnPoints = spawnPoints.OrderBy(x => Random.value).Take(npcSpawnCount).ToArray();
        }

        // Determine how many NPCs of each class we need to ensure all are represented
        List<NPCData> dataToUse = new List<NPCData>(allNPCDataAssets);

        // Limit to the number we can spawn
        int actualSpawnCount = Mathf.Min(npcSpawnCount, selectedSpawnPoints.Length, dataToUse.Count);

        // Shuffle the NPCData assets
        dataToUse = dataToUse.OrderBy(x => Random.value).ToList();

        // Spawn NPCs
        for (int i = 0; i < actualSpawnCount; i++)
        {
            Vector3 spawnPosition = selectedSpawnPoints[i % selectedSpawnPoints.Length].position;

            // Create the NPC
            GameObject npc = Instantiate(npcPrefab, spawnPosition, Quaternion.identity);

            // Assign NPCData
            NPCDataHolder dataHolder = npc.GetComponent<NPCDataHolder>();
            if (dataHolder != null)
            {
                NPCData npcData = dataToUse[i % dataToUse.Count];
                dataHolder.nPCData = npcData;

                // Update sprite and animator
                UpdateNPCVisuals(npc, npcData);
            }

            spawnedNPCs.Add(npc);
        }

        Debug.Log($"Spawned {spawnedNPCs.Count} NPCs");
    }

    private void UpdateNPCVisuals(GameObject npc, NPCData npcData)
    {
        // Update sprite renderer
        SpriteRenderer spriteRenderer = npc.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && npcData.npcSprite != null)
        {
            spriteRenderer.sprite = npcData.npcSprite;
        }

        // Update animator controller
        Animator animator = npc.GetComponent<Animator>();
        if (animator != null && npcData.npcAnimatorController != null)
        {
            animator.runtimeAnimatorController = npcData.npcAnimatorController;
        }
    }

    private void ClearNPCs()
    {
        foreach (GameObject npc in spawnedNPCs)
        {
            if (npc != null)
            {
                Destroy(npc);
            }
        }
        spawnedNPCs.Clear();
    }

    public void CheckNPCSelection(GameObject selectedNPC)
    {
        // Find the WantedListManager if not already cached
        if (wantedListManager == null)
        {
            wantedListManager = WantedListManager.Instance;
            if (wantedListManager == null)
            {
                Debug.LogError("WantedListManager not found!");
                return;
            }
        }

        NPCDataHolder dataHolder = selectedNPC.GetComponent<NPCDataHolder>();
        if (dataHolder == null || dataHolder.nPCData == null) return;

        // Check if the selected NPC is in the wanted list
        bool isWanted = wantedListManager.IsWanted(dataHolder.nPCData);

        // Report to RoundManager
        if (roundManager != null)
        {
            roundManager.ReportArrest(isWanted);
        }

        // If it's a correct arrest, remove the NPC
        if (isWanted)
        {
            spawnedNPCs.Remove(selectedNPC);
            Destroy(selectedNPC);

            // Remove from wanted list - use the public method instead of accessing the field directly
            var currentList = wantedListManager.GetCurrentWantedList();
            currentList.Remove(dataHolder.nPCData);

            // Use the event to update the wanted list
            wantedListManager.UpdateWantedList(currentList);
        }

        Debug.Log($"Player selected {dataHolder.nPCData.npcName}. Wanted? {isWanted}");
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (roundManager != null)
        {
            roundManager.OnRoundStarted -= OnRoundStarted;
            roundManager.OnRoundEnded -= OnRoundEnded;
        }
    }
}