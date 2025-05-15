using UnityEngine;

public class NPCSelectable : MonoBehaviour
{
    private NPCDataHolder dataHolder;

    private void Start()
    {
        dataHolder = GetComponent<NPCDataHolder>();
    }

    private void OnMouseDown()
    {
        if (GameManager.Instance != null && RoundManager.Instance != null)
        {
            // Only allow selection during playing state
            if (RoundManager.Instance.CurrentState == GameState.Playing)
            {
                GameManager.Instance.CheckNPCSelection(gameObject);
            }
        }
    }
}