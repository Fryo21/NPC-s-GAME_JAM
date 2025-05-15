using UnityEngine;
using UnityEngine.UI;

public class NPCSelectable : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Canvas selectionCanvas;   // The world space canvas with buttons
    [SerializeField] private Button arrestButton;      // The arrest button
    [SerializeField] private Button cancelButton;      // The cancel button

    private NPCDataHolder dataHolder;
    private bool isSelected = false;

    private void Awake()
    {
        dataHolder = GetComponent<NPCDataHolder>();

        // Hide the selection canvas initially
        if (selectionCanvas != null)
        {
            selectionCanvas.gameObject.SetActive(false);
        }

    }

    private void OnMouseDown()
    {
        Debug.Log("NPC clicked: " + gameObject.name);
        if (GameManager.Instance == null) return;


        // Toggle selection state
        isSelected = !isSelected;

        // Show/hide selection canvas
        if (selectionCanvas != null)
        {
            selectionCanvas.gameObject.SetActive(isSelected);
        }
    }

    public void OnArrestButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            // Process the arrest
            GameManager.Instance.CheckNPCSelection(gameObject);

            // Hide selection canvas
            isSelected = false;
            if (selectionCanvas != null)
            {
                selectionCanvas.gameObject.SetActive(false);
            }
        }
    }

    public void OnCancelButtonClicked()
    {
        // Just hide the selection canvas
        isSelected = false;
        if (selectionCanvas != null)
        {
            selectionCanvas.gameObject.SetActive(false);
        }
    }

    // Optional: Hide selection UI when NPC is disabled/destroyed
    private void OnDisable()
    {
        if (selectionCanvas != null)
        {
            selectionCanvas.gameObject.SetActive(false);
        }
    }
}