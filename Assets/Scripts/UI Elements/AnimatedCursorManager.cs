using UnityEngine;
using UnityEngine.SceneManagement;

public class AnimatedCursorManager : MonoBehaviour
{
    [Header("Cursor Settings")]
    [Tooltip("The GameObject containing your animated cursor sprite")]
    public GameObject cursorGameObject;

    [SerializeField] private Animator cursorAnimator;

    [Tooltip("Offset from the actual mouse position (useful for positioning the cursor tip)")]
    public Vector2 cursorOffset = new Vector2(0, 0);

    [Tooltip("Should the cursor be hidden when the game starts?")]
    public bool hideCursorOnStart = true;

    // [Header("Camera Reference")]
    // [Tooltip("The camera that renders the UI (usually the main camera)")]
    // public Camera uiCamera;

    private RectTransform cursorRectTransform;
    private Canvas parentCanvas;

    public static AnimatedCursorManager Instance { get; private set; }

    private bool isHovering = false;
    private bool isClicking = false;


    void Awake()
    {
        // Singleton setup
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

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    //call this when a new scene loads (as this is dontDestroyOnLoad)
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SetupCursor();
    }


    void SetupCursor()
    {
        // Hide the default system cursor
        if (hideCursorOnStart)
        {
            Cursor.visible = false;
        }

        // Set up the cursor GameObject
        if (cursorGameObject != null)
        {
            // Get the RectTransform component
            cursorRectTransform = cursorGameObject.GetComponent<RectTransform>();
            if (cursorRectTransform == null)
            {
                Debug.LogError("Cursor GameObject must have a RectTransform component!");
                return;
            }

            // Find the parent canvas
            parentCanvas = cursorGameObject.GetComponentInParent<Canvas>();
            if (parentCanvas == null)
            {
                Debug.LogError("Cursor must be a child of a Canvas!");
                return;
            }

            // Make sure the cursor appears on top of other UI elements
            cursorGameObject.transform.SetAsLastSibling();
        }
        else
        {
            Debug.LogError("Cursor GameObject is not set!");
        }
    }

    void Update()
    {
        UpdateCursorPosition();
    }

    void UpdateCursorPosition()
    {
        if (cursorRectTransform == null || parentCanvas == null)
            return;

        // Get the mouse position
        Vector2 mousePosition = Input.mousePosition;

        // Apply offset
        mousePosition += cursorOffset;

        // Convert screen position to canvas position
        Vector2 canvasPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.transform as RectTransform,
            mousePosition,
            parentCanvas.worldCamera,
            out canvasPosition
        );

        // Update cursor position
        cursorRectTransform.localPosition = canvasPosition;
    }

    public void OnCursorExitUIElement()
    {
        isHovering = false;
        isClicking = false;
        SetCursorAnimation("Idle");
    }

    public void OnCursorHoverUIElement()
    {
        isHovering = true;
        SetCursorAnimation("Hover");
    }

    public void OnCursorClick()
    {
        isClicking = true;

        if (cursorAnimator != null)
        {
            SetCursorAnimation("Click");
        }

    }

    public void OnCursorClickEnd()
    {
        isClicking = false;
        SetCursorAnimation("Idle");

        // If still hovering, go back to hover animation, otherwise go to normal
        if (cursorAnimator != null)
        {
            if (isHovering)
            {
                SetCursorAnimation("Hover");
            }
            else
            {
                SetCursorAnimation("Idle");
            }
        }
    }


    private void SetCursorAnimation(string animationName)
    {
        cursorAnimator.SetTrigger(animationName);
    }

    // Call this if you want to show/hide the system cursor
    public void SetSystemCursorVisible(bool visible)
    {
        Cursor.visible = visible;
    }

    // Call this if you want to show/hide your custom cursor
    public void SetCustomCursorVisible(bool visible)
    {
        if (cursorGameObject != null)
        {
            cursorGameObject.SetActive(visible);
        }
    }

    // Useful for temporarily switching back to system cursor (like when opening menus)
    public void SwitchToSystemCursor()
    {
        SetCustomCursorVisible(false);
        SetSystemCursorVisible(true);
    }

    public void SwitchToCustomCursor()
    {
        SetSystemCursorVisible(false);
        SetCustomCursorVisible(true);
    }
}