using UnityEngine;

public class NpcSelection : MonoBehaviour
{
    public void OnMouseDown()
    {
        // Check if mouse is selected
        Debug.Log("Mouse clicked on NPC");
        this.name = this.name.Replace("(Clone)", "").Trim();
        GameManager.Instance.ConfirmSelection(this.gameObject, this.gameObject.name);
    }
    public void OnYesClicked()
    {
        Debug.Log("Yes clicked");
        this.name = this.name.Replace("(Clone)", "").Trim();
        GameManager.Instance.ConfirmSelection(this.gameObject, this.gameObject.name);
    }
    public void OnNoClicked()
    {
        Debug.Log("No clicked");
    }
}

