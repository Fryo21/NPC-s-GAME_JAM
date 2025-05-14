using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("NPC Selection")]
    public GameObject[] npcPrefabs;
    public Transform[] spawnPoints;

    [Header("Game Logic")]
    public List<NPCClassType> wantedClasses = new List<NPCClassType>();
    public int wantedCount = 0;

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
        StartGame();
    }
    public void StartGame()
    {
        var classList = System.Enum.GetValues(typeof(NPCClassType)).Cast<NPCClassType>().ToList();
        wantedClasses = classList.OrderBy(x => Random.value).Take(wantedCount).ToList(); // Randomly select 3 classes from the classList
        // Randomly select 3 classes from the classList
        for (int i = 0; i < 20; i++)
        {
            GameObject npc = Instantiate(npcPrefabs[Random.Range(0, npcPrefabs.Length)],
                                         spawnPoints[Random.Range(0, spawnPoints.Length)].position,
                                         Quaternion.identity);
        }
    }
}
