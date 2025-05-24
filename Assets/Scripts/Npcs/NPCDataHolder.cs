using UnityEngine;

public class NPCDataHolder : MonoBehaviour
{
    public NPCData nPCData;

    private void Start()
    {
        // Register with DroneManager
        if (DroneManager.Instance != null)
        {
            DroneManager.Instance.RegisterNPC(this);
        }
    }

    private void OnDestroy()
    {
        // Unregister when destroyed
        if (DroneManager.Instance != null)
        {
            DroneManager.Instance.UnregisterNPC(this);
        }
    }
}