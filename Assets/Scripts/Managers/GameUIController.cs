using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Sirenix.OdinInspector;

public class GameUIController : MonoBehaviour
{
    [FoldoutGroup("Money UI")]
    [SerializeField] private TextMeshProUGUI balanceText;
    [FoldoutGroup("Money UI")]
    [SerializeField] private Color positiveBalanceColor = Color.green;
    [FoldoutGroup("Money UI")]
    [SerializeField] private Color negativeBalanceColor = Color.red;

    [FoldoutGroup("Round UI")]
    [SerializeField] private TextMeshProUGUI roundText;
    [FoldoutGroup("Round UI")]
    [SerializeField] private TextMeshProUGUI timerText;
    [FoldoutGroup("Round UI")]
    [SerializeField] private Color timerLowColor = Color.red; // Color when time is low
    [FoldoutGroup("Round UI")]
    [SerializeField] private TextMeshProUGUI arrestQuotaText;

    [FoldoutGroup("Game State Panels")]
    [SerializeField] private GameObject interludePanel;
    [FoldoutGroup("Game State Panels")]
    [SerializeField] private GameObject gameOverPanel;

    [FoldoutGroup("Interlude UI")]
    [SerializeField] private TextMeshProUGUI roundSummaryText;

    [FoldoutGroup("Game Over UI")]
    [SerializeField] private TextMeshProUGUI gameOverReasonText;

    [FoldoutGroup("Drone Purchase UI")]
    [SerializeField] private Button purchaseDroneButton;
    [FoldoutGroup("Drone Purchase UI")]
    [SerializeField] private TextMeshProUGUI droneCostText;

    [FoldoutGroup("ESC Menu")]
    [SerializeField] private GameObject escMenu;

    [FoldoutGroup("Help Tips")]
    [SerializeField] private GameObject timerHelpTip;
    [FoldoutGroup("Help Tips")]
    [SerializeField] private GameObject arrestQuotaHelpTip;
    [FoldoutGroup("Help Tips")]
    [SerializeField] private GameObject balanceHelpTip;
    [FoldoutGroup("Help Tips")]
    [SerializeField] private GameObject dronePurchaseHelpTip;
    [FoldoutGroup("Help Tips")]
    [SerializeField] private GameObject moveableUIHelpTip;

    public static GameUIController Instance { get; private set; }

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

    }

    private void Start()
    {
        // Subscribe to events
        MoneyManager.Instance.OnBalanceChanged += UpdateBalanceUI;
        RoundManager.Instance.OnTimerTick += UpdateTimerUI;
        RoundManager.Instance.OnGameStateChanged += HandleGameStateChanged;
        RoundManager.Instance.OnRoundStarted += UpdateRoundUI;
        RoundManager.Instance.OnRoundEnded += UpdateInterludeRoundSummary;
        RoundManager.Instance.OnGameOver += ShowGameOverUI;

        // Initial UI setup
        UpdateBalanceUI(MoneyManager.Instance.CurrentBalance);
        HandleGameStateChanged(RoundManager.Instance.CurrentState);
    }

    private void OnDestroy()
    {
        // Unsubscribe from events when destroyed
        if (MoneyManager.Instance != null)
            MoneyManager.Instance.OnBalanceChanged -= UpdateBalanceUI;

        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.OnTimerTick -= UpdateTimerUI;
            RoundManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            RoundManager.Instance.OnRoundStarted -= UpdateRoundUI;
            RoundManager.Instance.OnRoundEnded -= UpdateInterludeRoundSummary;
            RoundManager.Instance.OnGameOver -= ShowGameOverUI;
        }
    }

    private void UpdateBalanceUI(float balance)
    {
        balanceText.text = $"${balance:F2}";

        // Optional: Change color if negative
        if (balance < 0)
            balanceText.color = negativeBalanceColor;
        else
            balanceText.color = positiveBalanceColor;
    }

    private void UpdateTimerUI(float timeRemaining)
    {
        int minutes = Mathf.FloorToInt(timeRemaining / 60);
        int seconds = Mathf.FloorToInt(timeRemaining % 60);
        timerText.text = $"{minutes:00}:{seconds:00}";

        // Optional: Make timer flash red when low on time
        if (timeRemaining <= 10)
            timerText.color = Color.Lerp(Color.white, Color.red, Mathf.PingPong(Time.time * 2, 1));
        else
            timerText.color = Color.white;
    }

    private void UpdateRoundUI(int roundNumber)
    {
        roundText.text = $"Shift {roundNumber}";
        UpdateArrestQuotaUI();
        UpdatePurchaseDroneButtonUI();
    }

    public void UpdateArrestQuotaUI()
    {
        arrestQuotaText.text = $"{RoundManager.Instance.ArrestedSuspects}/{RoundManager.Instance.GetMinArrestQuotaForRound()}";
    }

    public void PurchaseDrone()
    {
        if (MoneyManager.Instance.CanAffordDrone())
        {
            // MoneyManager.Instance.SubtractMoney(MoneyManager.Instance.droneCost);
            DroneManager.Instance.PurchaseDrone();
            UpdatePurchaseDroneButtonUI();
        }
        else
        {
            // Flash the cost text red to indicate insufficient funds
            StartCoroutine(FlashText(droneCostText));
        }
    }


    public void UpdatePurchaseDroneButtonUI()
    {
        droneCostText.text = $"Buy Drone: ${MoneyManager.Instance.droneCost:F2}";

        // Disable button if can't afford
        purchaseDroneButton.interactable = MoneyManager.Instance.CanAffordDrone();
    }


    private void HandleGameStateChanged(GameState newState)
    {
        // Hide all panels first
        interludePanel.SetActive(false);
        gameOverPanel.SetActive(false);

        // Show appropriate panel based on state
        switch (newState)
        {
            case GameState.Playing:
                break;

            case GameState.Interlude:
                interludePanel.SetActive(true);
                UpdateInterludeRoundSummary();
                break;

            case GameState.GameOver:
                gameOverPanel.SetActive(true);
                break;
        }
    }

    private void UpdateInterludeRoundSummary()
    {
        if (RoundManager.Instance.CurrentRound == 0)
        {
            // First round
            roundSummaryText.text = "Welcome to your first shift. Ready to begin surveillance?";
            UpdateRoundUI(1);
        }
        else
        {
            // Calculate completion percentage
            float completionPercent = (float)RoundManager.Instance.ArrestedSuspects / RoundManager.Instance.TotalSuspectsForThisRound * 100;

            roundSummaryText.text = $"Shift {RoundManager.Instance.CurrentRound} Complete!\n" +
                                    $"Suspects arrested: {RoundManager.Instance.ArrestedSuspects}/{RoundManager.Instance.TotalSuspectsForThisRound} ({completionPercent:F1}%)\n" +
                                    $"Current balance: ${MoneyManager.Instance.CurrentBalance:F2}\n\n" +
                                    $"Ready for your next shift?";
        }
    }

    private void ShowGameOverUI()
    {
        string gameOverReason;

        if (RoundManager.Instance.PlayerWasCaught())
        {
            gameOverReason = "The surveillance system has deemed you a person of interest.\n" +
                             "You have been detained for questioning.\n\n" +
                             "GAME OVER";
        }
        else if (MoneyManager.Instance.IsBankrupt())
        {
            gameOverReason = "You've gone bankrupt! The department has fired you.";
        }
        else if (RoundManager.Instance.ArrestedSuspects < Mathf.CeilToInt(RoundManager.Instance.TotalSuspectsForThisRound * 0.33f))
        {
            gameOverReason = "You failed to meet your arrest quota! The department has fired you.";
        }
        else if (RoundManager.Instance.CurrentRound >= 5) // Assuming 5 is max rounds
        {
            // Win condition - could be customized based on number of drones purchased
            gameOverReason = "Congratulations! You've completed your surveillance duties.\n" +
                             "The drones have identified you as the next target...";
        }
        else
        {
            gameOverReason = "Game Over!";
        }

        gameOverReasonText.text = gameOverReason;
    }

    public void ToggleESCMenu()
    {
        escMenu.SetActive(!escMenu.activeSelf);
    }

    public void ShowFirstRoundHelpTips()
    {
        timerHelpTip.SetActive(true);
        arrestQuotaHelpTip.SetActive(true);
        balanceHelpTip.SetActive(true);
        dronePurchaseHelpTip.SetActive(true);
        moveableUIHelpTip.SetActive(true);
    }

    private IEnumerator FlashText(TextMeshProUGUI text)
    {
        Color originalColor = text.color;

        for (int i = 0; i < 3; i++)
        {
            text.color = timerLowColor; // Flash red
            yield return new WaitForSeconds(0.1f);
            text.color = originalColor;
            yield return new WaitForSeconds(0.1f);
        }
    }
}