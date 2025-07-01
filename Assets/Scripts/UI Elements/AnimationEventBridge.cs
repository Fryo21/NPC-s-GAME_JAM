using UnityEngine;

/// <summary>
/// Simple bridge script that allows animation events on child objects to call methods on parent objects
/// Attach this to the GameObject that has the Animator (the cross-out object)
/// </summary>
public class AnimationEventBridge : MonoBehaviour
{
    [Header("Target Reference")]
    [SerializeField] private WantedPersonEntry targetWantedPersonEntry;

    [Header("Auto-Find Parent")]
    [SerializeField] private bool autoFindParentOnStart = true;

    private void Start()
    {
        // Automatically find the WantedPersonEntry in the parent hierarchy
        if (autoFindParentOnStart && targetWantedPersonEntry == null)
        {
            targetWantedPersonEntry = GetComponentInParent<WantedPersonEntry>();

            if (targetWantedPersonEntry == null)
            {
                Debug.LogError($"[AnimationEventBridge] Could not find WantedPersonEntry in parent hierarchy of {gameObject.name}");
            }
            else
            {
                Debug.Log($"[AnimationEventBridge] Auto-found WantedPersonEntry: {targetWantedPersonEntry.name}");
            }
        }
    }

    /// <summary>
    /// This method should be called by the Animation Event at the end of your cross-out animation
    /// Add this as an Animation Event in your "SuspectApprehended" animation
    /// </summary>
    public void OnCrossOutAnimationFinished()
    {
        Debug.Log($"[AnimationEventBridge] Cross-out animation finished on {gameObject.name}");

        if (targetWantedPersonEntry != null)
        {
            targetWantedPersonEntry.OnAnimationFinished();
        }
        else
        {
            Debug.LogError($"[AnimationEventBridge] No target WantedPersonEntry set for {gameObject.name}!");
        }
    }

    /// <summary>
    /// Manual method to set the target (useful if auto-find doesn't work)
    /// </summary>
    public void SetTarget(WantedPersonEntry target)
    {
        targetWantedPersonEntry = target;
    }
}