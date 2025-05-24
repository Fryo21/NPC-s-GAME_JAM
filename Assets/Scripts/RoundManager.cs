using System;
using UnityEngine;

public enum GameState
{
    Preparing,
    Playing,
    Interlude,
    GameOver
}

public class RoundManager : MonoBehaviour
{
    // Singleton pattern
    public static RoundManager Instance { get; private set; }

    [Header("Round Settings")]
    [SerializeField] private int maxRounds = 5;
    [SerializeField] private float roundDuration = 60f; // 1 minute per round
    [SerializeField] private float interludeDuration = 10f; // Optional auto-continue

    [Header("Quota Settings")]
    [SerializeField] private float quotaPercentage = 0.33f; // Need to arrest 1/3 of suspects

    // State tracking
    public int CurrentRound { get; private set; } = 0;
    public float RemainingTime { get; private set; }
    public GameState CurrentState { get; private set; } = GameState.Preparing;
    public int TotalSuspectsForThisRound { get; private set; }
    public int ArrestedSuspects { get; private set; }

    // Events
    public event Action<GameState> OnGameStateChanged;
    public event Action<int> OnRoundStarted;
    public event Action OnRoundEnded;
    public event Action OnGameOver;
    public event Action<float> OnTimerTick;


    private bool playerWasCaught = false;


    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Start at interlude to let player begin when ready
        ChangeState(GameState.Interlude);
    }

    private void Update()
    {
        if (CurrentState == GameState.Playing)
        {
            UpdateTimer();
        }
    }

    private void UpdateTimer()
    {
        RemainingTime -= Time.deltaTime;
        OnTimerTick?.Invoke(RemainingTime);

        if (RemainingTime <= 0)
        {
            EndRound();
        }
    }

    public void StartNextRound()
    {
        if (CurrentState != GameState.Interlude)
        {
            Debug.LogWarning("Tried to start next round when not in interlude state");
            return;
        }

        CurrentRound++;
        RemainingTime = roundDuration;
        ArrestedSuspects = 0;

        Debug.Log($"Starting Round {CurrentRound}. Find {TotalSuspectsForThisRound} suspects!");

        ChangeState(GameState.Playing);
        OnRoundStarted?.Invoke(CurrentRound);

        MusicManager.Instance.SetHardIntensity();

        SFXManager.Instance.PlayRoundStartSound();
    }

    public void EndRound()
    {
        if (CurrentState != GameState.Playing)
        {
            return;
        }

        OnRoundEnded?.Invoke();
        Debug.Log($"Round {CurrentRound} ended. Arrested {ArrestedSuspects}/{TotalSuspectsForThisRound} suspects.");

        // Check win/lose conditions
        if (CheckWinLoseConditions())
        {
            ChangeState(GameState.GameOver);
            SFXManager.Instance.PlayGameOverSound();
            OnGameOver?.Invoke();
        }
        else
        {
            ChangeState(GameState.Interlude);
            SFXManager.Instance.PlayRoundEndSound();
            TipManager.Instance.SetTipTextToRandomTip();

        }

        MusicManager.Instance.SetSoftIntensity();


    }

    private bool CheckWinLoseConditions()
    {
        // Check for bankruptcy (delegate to MoneyManager)
        if (MoneyManager.Instance.IsBankrupt())
        {
            Debug.Log("Game Over: Bankrupt!");
            return true;
        }

        // Check for quota failure
        if (ArrestedSuspects < Mathf.CeilToInt(TotalSuspectsForThisRound * quotaPercentage))
        {
            Debug.Log("Game Over: Failed to meet arrest quota!");
            return true;
        }

        if (playerWasCaught)
        {
            Debug.Log("Game Over: Player was caught by the drones!");
            return true;
        }

        // Check for max rounds (win condition)
        if (CurrentRound >= maxRounds)
        {
            Debug.Log("Game Won: Completed all rounds!");
            return true;
        }

        return false;
    }

    public void ReportArrest(bool isCorrect)
    {
        if (CurrentState != GameState.Playing)
        {
            return;
        }

        if (isCorrect)
        {
            ArrestedSuspects++;
            MoneyManager.Instance.ProcessCorrectArrest();
        }
        else
        {
            MoneyManager.Instance.ProcessWrongArrest();
        }
    }

    public void EndRoundEarly()
    {
        EndRound();
    }

    private void ChangeState(GameState newState)
    {
        CurrentState = newState;
        OnGameStateChanged?.Invoke(newState);
    }

    // Method to reset the game (can be called to restart)
    public void ResetGame()
    {
        Debug.Log("[RoundManager] Resetting game");

        // Reset round state
        CurrentRound = 0;
        ArrestedSuspects = 0;
        playerWasCaught = false;

        // Reset money
        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.ResetBalance();
        }

        // Reset drones
        if (DroneManager.Instance != null)
        {
            DroneManager.Instance.ResetAllDrones();
        }

        if (FeedbackManager.Instance != null)
        {
            FeedbackManager.Instance.ClearEmployeeOfMonthPopup();
        }

        // Change state to interlude to start fresh
        ChangeState(GameState.Interlude);

        // Log reset completion
        Debug.Log("[RoundManager] Game reset complete");
    }


    public void EndGameWithPlayerCaught()
    {
        playerWasCaught = true;
        EndRound();
    }

    public bool PlayerWasCaught()
    {
        return playerWasCaught;
    }
}