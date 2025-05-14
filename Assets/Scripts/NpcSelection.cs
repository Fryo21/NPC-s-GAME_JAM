using UnityEngine;

public class NpcSelection : MonoBehaviour
{
    public void OnMouseDown()
    {
        // Check if mouse is selected
        Debug.Log("Mouse clicked on NPC");
    }
    public void OnYesClicked()
    {
        Debug.Log("Yes clicked");
    }
    public void OnNoClicked()
    {
        Debug.Log("No clicked");
    }
}

