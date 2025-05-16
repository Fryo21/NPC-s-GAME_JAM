using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

/// <summary>
/// Add this component to any UI element to add juice effects like pop-in, hover feedback, click feedback, and drag enhancement.
/// Requires DOTween to be installed in your project.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class UIJuiceEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Appearance Animation")]
    [SerializeField] private bool playAppearAnimation = true;
    [SerializeField] private float appearDelay = 0f;
    [SerializeField] private float appearDuration = 0.3f;
    [SerializeField] private Ease appearEase = Ease.OutBack;
    [SerializeField] private bool useRandomRotation = false;
    [SerializeField] private float maxRandomRotation = 5f;

    [Header("Hover Effects")]
    [SerializeField] private bool enableHoverEffects = true;
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private float hoverScaleDuration = 0.2f;
    [SerializeField] private Ease hoverEase = Ease.OutQuad;
    [SerializeField] private bool useHoverColor = false;
    [SerializeField] private Color hoverColor = new Color(1f, 1f, 1f, 1f);

    [Header("Click Effects")]
    [SerializeField] private bool enableClickEffects = true;
    [SerializeField] private float clickScale = 0.95f;
    [SerializeField] private float clickDuration = 0.1f;
    [SerializeField] private Ease clickEase = Ease.OutQuad;
    [SerializeField] private bool playClickSound = false;
    [SerializeField] private AudioClip clickSound;

    [Header("Drag Effects")]
    [SerializeField] private bool enableDragEffects = true;
    [SerializeField] private float dragScale = 1.05f;
    [SerializeField] private float dragAlpha = 0.9f;
    [SerializeField] private bool bringToFrontWhenDragged = true;
    [SerializeField] private bool addDragRotation = true;
    [SerializeField] private float maxDragRotation = 1.5f;
    [SerializeField] private float dragRotationSpeed = 1f;

    [Header("Drop Effects")]
    [SerializeField] private bool enableDropEffects = true;
    [SerializeField] private bool bounceDrop = true;
    [SerializeField] private float dropBounceDuration = 0.3f;
    [SerializeField] private Ease dropBounceEase = Ease.OutBounce;
    [SerializeField] private bool shakeOnDrop = false;
    [SerializeField] private float shakeStrength = 5f;
    [SerializeField] private int shakeVibrato = 10;

    // Component references
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Image backgroundImage;
    private AudioSource audioSource;

    // State tracking
    private Vector3 originalScale;
    private Color originalColor;
    private int originalSiblingIndex;
    private Vector3 dragVelocity;
    private Vector3 lastPosition;
    private Vector3 dragStartPosition;
    private bool isDragging = false;
    private bool isPointerOver = false;

    // Tweens
    private Tweener scaleTween;
    private Tweener colorTween;
    private Tweener rotateTween;
    private Tweener fadeTween;

    private void Awake()
    {
        // Get component references
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        backgroundImage = GetComponent<Image>();
        audioSource = GetComponent<AudioSource>();

        // Ensure we have a CanvasGroup for alpha effects
        if (canvasGroup == null && (dragAlpha < 1f || useHoverColor))
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // Create audio source if needed and not present
        if (playClickSound && audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // Store original values
        originalScale = rectTransform.localScale;
        if (backgroundImage != null)
        {
            originalColor = backgroundImage.color;
        }
    }

    private void Start()
    {
        // Play appear animation
        if (playAppearAnimation)
        {
            PlayAppearAnimation();
        }
    }

    private void OnEnable()
    {
        // Reset to original state when re-enabled
        ResetToOriginalState();

        // Play appear animation if enabled
        if (playAppearAnimation)
        {
            PlayAppearAnimation();
        }
    }

    private void PlayAppearAnimation()
    {
        // Kill any active tweens
        KillAllTweens();

        // Store the current scale for restoration after animation
        Vector3 targetScale = rectTransform.localScale;

        // Set initial state
        rectTransform.localScale = Vector3.zero;

        // Random starting rotation if enabled
        if (useRandomRotation)
        {
            float randomRotation = Random.Range(-maxRandomRotation, maxRandomRotation);
            rectTransform.rotation = Quaternion.Euler(0, 0, randomRotation);
        }

        // Create sequence for appear animation
        Sequence appearSequence = DOTween.Sequence();

        // Add delay if specified
        if (appearDelay > 0)
        {
            appearSequence.AppendInterval(appearDelay);
        }

        // Add scale animation
        appearSequence.Append(rectTransform.DOScale(targetScale, appearDuration).SetEase(appearEase));

        // Add rotation reset if needed
        if (useRandomRotation)
        {
            appearSequence.Join(rectTransform.DORotate(Vector3.zero, appearDuration).SetEase(appearEase));
        }

        // Play the sequence
        appearSequence.Play();
    }

    private void ResetToOriginalState()
    {
        // Kill any active tweens
        KillAllTweens();

        // Reset transform
        rectTransform.localScale = originalScale;
        rectTransform.localRotation = Quaternion.identity;

        // Reset color if applicable
        if (backgroundImage != null)
        {
            backgroundImage.color = originalColor;
        }

        // Reset alpha if applicable
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }

        // Reset state tracking
        isDragging = false;
        isPointerOver = false;
    }

    private void KillAllTweens()
    {
        if (scaleTween != null && scaleTween.IsActive())
            scaleTween.Kill();

        if (colorTween != null && colorTween.IsActive())
            colorTween.Kill();

        if (rotateTween != null && rotateTween.IsActive())
            rotateTween.Kill();

        if (fadeTween != null && fadeTween.IsActive())
            fadeTween.Kill();
    }

    #region Event Handlers

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!enableHoverEffects || isDragging) return;

        isPointerOver = true;

        // Scale effect
        if (hoverScale != 1f)
        {
            Vector3 targetScale = originalScale * hoverScale;
            scaleTween = rectTransform.DOScale(targetScale, hoverScaleDuration).SetEase(hoverEase);
        }

        // Color effect
        if (useHoverColor && backgroundImage != null)
        {
            colorTween = backgroundImage.DOColor(hoverColor, hoverScaleDuration).SetEase(hoverEase);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!enableHoverEffects || isDragging) return;

        isPointerOver = false;

        // Reset scale
        if (hoverScale != 1f)
        {
            scaleTween = rectTransform.DOScale(originalScale, hoverScaleDuration).SetEase(hoverEase);
        }

        // Reset color
        if (useHoverColor && backgroundImage != null)
        {
            colorTween = backgroundImage.DOColor(originalColor, hoverScaleDuration).SetEase(hoverEase);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!enableClickEffects) return;

        // Scale effect
        if (clickScale != 1f)
        {
            Vector3 targetScale = (isPointerOver ? originalScale * hoverScale : originalScale) * clickScale;
            scaleTween = rectTransform.DOScale(targetScale, clickDuration).SetEase(clickEase);
        }

        // Play sound
        if (playClickSound && audioSource != null && clickSound != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!enableClickEffects) return;

        // Reset scale based on hover state
        if (clickScale != 1f)
        {
            Vector3 targetScale = isPointerOver ? originalScale * hoverScale : originalScale;
            scaleTween = rectTransform.DOScale(targetScale, clickDuration).SetEase(clickEase);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!enableDragEffects) return;

        isDragging = true;
        dragStartPosition = rectTransform.position;
        lastPosition = dragStartPosition;
        originalSiblingIndex = transform.GetSiblingIndex();

        // Store velocity for rotation calculation
        dragVelocity = Vector3.zero;

        // Bring to front if enabled
        if (bringToFrontWhenDragged)
        {
            transform.SetAsLastSibling();
        }

        // Scale effect
        if (dragScale != 1f)
        {
            scaleTween = rectTransform.DOScale(originalScale * dragScale, 0.2f).SetEase(Ease.OutQuad);
        }

        // Alpha effect
        if (dragAlpha < 1f && canvasGroup != null)
        {
            fadeTween = canvasGroup.DOFade(dragAlpha, 0.2f).SetEase(Ease.OutQuad);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!enableDragEffects || !isDragging) return;

        // Calculate and store velocity for rotation
        if (addDragRotation)
        {
            Vector3 currentPosition = rectTransform.position;
            dragVelocity = (currentPosition - lastPosition) / Time.deltaTime;
            lastPosition = currentPosition;

            // Apply subtle rotation based on drag velocity
            float rotationAmount = Mathf.Clamp(dragVelocity.x * dragRotationSpeed * 0.01f, -maxDragRotation, maxDragRotation);
            rectTransform.rotation = Quaternion.Euler(0, 0, rotationAmount);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!enableDragEffects || !enableDropEffects || !isDragging) return;

        isDragging = false;

        // Reset sibling index if not hover
        if (bringToFrontWhenDragged && !isPointerOver)
        {
            transform.SetSiblingIndex(originalSiblingIndex);
        }

        // Create sequence for drop animation
        Sequence dropSequence = DOTween.Sequence();

        // Reset scale
        if (dragScale != 1f)
        {
            Vector3 targetScale = isPointerOver ? originalScale * hoverScale : originalScale;
            dropSequence.Append(rectTransform.DOScale(targetScale, 0.2f).SetEase(Ease.OutQuad));
        }

        // Reset alpha
        if (dragAlpha < 1f && canvasGroup != null)
        {
            dropSequence.Join(canvasGroup.DOFade(1f, 0.2f).SetEase(Ease.OutQuad));
        }

        // Bounce effect
        if (bounceDrop)
        {
            // Slightly overshoot and bounce back
            Vector3 bounceScale = isPointerOver ? originalScale * hoverScale * 1.1f : originalScale * 1.1f;
            dropSequence.Append(rectTransform.DOScale(bounceScale, dropBounceDuration * 0.3f).SetEase(Ease.OutQuad));
            dropSequence.Append(rectTransform.DOScale(isPointerOver ? originalScale * hoverScale : originalScale, dropBounceDuration * 0.7f).SetEase(dropBounceEase));
        }

        // Reset rotation
        if (addDragRotation)
        {
            dropSequence.Join(rectTransform.DORotate(Vector3.zero, 0.3f).SetEase(Ease.OutQuad));
        }

        // Shake effect
        if (shakeOnDrop)
        {
            dropSequence.Append(rectTransform.DOShakePosition(0.3f, shakeStrength, shakeVibrato));
        }

        // Play the sequence
        dropSequence.Play();
    }

    #endregion

    private void OnDisable()
    {
        // Kill all tweens when disabled
        KillAllTweens();
    }
}