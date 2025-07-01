using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class WantedPersonEntry : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image characterImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Animator crossOutAnimator;  // Reference to the animator with the big X animation
    [SerializeField] private GameObject crossOutObject;  // The actual X GameObject (in case you need to activate it)

    [Header("Animation Settings")]
    [SerializeField] private string apprehendedAnimationTrigger = "SuspectApprehended";  // Name of the animation trigger

    // Store the NPC data for this entry
    private NPCData associatedNPCData;

    // Event that fires when the animation is complete and the entry should be removed
    public event Action<WantedPersonEntry> OnAnimationComplete;

    // Flag to prevent multiple animations
    private bool isAnimating = false;
    private bool isMarkedForRemoval = false;

    public void Initialize(NPCData npcData)
    {
        if (npcData == null) return;

        // Store the NPC data
        associatedNPCData = npcData;

        // Set the character image
        if (characterImage != null && npcData.npcSprite != null)
        {
            characterImage.sprite = npcData.npcSprite;
            characterImage.preserveAspect = true;
        }

        // Set the name text
        if (nameText != null)
        {
            nameText.text = npcData.npcName;
        }

        // Make sure the cross-out is initially hidden
        // if (crossOutObject != null)
        // {
        //     crossOutObject.SetActive(false);

        // Set up the animation event bridge if it exists
        AnimationEventBridge bridge = crossOutObject.GetComponent<AnimationEventBridge>();
        if (bridge != null)
        {
            bridge.SetTarget(this);
            //                Debug.Log($"[WantedPersonEntry] Set up animation bridge for {npcData.npcName}");
        }
        //}

        // Reset flags
        isAnimating = false;
        isMarkedForRemoval = false;
    }

    /// <summary>
    /// Call this when the suspect has been apprehended to start the cross-out animation
    /// </summary>
    public void MarkAsApprehended()
    {
        // Prevent multiple calls
        if (isAnimating || isMarkedForRemoval) return;

        isAnimating = true;
        isMarkedForRemoval = true;

        Debug.Log($"[WantedPersonEntry] Marking {associatedNPCData?.npcName} as apprehended");

        // // Activate the cross-out object if it's not already active
        // if (crossOutObject != null)
        // {
        //     crossOutObject.SetActive(true);
        // }

        // Trigger the animation
        if (crossOutAnimator != null)
        {
            crossOutAnimator.SetTrigger(apprehendedAnimationTrigger);
        }
        else
        {
            Debug.LogWarning("[WantedPersonEntry] No animator found! Calling animation complete immediately.");
            // If no animator, just call the completion immediately
            OnAnimationFinished();
        }
    }

    /// <summary>
    /// This method should be called by an Animation Event at the end of your cross-out animation
    /// Add this as an Animation Event in your "SuspectApprehended" animation
    /// </summary>
    public void OnAnimationFinished()
    {
        Debug.Log($"[WantedPersonEntry] Animation finished for {associatedNPCData?.npcName}");

        isAnimating = false;

        // Notify any listeners that this entry is ready to be removed
        OnAnimationComplete?.Invoke(this);
    }

    /// <summary>
    /// Get the NPC data associated with this wanted person entry
    /// </summary>
    public NPCData GetAssociatedNPCData()
    {
        return associatedNPCData;
    }

    /// <summary>
    /// Check if this entry is currently being animated or marked for removal
    /// </summary>
    public bool IsMarkedForRemoval()
    {
        return isMarkedForRemoval;
    }

    /// <summary>
    /// Force remove this entry (for cleanup purposes)
    /// </summary>
    public void ForceRemove()
    {
        if (isAnimating && crossOutAnimator != null)
        {
            // Stop any ongoing animation
            crossOutAnimator.SetTrigger("Reset"); // You might want to add a reset trigger to your animator
        }

        // Clean up
        OnAnimationComplete = null;

        // Destroy the GameObject
        Destroy(gameObject);
    }
}