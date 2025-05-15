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

    // Wandering settings
    [Header("Wandering Settings")]
    [SerializeField] private float wanderRadius = 10f;        // How far to wander from start position
    [SerializeField] private float minWanderDistance = 3f;    // Minimum distance for a new point
    [SerializeField] private float nextWaypointDistance = 1f; // When to consider a waypoint reached

    [Header("Movement Settings")]
    [SerializeField] private float speed = 2f;                // Movement speed
    [SerializeField] private float rotationSpeed = 10f;       // How fast to rotate (for sprites)
    [SerializeField] private bool faceMovementDirection = true; // Whether to rotate sprite to face movement direction

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

    private void Start()
    {
        // Get components
        seeker = GetComponent<Seeker>();
        rb = GetComponent<Rigidbody2D>();

        // Store starting position
        startPosition = transform.position;

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
    }

    private void FixedUpdate()
    {
        // Don't move if paused or no path
        if (isPaused || path == null || !isWandering)
        {
            // Optional: You could gradually reduce velocity here for smoother stops
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
        Vector2 force = direction * speed;

        // Apply movement
        rb.velocity = force;

        // Check if we're close enough to the current waypoint
        float distanceToWaypoint = Vector2.Distance(rb.position, path.vectorPath[currentWaypoint]);
        if (distanceToWaypoint < nextWaypointDistance)
        {
            currentWaypoint++;
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
        }
        while (Vector3.Distance(transform.position, newTarget) < minWanderDistance && attempts < 10);

        targetPosition = newTarget;
    }

    private void StartPause()
    {
        isPaused = true;
        pauseTimer = Random.Range(pauseTimeMin, pauseTimeMax);
        rb.velocity = Vector2.zero; // Stop movement
    }

    // Optional: Public methods to control NPC behavior from outside
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

    // Optional: Gizmos to visualize the wander radius in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(Application.isPlaying ? startPosition : transform.position, wanderRadius);
    }

    // Optional: Set a specific target location (for integrating with other systems)
    public void SetTarget(Vector3 target)
    {
        isWandering = true;
        isPaused = false;
        targetPosition = target;
        CalculatePath();
    }
}