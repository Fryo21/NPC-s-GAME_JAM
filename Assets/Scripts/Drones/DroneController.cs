using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using Sirenix.OdinInspector;

public class DroneController : MonoBehaviour
{
    [FoldoutGroup("UI References")]
    [SerializeField] private TextMeshProUGUI droneIdText;
    [FoldoutGroup("UI References")]
    [SerializeField] private TextMeshProUGUI statusText;
    [FoldoutGroup("UI References")]
    [SerializeField] private Image suspectImage;
    [FoldoutGroup("UI References")]
    [SerializeField] private TextMeshProUGUI suspectNameText;
    [FoldoutGroup("UI References")]
    [SerializeField] private Button confirmButton;
    [FoldoutGroup("UI References")]
    [SerializeField] private Button denyButton;
    [FoldoutGroup("UI References")]
    [SerializeField] private Slider timerSlider;
    [FoldoutGroup("UI References")]
    [SerializeField] private GameObject helpTip;  // Reference to the help tip UI element

    [FoldoutGroup("Settings")]
    [SerializeField] private float scanInterval = 8f;  // Time between scans
    [FoldoutGroup("Settings")]
    [SerializeField] private float identificationDuration = 5f;  // Time player has to respond
    [FoldoutGroup("Settings")]
    [SerializeField] private float accuracyRate = 0.75f;  // Chance of correct identification
    [FoldoutGroup("Settings")]
    [SerializeField] private bool autoStartScanning = true;  // Start scanning immediately

    [FoldoutGroup("Visual Settings")]
    [SerializeField] private Color scanningImageTint = new Color(0.6f, 0.6f, 0.6f, 0.5f);  // Greyed out tint
    [FoldoutGroup("Visual Settings")]
    [SerializeField] private Color activeImageTint = Color.white;  // Normal tint
    [FoldoutGroup("Visual Settings")]
    [SerializeField] private Sprite placeholderSprite;  // Default image when scanning

    // State variables
    private int droneId;
    private bool isScanning = false;
    private NPCDataHolder currentTarget = null;
    private NPCData reportedAs = null;
    private Coroutine scanCoroutine = null;
    private Coroutine timerCoroutine = null;

    // UI States
    private enum DroneState { Idle, Scanning, AwaitingResponse }
    private DroneState currentState = DroneState.Idle;

    // Debug flag
    private bool initialized = false;

    // Flag to know if targeting player
    private bool isTargetingPlayer = false;
    private NPCData playerData;

    private void Start()
    {
        Debug.Log($"[DroneController] Start called. Initialized={initialized}, AutoStart={autoStartScanning}");

        // Auto-start scanning if enabled and already initialized
        if (initialized && autoStartScanning)
        {
            Debug.Log("[DroneController] Auto-starting scanning from Start()");
            StartScanning();
        }
    }

    public void Initialize(int id, float accuracy, float interval)
    {
        droneId = id;
        accuracyRate = accuracy;
        scanInterval = interval;
        initialized = true;

        Debug.Log($"[DroneController] Initialized with ID={id}, Accuracy={accuracy:P0}, Interval={interval}s");

        // Update UI
        if (droneIdText != null)
        {
            droneIdText.text = $"DRONE #{droneId}";
        }
        else
        {
            Debug.LogError("[DroneController] DroneIdText is null during Initialize!");
        }

        // Initialize UI state
        SetDroneState(DroneState.Idle);

        // Start scanning immediately if autostart is enabled
        if (autoStartScanning)
        {
            Debug.Log("[DroneController] Auto-starting scanning from Initialize()");
            StartScanning();
        }
    }

    public void StartScanning()
    {
        if (isScanning)
        {
            Debug.Log($"[DroneController] Drone #{droneId} is already scanning");
            return;
        }

        Debug.Log($"[DroneController] Drone #{droneId} starting scanning");
        isScanning = true;

        if (scanCoroutine != null)
        {
            StopCoroutine(scanCoroutine);
        }

        scanCoroutine = StartCoroutine(ScanningRoutine());
    }

    public void StopScanning()
    {
        if (!isScanning) return;

        Debug.Log($"[DroneController] Drone #{droneId} stopping scanning");
        isScanning = false;

        if (scanCoroutine != null)
        {
            StopCoroutine(scanCoroutine);
            scanCoroutine = null;
        }

        // Reset to idle state
        SetDroneState(DroneState.Idle);
    }

    private IEnumerator ScanningRoutine()
    {
        Debug.Log($"[DroneController] Drone #{droneId} scanning routine started");

        // Short delay before starting first scan (to allow UI to initialize)
        yield return new WaitForSeconds(1f);

        int scanCount = 0;
        while (isScanning)
        {
            // Check if the round is in progress
            if (RoundManager.Instance == null || RoundManager.Instance.CurrentState != GameState.Playing)
            {
                Debug.Log($"[DroneController] Drone #{droneId} waiting for round to start");
                yield return new WaitForSeconds(1f);
                continue;
            }

            // Set to scanning state
            SetDroneState(DroneState.Scanning);
            scanCount++;
            Debug.Log($"[DroneController] Drone #{droneId} starting scan #{scanCount}");

            // Wait for scan interval
            float scanProgress = 0;
            while (scanProgress < scanInterval)
            {
                scanProgress += Time.deltaTime;

                // Update scanning progress in UI
                if (timerSlider != null)
                {
                    timerSlider.value = scanProgress / scanInterval;
                }

                yield return null;
            }

            Debug.Log($"[DroneController] Drone #{droneId} scan completed, attempting identification");

            // Try to identify a suspect
            if (TryIdentifySuspect())
            {
                Debug.Log($"[DroneController] Drone #{droneId} successfully identified a suspect");

                // Awaiting response
                SetDroneState(DroneState.AwaitingResponse);

                // Start timer for response
                if (timerCoroutine != null)
                    StopCoroutine(timerCoroutine);

                timerCoroutine = StartCoroutine(ResponseTimerRoutine());

                //if this is the first drone that the player has bought and it is the first scan, show the help tip
                if (scanCount == 1 && droneId == 1)
                    ShowHelpTip();

                // Notify the drone manager
                if (DroneManager.Instance != null)
                {
                    DroneManager.Instance.ReportDroneIdentification(
                        this, currentTarget, reportedAs, identificationDuration);
                }

                // Wait for player decision or timeout
                while (currentState == DroneState.AwaitingResponse)
                {
                    yield return null;
                }
            }
            else
            {
                Debug.Log($"[DroneController] Drone #{droneId} failed to identify a suspect, continuing scanning");
                // If identification failed, continue scanning after a short delay
                yield return new WaitForSeconds(1f);
            }
        }
    }

    private void ShowHelpTip()
    {
        if (helpTip != null)
            helpTip.SetActive(true);
    }

    private void HideHelpTip()
    {
        if (helpTip != null)
        {
            helpTip.GetComponent<HelpTip>().FadeOut(1); // Use the FadeOut method from HelpTip script
        }

    }

    public void CheckIfTargetWasArrested(NPCDataHolder arrestedNPC)
    {
        // If this was our current target, restart scanning
        if (currentTarget == arrestedNPC)
        {
            Debug.Log($"Drone #{droneId} - Current target was arrested. Finding new target.");

            // Reset state to scanning
            currentTarget = null;
            reportedAs = null;

            // Cancel response timer if it's running
            if (currentState == DroneState.AwaitingResponse && timerCoroutine != null)
            {
                StopCoroutine(timerCoroutine);
                timerCoroutine = null;
            }

            // Reset to scanning state
            SetDroneState(DroneState.Scanning);
        }
    }

    private bool TryIdentifySuspect()
    {
        // Get wanted list from manager
        var wantedList = WantedListManager.Instance.GetCurrentWantedList();
        if (wantedList == null || wantedList.Count == 0) return false;

        // Get all NPCs in the scene
        var allNPCs = DroneManager.Instance.GetAllActiveNPCs();
        if (allNPCs.Count == 0) return false;

        // Decide whether to pick a correct or incorrect match based on accuracy
        bool pickCorrectly = Random.value <= accuracyRate;

        if (pickCorrectly)
        {
            // Try to find a wanted NPC in the scene
            var wantedNPCs = allNPCs.Where(npc =>
                wantedList.Contains(npc.nPCData)).ToList();

            if (wantedNPCs.Count > 0)
            {
                // Pick a random wanted NPC
                currentTarget = wantedNPCs[Random.Range(0, wantedNPCs.Count)];
                reportedAs = currentTarget.nPCData; // Correctly identified

                // Display the actual NPC's image but with the reported name + "?"
                UpdateSuspectUI(currentTarget.nPCData, reportedAs);
                return true;
            }
        }

        // If we couldn't pick correctly or chose not to, pick a random NPC
        // but report it as one of the wanted ones
        currentTarget = allNPCs[Random.Range(0, allNPCs.Count)];
        reportedAs = wantedList[Random.Range(0, wantedList.Count)];

        // Update UI with the actual NPC's image but the reported (false) identity + "?"
        UpdateSuspectUI(currentTarget.nPCData, reportedAs);
        SFXManager.Instance.PlayDroneAlertSound();
        return true;
    }

    private IEnumerator ResponseTimerRoutine()
    {
        float timeRemaining = identificationDuration;

        // Initialize slider
        if (timerSlider != null)
        {
            timerSlider.maxValue = identificationDuration;
            timerSlider.value = identificationDuration;
        }

        while (timeRemaining > 0 && currentState == DroneState.AwaitingResponse)
        {
            timeRemaining -= Time.deltaTime;

            // Update slider
            if (timerSlider != null)
            {
                timerSlider.value = timeRemaining;
            }

            yield return null;
        }

        // If timer ran out and we're still awaiting response, auto-confirm
        if (currentState == DroneState.AwaitingResponse)
        {
            OnConfirmButtonClicked();
        }
    }

    private void UpdateSuspectUI(NPCData actualNpcData, NPCData reportedAsNpcData)
    {
        if (actualNpcData == null || reportedAsNpcData == null) return;

        // Update image with the ACTUAL NPC's image (what the drone is looking at)
        if (suspectImage != null && actualNpcData.npcSprite != null)
        {
            suspectImage.sprite = actualNpcData.npcSprite;
            suspectImage.preserveAspect = true;
        }

        // Update name with the REPORTED name (who the drone THINKS it is) + question mark
        if (suspectNameText != null)
        {
            suspectNameText.text = reportedAsNpcData.npcName + "?";
        }
    }

    // New method to force this drone to target the player
    public void ForceTargetPlayer(NPCData playerData)
    {
        this.playerData = playerData;
        isTargetingPlayer = true;

        // Cancel current scanning and go directly to awaiting response
        if (scanCoroutine != null)
        {
            StopCoroutine(scanCoroutine);
        }

        // Set up the identification
        reportedAs = playerData;
        currentTarget = null; // There's no actual NPC for the player

        // Update UI
        UpdateSuspectUI(playerData, playerData);

        SFXManager.Instance.PlayDroneTargetPlayerSound();

        // Change state to awaiting response
        SetDroneState(DroneState.AwaitingResponse);

        // Start response timer
        if (timerCoroutine != null)
            StopCoroutine(timerCoroutine);

        timerCoroutine = StartCoroutine(ResponseTimerRoutine());

        Debug.Log($"Drone #{droneId} is now targeting the player!");
    }

    private void SetDroneState(DroneState newState)
    {
        currentState = newState;

        // Update UI based on state
        switch (newState)
        {
            case DroneState.Idle:
                if (statusText != null)
                    statusText.text = "Standby";

                // Show placeholder image with grey tint
                if (suspectImage != null)
                {
                    suspectImage.gameObject.SetActive(true);
                    suspectImage.sprite = placeholderSprite;
                    suspectImage.color = scanningImageTint;
                }

                if (suspectNameText != null)
                {
                    suspectNameText.gameObject.SetActive(true);
                    suspectNameText.text = "No Target";
                }

                // Disable buttons but keep them visible
                if (confirmButton != null)
                {
                    confirmButton.gameObject.SetActive(true);
                    confirmButton.interactable = false;
                }

                if (denyButton != null)
                {
                    denyButton.gameObject.SetActive(true);
                    denyButton.interactable = false;
                }

                // Show timer but empty
                if (timerSlider != null)
                {
                    timerSlider.gameObject.SetActive(true);
                    timerSlider.value = 0;
                }
                break;

            case DroneState.Scanning:
                if (statusText != null)
                    statusText.text = "Scanning...";

                // Show placeholder image with grey tint
                if (suspectImage != null)
                {
                    suspectImage.gameObject.SetActive(true);
                    suspectImage.sprite = placeholderSprite;
                    suspectImage.color = scanningImageTint;
                }

                if (suspectNameText != null)
                {
                    suspectNameText.gameObject.SetActive(true);
                    suspectNameText.text = "Scanning...";
                }

                // Disable buttons but keep them visible
                if (confirmButton != null)
                {
                    confirmButton.gameObject.SetActive(true);
                    confirmButton.interactable = false;
                }

                if (denyButton != null)
                {
                    denyButton.gameObject.SetActive(true);
                    denyButton.interactable = false;
                }

                // Show timer for scanning progress
                if (timerSlider != null)
                {
                    timerSlider.gameObject.SetActive(true);
                }
                break;

            case DroneState.AwaitingResponse:
                if (statusText != null)
                    statusText.text = "Suspect Identified!";

                // Show suspect image with normal tint
                if (suspectImage != null)
                {
                    suspectImage.gameObject.SetActive(true);
                    suspectImage.color = activeImageTint;
                }

                if (suspectNameText != null)
                {
                    suspectNameText.gameObject.SetActive(true);
                }

                // Enable buttons
                if (confirmButton != null)
                {
                    confirmButton.gameObject.SetActive(true);
                    confirmButton.interactable = true;
                }

                if (denyButton != null)
                {
                    denyButton.gameObject.SetActive(true);
                    denyButton.interactable = true;
                }

                // Show timer with full value
                if (timerSlider != null)
                {
                    timerSlider.gameObject.SetActive(true);
                    timerSlider.value = timerSlider.maxValue;
                }
                break;
        }
    }

    public void OnConfirmButtonClicked()
    {

        // Special handling for player targeting
        if (isTargetingPlayer)
        {
            // Notify DroneManager that player confirmed their own arrest
            if (DroneManager.Instance != null)
                DroneManager.Instance.CheckIfPlayerWasCaught(true);

            // Don't reset state - keep targeting the player
            return;
        }

        // Process the confirmation
        DroneManager.Instance.ProcessDroneIdentificationResponse(this, currentTarget, true);

        // Reset state
        currentTarget = null;
        reportedAs = null;
        SetDroneState(DroneState.Scanning);
        HideHelpTip();
    }

    public void OnDenyButtonClicked()
    {
        if (isTargetingPlayer)
        {
            return;
        }

        // Process the denial
        DroneManager.Instance.ProcessDroneIdentificationResponse(this, currentTarget, false);

        // Reset state
        currentTarget = null;
        reportedAs = null;
        SetDroneState(DroneState.Scanning);
        HideHelpTip();
    }

    public void ShowDroneHelpTip()
    {
        if (helpTip != null)
        {
            helpTip.gameObject.SetActive(true);
        }
    }
}