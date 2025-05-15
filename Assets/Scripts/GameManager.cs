using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.SpeedTree.Importer;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("NPC Selection")]
    public GameObject[] npcPrefabs;
    public Transform[] spawnPoints;

    [Header("Game Rules")]
    public int criminalsToFind = 5;
    public int totalCriminalsToFind = 10;

    [Header("Game Logic")]
    private Dictionary<string, int> npcTypeCount = new Dictionary<string, int>();
    public List<string> wantedCriminals = new List<string>();

    private Dictionary<string, int> targetPerNPC = new Dictionary<string, int>();
    //public int wantedCount = 0;

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
        SpawnNpc();
    }
    public void SpawnNpc()
    {
        for (int i = 0; i < spawnPoints.Length; i++)
        {     
            GameObject criminalType = (npcPrefabs[Random.Range(0, npcPrefabs.Length)]);

            GameObject npc = Instantiate(criminalType, spawnPoints[i].position, Quaternion.identity);

            string npcName = npc.name.Replace("(Clone)", "").Trim();
            if (npcTypeCount.ContainsKey(npcName))
            {
                npcTypeCount[npcName]++;
            }
            else
            {
                npcTypeCount[npcName] = 1;
            }

            foreach (var entry in npcTypeCount)
            {
                Debug.Log($"NPC Class: {entry.Key}, Count: {entry.Value}");
            }   
        }
        wantedCriminals = npcTypeCount
                        .OrderBy(entry => entry.Value)
                        .Take(criminalsToFind)
                        .Select(entry => entry.Key)
                        .ToList();

        targetPerNPC = CalculateTartgetPerNPC(npcTypeCount, wantedCriminals, totalCriminalsToFind);

        Debug.Log("=== Target NPC Count Breakdown ===");
        foreach (var kvp in targetPerNPC)
        {
            Debug.Log($"Target NPC: {kvp.Key}, Assigned Count: {kvp.Value}");
        }
        Debug.Log("=== END ===");

               Debug.Log("=== Spawned NPCs ===");
        Debug.Log("Wanted Criminals: " + string.Join(", ", wantedCriminals));

        // Display the npc preset in the console which you are looking for and how many of them
        // Send a message to the UI manager to update the UI with the wanted criminals
    }
    public Dictionary<string, int> CalculateTartgetPerNPC(Dictionary<string, int> npcTypeCount, List<string> wantedCriminals, int totalCriminalsToFind)
    {
        Dictionary<string, int> targetPerNPC = new Dictionary<string, int>();

        int sumTargetSpawned = wantedCriminals.Sum(tc => npcTypeCount.ContainsKey(tc) ? npcTypeCount[tc] : 0);

        if (sumTargetSpawned == 0)
        {
            int evenCount = Mathf.RoundToInt(totalCriminalsToFind / wantedCriminals.Count);
            foreach (var cls in wantedCriminals)
            {
                targetPerNPC[cls] = evenCount;
            }
            return targetPerNPC;
        }

        int assignedCriminals = 0;
        
        foreach (var cls in wantedCriminals)
        {
            int count = 0;

            if (npcTypeCount.ContainsKey(cls))
            {
                float ratio = (float)npcTypeCount[cls] / sumTargetSpawned;

                count = Mathf.RoundToInt(ratio * totalCriminalsToFind);
            }
            targetPerNPC[cls] = count;

            assignedCriminals += count;
        }
        int remainingCriminals = totalCriminalsToFind - assignedCriminals;

        if (remainingCriminals != 0 && targetPerNPC.Count > 0)
        {
            string lastNpc = targetPerNPC.Keys.Last();

            targetPerNPC[lastNpc] += remainingCriminals;

            if (targetPerNPC[lastNpc] < 0)
            {
                targetPerNPC[lastNpc] = 0;
            }
        }
        return targetPerNPC;
    }
    public void ConfirmSelection(GameObject criminal, string npcName)
    {
        //Check if the selected NPC is in the wanted list
        if (wantedCriminals.Contains(npcName))
        {
            Debug.Log("Wanted Criminal: " + npcName);
            // Check the targetPerNPC dictionary to see how many of this NPC type are needed and update the count
            if (targetPerNPC.ContainsKey(npcName))
            {
                int npcCount = targetPerNPC[npcName];
                if (npcCount > 0)
                {
                    targetPerNPC[npcName] = targetPerNPC[npcName] - 1;
                    Debug.Log($"Confirmed selection of {npcName}. Remaining count: {targetPerNPC[npcName]}");

                    //remove the NPC from the scene
                    Destroy(criminal);

                    // Update the UI to remove minus one from the count of this NPC type

                    // Check if the list is completed
                    if (IsListCompleted())
                    {
                        Debug.Log("All NPCs found!");
                        // Handle Game Over logic
                    }
                    else
                    {
                        Debug.Log("NPCs still needed.");
                    }
                }
                else
                {
                    Debug.Log($"No more {npcName} needed.");
                }
            }
            else
            {
                Debug.Log("NPC not found in target list.");
            }
        }
        else
        {
            Debug.Log("Incorrect NPC selected");
            return;
        }
    }
    public bool IsListCompleted()
    {
        foreach (var target in targetPerNPC)
        {
            if (target.Value > 0)
            {
                return false;
            }
        }
        return true;
    }

    
}
