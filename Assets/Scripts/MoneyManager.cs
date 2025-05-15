using System;
using UnityEngine;

public class MoneyManager : MonoBehaviour
{
    // Singleton pattern for easy access
    public static MoneyManager Instance { get; private set; }

    [SerializeField] private float startingBalance = 100f;
    [SerializeField] private float correctArrestReward = 50f;
    [SerializeField] private float wrongArrestPenalty = 30f;
    [SerializeField] private float bankruptcyThreshold = -10f;
    public float droneCost = 10f;

    public float CurrentBalance { get; private set; }

    // Events
    public event Action<float> OnBalanceChanged;
    public event Action OnBankruptcy;

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Optional: Make this persist between scenes if needed
        // DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        ResetBalance();
    }

    public void ResetBalance()
    {
        CurrentBalance = startingBalance;
        OnBalanceChanged?.Invoke(CurrentBalance);
    }

    public void AddMoney(float amount)
    {
        CurrentBalance += amount;
        OnBalanceChanged?.Invoke(CurrentBalance);

        Debug.Log($"Added ${amount}. New balance: ${CurrentBalance}");
    }

    public void SubtractMoney(float amount)
    {
        CurrentBalance -= amount;
        OnBalanceChanged?.Invoke(CurrentBalance);

        // Check for bankruptcy
        if (CurrentBalance <= bankruptcyThreshold)
        {
            OnBankruptcy?.Invoke();
        }

        Debug.Log($"Subtracted ${amount}. New balance: ${CurrentBalance}");
    }

    public void ProcessCorrectArrest()
    {
        AddMoney(correctArrestReward);
    }

    public void ProcessWrongArrest()
    {
        SubtractMoney(wrongArrestPenalty);
    }

    public bool CanAffordDrone()
    {
        DroneAIManager.Instance.ActivateDrone();
        return CurrentBalance >= droneCost;
    }

    public bool IsBankrupt()
    {
        return CurrentBalance <= bankruptcyThreshold;
    }
}