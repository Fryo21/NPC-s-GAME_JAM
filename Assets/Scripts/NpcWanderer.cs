using UnityEngine;
using Pathfinding;

[RequireComponent(typeof(Seeker))]
[RequireComponent(typeof(Rigidbody2D))]
public class NpcWanderer : MonoBehaviour
{
    // A* Pathfinding components
    private Seeker seeker;
    private Rigidbody2D rb;
    private Path path;

    // Animation components
    [Header("Animation")]
    [SerializeField] private Animator animator;
    private SpriteRenderer spriteRenderer;

    // Wandering settings
    [Header("Wandering Settings")]
    [SerializeField] private float wanderRadius = 10f;        // How far to wander from start position
    [SerializeField] private float minWanderDistance = 3f;    // Minimum distance for a new point
    [SerializeField] private float nextWaypointDistance = 1f; // When to consider a waypoint reached
    [SerializeField] private BoxCollider2D boundaryBox;       // Optional boundary constraint

    [Header("Movement Settings")]
    [SerializeField] private float speed = 2f;                // Movement speed

    [Header("Avoidance Settings")]
    [SerializeField] private float wandererAvoidanceRadius = 1f;
    [SerializeField] private float wandererAvoidanceStrength = 1f;

    [Header("Timing Settings")]
    [SerializeField] private float repathRate = 0.5f;         // How often to recalculate path
    [SerializeField] private float pauseTimeMin = 1f;         // Minimum time to pause at destination
    [SerializeField] private float pauseTimeMax = 3f;         // Maximum time to pause at destination

    // State variables
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private int currentWaypoint = 0;
    private bool reachedEndOfPath = false;
    private bool isWandering = true;
    private bool isPaused = false;
    private float pauseTimer = 0f;
    private float pathUpdateTimer = 0f;
    private Bounds boundaryBounds;

    private void Awake()
    {
        // Get the sprite renderer component
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        // Get components
        seeker = GetComponent<Seeker>();
        rb = GetComponent<Rigidbody2D>();

        // Store starting position
        startPosition = transform.position;

        // Set up boundary if provided
        if (boundaryBox != null)
        {
            boundaryBounds = boundaryBox.bounds;
        }

        // Calculate initial target position
        CalculateNewWanderTarget();

        // Start pathfinding
        CalculatePath();
    }

    private void Update()
    {
        // Handle pause at destination
        if (isPaused)
        {
            pauseTimer -= Time.deltaTime;
            if (pauseTimer <= 0)
            {
                isPaused = false;
                CalculateNewWanderTarget();
                CalculatePath();
            }
            return;
        }

        // Update path periodically
        pathUpdateTimer -= Time.deltaTime;
        if (pathUpdateTimer <= 0 && isWandering)
        {
            pathUpdateTimer = repathRate;
            CalculatePath();
        }

        // Update animation based on velocity
        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        // Don't move if paused or no path
        if (isPaused || path == null || !isWandering)
        {
            // If stopped, set velocity to zero
            if (rb.velocity.magnitude > 0.1f)
            {
                rb.velocity = Vector2.zero;
            }
            return;
        }

        // Check if we've reached the end of the path
        if (currentWaypoint >= path.vectorPath.Count)
        {
            reachedEndOfPath = true;
            StartPause();
            return;
        }
        else
        {
            reachedEndOfPath = false;
        }

        // Direction to the next waypoint
        Vector2 direction = ((Vector2)path.vectorPath[currentWaypoint] - rb.position).normalized;

        // Calculate avoidance vector for other NPCs
        Vector2 avoidance = CalculateAvoidance();

        // Combine direction with avoidance
        Vector2 finalDirection = (direction + avoidance).normalized;

        // Check boundary constraint if available
        if (boundaryBox != null)
        {
            Vector2 nextPosition = rb.position + finalDirection * speed * Time.fixedDeltaTime;
            if (!boundaryBounds.Contains(nextPosition))
            {
                // If we're going to hit the boundary, redirect toward center
                Vector2 directionToCenter = ((Vector2)boundaryBounds.center - rb.position).normalized;
                Vector2 randomOffset = Random.insideUnitCircle.normalized * 0.5f;
                finalDirection = (directionToCenter + randomOffset).normalized;

                // Recalculate target position away from boundary
                targetPosition = ClampToBounds((Vector2)transform.position + finalDirection * wanderRadius);
                CalculatePath();
            }
        }

        // Apply movement
        Vector2 force = finalDirection * speed;
        rb.velocity = force;

        // Check if we're close enough to the current waypoint
        float distanceToWaypoint = Vector2.Distance(rb.position, path.vectorPath[currentWaypoint]);
        if (distanceToWaypoint < nextWaypointDistance)
        {
            currentWaypoint++;
        }
    }

    private Vector2 CalculateAvoidance()
    {
        Vector2 avoidance = Vector2.zero;

        // Find all nearby NPCs
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, wandererAvoidanceRadius);

        foreach (Collider2D hit in hits)
        {
            if (hit.gameObject != gameObject && hit.CompareTag("NPC_Wanderer"))
            {
                Vector2 change = (Vector2)transform.position - (Vector2)hit.transform.position;
                avoidance += change.normalized * wandererAvoidanceStrength;
            }
        }

        return avoidance;
    }

    private void UpdateAnimation()
    {
        if (animator != null)
        {
            // Use velocity for animation parameters
            Vector2 velocity = rb.velocity;

            // Update animator parameters
            animator.SetFloat("VelocityX", Mathf.Abs(velocity.x));
            animator.SetFloat("VelocityY", velocity.y);

            // Set sideways animation flag based on vertical movement
            if (velocity.y <= 0.3f)
            {
                animator.SetBool("IsSideways", true);
            }
            else
            {
                animator.SetBool("IsSideways", false);
            }

            // Flip sprite based on horizontal movement direction
            if (velocity.x >= 0.1f)
            {
                spriteRenderer.flipX = false;
            }
            else if (velocity.x < 0)
            {
                spriteRenderer.flipX = true;
            }
        }
    }

    private void CalculatePath()
    {
        if (seeker.IsDone())
        {
            seeker.StartPath(transform.position, targetPosition, OnPathComplete);
        }
    }

    private void OnPathComplete(Path p)
    {
        if (!p.error)
        {
            path = p;
            currentWaypoint = 0;
        }
        else
        {
            Debug.LogWarning("Path error: " + p.errorLog);

            // If path failed, try a new target after a short delay
            Invoke("CalculateNewWanderTarget", 1f);
        }
    }

    private void CalculateNewWanderTarget()
    {
        // Try to find a valid point that's not too close
        int attempts = 0;
        Vector3 newTarget;

        do
        {
            // Get random point within circle
            Vector2 randomDirection = Random.insideUnitCircle * wanderRadius;
            newTarget = startPosition + new Vector3(randomDirection.x, randomDirection.y, 0);
            attempts++;

            // Clamp to boundary if we have one
            if (boundaryBox != null)
            {
                newTarget = ClampToBounds(newTarget);
            }
        }
        while (Vector3.Distance(transform.position, newTarget) < minWanderDistance && attempts < 10);

        targetPosition = newTarget;
    }

    private Vector2 ClampToBounds(Vector2 point)
    {
        if (boundaryBox == null) return point;

        float clampedX = Mathf.Clamp(point.x, boundaryBounds.min.x + 0.5f, boundaryBounds.max.x - 0.5f);
        float clampedY = Mathf.Clamp(point.y, boundaryBounds.min.y + 0.5f, boundaryBounds.max.y - 0.5f);

        return new Vector2(clampedX, clampedY);
    }

    private void StartPause()
    {
        isPaused = true;
        pauseTimer = Random.Range(pauseTimeMin, pauseTimeMax);
        rb.velocity = Vector2.zero; // Stop movement
    }

    // Public methods to control NPC behavior from outside
    public void StopWandering()
    {
        isWandering = false;
        rb.velocity = Vector2.zero;
    }

    public void ResumeWandering()
    {
        isWandering = true;
        CalculateNewWanderTarget();
        CalculatePath();
    }

    // Gizmos for visualization in the editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(Application.isPlaying ? startPosition : transform.position, wanderRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(Application.isPlaying ? transform.position : transform.position, wandererAvoidanceRadius);
    }

    // Set a specific target location (for integrating with other systems)
    public void SetTarget(Vector3 target)
    {
        isWandering = true;
        isPaused = false;
        targetPosition = target;
        CalculatePath();
    }
}