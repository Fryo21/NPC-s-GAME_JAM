using UnityEngine;

public class DroneAnim : MonoBehaviour
{
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (animator == null) return;

        Vector2 velocity = rb.velocity; 

        if (velocity.x < 0.1f)
        {
            spriteRenderer.flipX = true;
        } else if (velocity.x > 0.1f)
        {
            spriteRenderer.flipX = false;
        }
    
    }
}
