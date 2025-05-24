using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class FeedbackManager : MonoBehaviour
{
    public static FeedbackManager Instance { get; private set; }

    [Header("Success Feedback")]
    [SerializeField] private GameObject successPopupPrefab;
    [SerializeField] private Transform popUpCanvasTransform;
    [SerializeField] private float successPopupDuration = 2f;
    [SerializeField]
    private string[] successMessages = new string[]
    {
        "Great job! Suspect apprehended.",
        "Correct identification! +$10",
        "Criminal apprehended successfully!",
        "Target acquired. Well done."
    };

    [Header("Warning Feedback")]
    [SerializeField] private GameObject warningPopupPrefab;
    [SerializeField] private Vector2 warningSpawnPosition = new Vector2(200, -100);
    [SerializeField] private float warningSpawnOffset = 20f;
    [SerializeField]
    private string[] warningMessages = new string[]
    {
        "Wrong target! Innocent citizen arrested. -$15",
        "False arrest! Department faces lawsuit. -$15",
        "Incorrect identification! Penalty applied.",
        "Civilian wrongfully detained. Watch your accuracy!"
    };

    [Header("Employee of the Month")]
    [SerializeField] private GameObject employeeOfMonthPrefab;
    [SerializeField] private Sprite playerCharacterSprite;
    [SerializeField] private string playerName = "Officer #7482";
    [SerializeField] private Vector2 employeePopupPosition = new Vector2(-200, 100);

    private GameObject employeePopup;

    // List to track active warning popups
    private List<GameObject> activeWarnings = new List<GameObject>();
    private int warningCount = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Subscribe to round events to clear warnings
        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.OnRoundEnded += ClearAllWarnings;
        }

        // Subscribe to round events
        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.OnRoundEnded += ClearAllWarnings;
            RoundManager.Instance.OnRoundEnded += CheckForSpecialEvents;
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.OnRoundEnded -= ClearAllWarnings;
        }

        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.OnRoundEnded -= ClearAllWarnings;
            RoundManager.Instance.OnRoundEnded -= CheckForSpecialEvents;
        }
    }

    private void CheckForSpecialEvents()
    {
        if (RoundManager.Instance == null) return;

        int currentRound = RoundManager.Instance.CurrentRound;

        // End of 3rd shift - show Employee of the Month if player is doing well
        if (currentRound == 3)
        {
            bool isSuccessful = !MoneyManager.Instance.IsBankrupt() &&
                               RoundManager.Instance.ArrestedSuspects >=
                               Mathf.CeilToInt(RoundManager.Instance.TotalSuspectsForThisRound * 0.33f);

            if (isSuccessful)
            {
                ShowEmployeeOfMonthPopup();
            }
        }

        // End of 5th shift - make drones turn against player
        if (currentRound == 4 && DroneManager.Instance != null)
        {
            StartCoroutine(DronesTargetPlayerCoroutine());
        }
    }

    public void ShowEmployeeOfMonthPopup()
    {
        if (employeeOfMonthPrefab == null || popUpCanvasTransform == null) return;

        // If it's already shown, don't create another one
        if (employeePopup != null) return;

        // Instantiate employee popup
        employeePopup = Instantiate(employeeOfMonthPrefab, popUpCanvasTransform);

    }

    private IEnumerator DronesTargetPlayerCoroutine()
    {
        // Wait a bit before the drones turn on the player
        yield return new WaitForSeconds(30f); // 30 seconds before end of shift

        // Make all drones target the player
        DroneManager.Instance.MakeDronesTargetPlayer();
    }

    public void ShowSuccessFeedback()
    {
        if (successPopupPrefab == null || popUpCanvasTransform == null) return;

        // Instantiate success popup
        GameObject popup = Instantiate(successPopupPrefab, popUpCanvasTransform);

        // Set random success message
        TextMeshProUGUI messageText = popup.GetComponentInChildren<TextMeshProUGUI>();
        if (messageText != null)
        {
            messageText.text = successMessages[Random.Range(0, successMessages.Length)];
        }

        // Configure animation with DOTween
        RectTransform rect = popup.GetComponent<RectTransform>();
        if (rect != null)
        {
            // Start from slightly above center with 0 scale
            rect.anchoredPosition = new Vector2(0, 50);
            rect.localScale = Vector3.zero;

            // Animation sequence
            Sequence sequence = DOTween.Sequence();

            // Pop in
            sequence.Append(rect.DOScale(1f, 0.3f).SetEase(Ease.OutBack));

            // Slight bounce
            sequence.Append(rect.DOAnchorPosY(70, 0.2f).SetEase(Ease.OutQuad));
            sequence.Append(rect.DOAnchorPosY(50, 0.2f).SetEase(Ease.InOutQuad));

            // Wait
            sequence.AppendInterval(successPopupDuration - 1.2f);

            // Fade out
            sequence.Append(popup.GetComponent<CanvasGroup>().DOFade(0, 0.5f).SetEase(Ease.InQuad));

            // Destroy when complete
            sequence.OnComplete(() => Destroy(popup));
        }
    }

    public void ShowWarningFeedback()
    {
        if (warningPopupPrefab == null || popUpCanvasTransform == null) return;

        // Instantiate warning popup
        GameObject popup = Instantiate(warningPopupPrefab, popUpCanvasTransform);
        activeWarnings.Add(popup);

        // Set random warning message
        TextMeshProUGUI messageText = popup.GetComponentInChildren<TextMeshProUGUI>();
        if (messageText != null)
        {
            messageText.text = warningMessages[Random.Range(0, warningMessages.Length)];
        }

        // Configure position with offset based on existing warnings
        RectTransform rect = popup.GetComponent<RectTransform>();
        if (rect != null)
        {
            // Calculate position with offset for stacking
            Vector2 position = warningSpawnPosition + new Vector2(warningCount * warningSpawnOffset, -warningCount * warningSpawnOffset);
            rect.anchoredPosition = position;
            warningCount++;

            // Animation with DOTween
            rect.localScale = Vector3.zero;
            rect.DOScale(1f, 0.3f).SetEase(Ease.OutBack);

            // Add shake animation for emphasis
            rect.DOShakePosition(0.5f, 10, 20, 90, false, true).SetDelay(0.3f);
        }

        // Make warning draggable
        DraggablePopup draggable = popup.GetComponent<DraggablePopup>();
        if (draggable == null)
        {
            draggable = popup.AddComponent<DraggablePopup>();
        }
    }

    public void ClearAllWarnings()
    {
        foreach (GameObject warning in activeWarnings)
        {
            if (warning != null)
            {
                // Animate out before destroying
                CanvasGroup canvasGroup = warning.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.DOFade(0, 0.5f).OnComplete(() => Destroy(warning));
                }
                else
                {
                    Destroy(warning);
                }
            }
        }

        activeWarnings.Clear();
        warningCount = 0;
    }

    public void ClearEmployeeOfMonthPopup()
    {
        if (employeePopup != null)
        {
            Destroy(employeePopup);
            employeePopup = null;
        }
    }
}