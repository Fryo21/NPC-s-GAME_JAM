using System.Collections;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class NpcWanderer : MonoBehaviour
{
    public float wandererSpeed = 3f;
    public float wandererRadius = 5f;
    public float minWandererWaitTime = 1f;
    public float maxWandererWaitTime = 2f;

    public float wandererAvoidanceRadius = 1f;
    public float wandererAvoidanceStrength = 1f;

    public BoxCollider2D boundaryBox;
    private Bounds boundaryBounds;

    private Vector2 wanderingPoint;
    private bool isWandering;

    public Animator animator;
    private SpriteRenderer spriteRenderer;
    void Start()
    {
        boundaryBounds = boundaryBox.bounds;
        PickDirection();
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    void Update()
    {
        if (isWandering) { return; }

        Vector2 direction = (wanderingPoint - (Vector2)transform.position).normalized;      
        
        Vector2 avoidance = Vector2.zero;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, wandererAvoidanceRadius);

        foreach (Collider2D hit in hits)
        {
            if (hit.gameObject != gameObject && hit.CompareTag("NPC_Wanderer"))
            {
                Vector2 change = (Vector2)transform.position - (Vector2)hit.transform.position;

                avoidance += change.normalized * wandererAvoidanceStrength;
            }
        }

        Vector2 finalDirertion = (direction + avoidance * wandererAvoidanceStrength).normalized;

        //animation
        animator.SetFloat("VelocityX", Mathf.Abs(finalDirertion.x));
        animator.SetFloat("VelocityY", finalDirertion.y);
        horizontal = finalDirertion.x;

        if (finalDirertion.y <= 0.3f)
        {
            animator.SetBool("IsSideways", true);
        } else
        {
            animator.SetBool("IsSideways", false);
        }

        if (finalDirertion.x >= 0.1)
        {
            spriteRenderer.flipX = false;

        } else if (finalDirertion.x < 0)
        {
            spriteRenderer.flipX = true;
        }


        Vector2 nextPosition = (Vector2)transform.position + (Vector2)finalDirertion * wandererSpeed * Time.deltaTime;

        if (!boundaryBounds.Contains(nextPosition))
        {
            PickDirectionAwayFromBoundary();

            return;
        }

        transform.position = nextPosition;

        if (Vector2.Distance(transform.position, wanderingPoint) < 0.1f)
        {
            StartCoroutine(WaitAndMove());
        }
    }
    void PickDirection()
    {
        Vector2 ranDir = Random.insideUnitCircle * wandererRadius;

        wanderingPoint = (Vector2)transform.position + ranDir;
    }
    IEnumerator WaitAndMove()
    {
        float wandererWaitTime = Random.Range(minWandererWaitTime, maxWandererWaitTime);

        isWandering = true;

        yield return new WaitForSeconds(wandererWaitTime);

        PickDirection();

        isWandering = false;
    }
    private void PickDirectionAwayFromBoundary()
    {
        Vector2 directionToCenter = (Vector2)boundaryBounds.center - (Vector2)transform.position;
        
        Vector2 randomOffset = Random.insideUnitCircle.normalized * 0.5f; 
       
        wanderingPoint = (Vector2)transform.position + (directionToCenter + randomOffset).normalized * wandererRadius;

        wanderingPoint = ClampToBounds(wanderingPoint);
    }
    private Vector2 ClampToBounds(Vector2 point)
    {
        float clampedX = Mathf.Clamp(point.x, boundaryBounds.min.x + 0.5f, boundaryBounds.max.x - 0.5f);
        float clampedY = Mathf.Clamp(point.y, boundaryBounds.min.y + 0.5f, boundaryBounds.max.y - 0.5f);

        return new Vector2(clampedX, clampedY);
    }
}
