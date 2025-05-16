using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using UnityEngine.Events;

[RequireComponent(typeof(Button))]
public class ButtonJuice : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Scale Effects")]
    [SerializeField] private bool useScaleEffect = true;
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private float clickScale = 0.95f;
    [SerializeField] private float scaleDuration = 0.2f;

    [Header("Color Effects")]
    [SerializeField] private bool useColorEffect = true;
    [SerializeField] private float brightenAmount = 0.2f;
    [SerializeField] private float darkenAmount = 0.1f;
    [SerializeField] private float colorDuration = 0.1f;

    [Header("Movement Effects")]
    [SerializeField] private bool useShakeEffect = true;
    [SerializeField] private float shakeStrength = 3f;
    [SerializeField] private int shakeVibrato = 10;
    [SerializeField] private float shakeDuration = 0.3f;

    [Header("Rotation Effects")]
    [SerializeField] private bool useRotationEffect = false;
    [SerializeField] private float rotationAmount = 5f;
    [SerializeField] private float rotationDuration = 0.2f;

    [Header("Sound Effects")]
    [SerializeField] private bool useAudioEffects = false;
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private float volume = 0.5f;

    // Cached components
    private Button button;
    private Image image;
    private RectTransform rectTransform;
    private AudioSource audioSource;
    private Color originalColor;
    private Vector3 originalScale;
    private Quaternion originalRotation;

    // Animation tweens
    private Tweener scaleTween;
    private Tweener colorTween;
    private Tweener rotationTween;

    // Button state
    private bool isPointerOver = false;

    private void Awake()
    {
        // Get required components
        button = GetComponent<Button>();
        image = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();

        // Add audio source if needed
        if (useAudioEffects && (hoverSound != null || clickSound != null))
        {
            audioSource = gameObject.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.volume = volume;
            }
        }

        // Store original values
        if (image != null) originalColor = image.color;
        originalScale = rectTransform.localScale;
        originalRotation = rectTransform.localRotation;

        // Add custom click animation
        if (button != null)
        {
            // Store the original onClick events
            UnityEvent originalOnClick = new UnityEvent();
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            {
                var call = button.onClick.GetPersistentMethodName(i);
                var target = button.onClick.GetPersistentTarget(i);
                var methodInfo = target.GetType().GetMethod(call);

                if (methodInfo != null)
                {
                    originalOnClick.AddListener(() => methodInfo.Invoke(target, null));
                }
            }

            // Clear and set up our custom onClick with juice
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnButtonClick(originalOnClick));
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!button.interactable) return;

        isPointerOver = true;

        // Scale effect
        if (useScaleEffect)
        {
            scaleTween?.Kill();
            scaleTween = rectTransform.DOScale(originalScale * hoverScale, scaleDuration)
                .SetEase(Ease.OutBack);
        }

        // Color effect
        if (useColorEffect && image != null)
        {
            colorTween?.Kill();
            Color brighterColor = new Color(
                Mathf.Clamp01(originalColor.r + brightenAmount),
                Mathf.Clamp01(originalColor.g + brightenAmount),
                Mathf.Clamp01(originalColor.b + brightenAmount),
                originalColor.a
            );
            colorTween = image.DOColor(brighterColor, colorDuration);
        }

        // Rotation effect
        if (useRotationEffect)
        {
            rotationTween?.Kill();
            rotationTween = rectTransform.DOLocalRotate(
                new Vector3(0, 0, Random.Range(-rotationAmount / 2, rotationAmount / 2)),
                rotationDuration
            ).SetEase(Ease.InOutSine);
        }

        // Sound effect
        if (useAudioEffects && audioSource != null && hoverSound != null)
        {
            audioSource.clip = hoverSound;
            audioSource.Play();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerOver = false;
        ResetToOriginalState();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!button.interactable) return;

        // Scale effect
        if (useScaleEffect)
        {
            scaleTween?.Kill();
            scaleTween = rectTransform.DOScale(originalScale * clickScale, scaleDuration / 2)
                .SetEase(Ease.OutQuad);
        }

        // Color effect
        if (useColorEffect && image != null)
        {
            colorTween?.Kill();
            Color darkerColor = new Color(
                Mathf.Clamp01(originalColor.r - darkenAmount),
                Mathf.Clamp01(originalColor.g - darkenAmount),
                Mathf.Clamp01(originalColor.b - darkenAmount),
                originalColor.a
            );
            colorTween = image.DOColor(darkerColor, colorDuration / 2);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!button.interactable) return;

        // If still hovering, restore hover state, otherwise reset completely
        if (isPointerOver)
        {
            OnPointerEnter(eventData);
        }
        else
        {
            ResetToOriginalState();
        }
    }

    private void OnButtonClick(UnityEvent originalOnClick)
    {
        SFXManager.Instance.PlayButtonClickSound();
        // Shake effect
        if (useShakeEffect)
        {
            rectTransform.DOShakePosition(shakeDuration, shakeStrength, shakeVibrato)
                .SetUpdate(true);
        }

        // Sound effect
        if (useAudioEffects && audioSource != null && clickSound != null)
        {
            audioSource.clip = clickSound;
            audioSource.Play();
        }

        // // Invoke the original onClick events after a tiny delay for better feel
        // DOVirtual.DelayedCall(0.05f, () =>
        // {
        //     originalOnClick.Invoke();
        // });
    }

    private void ResetToOriginalState()
    {
        // Kill any ongoing animations
        scaleTween?.Kill();
        colorTween?.Kill();
        rotationTween?.Kill();

        // Reset scale
        if (useScaleEffect)
        {
            scaleTween = rectTransform.DOScale(originalScale, scaleDuration)
                .SetEase(Ease.OutBack);
        }

        // Reset color
        if (useColorEffect && image != null)
        {
            colorTween = image.DOColor(originalColor, colorDuration);
        }

        // Reset rotation
        if (useRotationEffect)
        {
            rotationTween = rectTransform.DOLocalRotateQuaternion(originalRotation, rotationDuration);
        }
    }

    private void OnDestroy()
    {
        // Clean up tweens when destroyed
        scaleTween?.Kill();
        colorTween?.Kill();
        rotationTween?.Kill();
    }
}