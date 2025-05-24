using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{

    [SerializeField] private float moveSpeed = 1f;

    private PlayerControls playerControls;
    private Vector2 moveInput;
    private Rigidbody2D rb;

    private void Awake()
    {
        playerControls = new PlayerControls();
        rb = GetComponent<Rigidbody2D>();
    }
    private void OnEnable()
    {
        playerControls.Enable();
    }
    private void OnDisable()
    {
        if (playerControls != null)
        {
            playerControls.Disable();
        }
    }
    private void Start()
    {
        playerControls.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        playerControls.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        playerControls.Player.EscMenuToggle.performed += ctx =>
        {
            // Handle escape menu toggle here
            Debug.Log("Escape menu toggled");
        };
    }
    private void Update()
    {
        // This is where you can add any other update logic if needed
    }
    private void FixedUpdate()
    {
        MoveInput();
    }
    private void MoveInput()
    {
        rb.MovePosition(rb.position + moveInput * (moveSpeed * Time.fixedDeltaTime));
    }
}
