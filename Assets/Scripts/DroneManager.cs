using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DroneManager : MonoBehaviour
{
    public static DroneManager Instance;

    private List<NPCDataHolder> activeNPC = new List<NPCDataHolder>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void RegisterNPC(NPCDataHolder npc)
    {
        if (!activeNPC.Contains(npc))
        {
            activeNPC.Add(npc);
        }
    }
    public void UnregisterNPC(NPCDataHolder npc)
    {
        if (activeNPC.Contains(npc))
        {
            activeNPC.Remove(npc);
        }
    }
    public List<NPCDataHolder> GetNPCsByClass(NPCClass targetClass)
    {
        return activeNPC
            .Where(n => n.nPCData != null && n.nPCData.nPCClass == targetClass)
            .ToList();
    }

    
}
