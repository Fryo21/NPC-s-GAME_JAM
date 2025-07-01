using UnityEngine;

public class CursorAnimationEventBridge : MonoBehaviour
{
    public void OnClickAnimEnd()
    {
        if (AnimatedCursorManager.Instance != null)
            AnimatedCursorManager.Instance.OnCursorClickEnd();
    }
}
