using DG.Tweening;
using UnityEngine;

public class HelpTip : MonoBehaviour
{

    [SerializeField] private bool fadeOutOnEnable = true; // Whether to fade out the help tip

    [SerializeField] private float helpTipDuration = 3f; // Duration to show the help tip before fading out

    //when activated
    private void OnEnable()
    {
        if (fadeOutOnEnable)
            FadeOut(helpTipDuration);
    }

    public void FadeOut(float duration)
    {
        // Configure animation with DOTween
        RectTransform rect = GetComponent<RectTransform>();
        if (rect != null)
        {
            // Animation sequence
            Sequence sequence = DOTween.Sequence();
            // Wait
            sequence.AppendInterval(duration);

            // Fade out
            sequence.Append(GetComponent<CanvasGroup>().DOFade(0, 0.5f).SetEase(Ease.InQuad));

            // Destroy when complete
            sequence.OnComplete(() => gameObject.SetActive(false));
        }
    }
}
