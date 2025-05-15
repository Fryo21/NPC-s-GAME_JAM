using UnityEngine;
using Pathfinding;

[RequireComponent(typeof(Seeker))]
[RequireComponent(typeof(AIPath))]
public class NpcWanderer : MonoBehaviour
{
    // Components
    private Seeker seeker;
    private AIPath aiPath;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    [Header("Wandering Settings")]
    [SerializeField] private float wanderRadius = 10f;         // How far to search for a random point
    [SerializeField] private float pauseTimeMin = 1f;          // Minimum time to pause at destination
    [SerializeField] private float pauseTimeMax = 3f;          // Maximum time to pause at destination

    // State variables
    private bool isPaused = false;
    private float pauseTimer = 0f;

    private void Awake()
    {
        // Get components
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        seeker = GetComponent<Seeker>();
        aiPath = GetComponent<AIPath>();
    }

    private void Start()
    {
        // Get initial wander point
        FindNewWanderTarget();
    }

    private void Update()
    {
        // If paused, count down timer
        if (isPaused)
        {
            pauseTimer -= Time.deltaTime;
            if (pauseTimer <= 0)
            {
                // Resume movement and find new target
                isPaused = false;
                aiPath.canMove = true;
                FindNewWanderTarget();
            }
            return;
        }

        // Check if reached destination
        if (!aiPath.pathPending && aiPath.reachedEndOfPath)
        {
            StartPause();
        }

        // Update animation
        UpdateAnimation();
    }

    private void FindNewWanderTarget()
    {
        // Try to find a valid random point
        Vector3 randomPoint;
        bool foundPoint = false;
        int attempts = 0;
        const int maxAttempts = 10;

        do
        {
            // Get random direction and distance
            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            float randomDistance = Random.Range(5f, wanderRadius);

            // Calculate target point
            randomPoint = (Vector2)transform.position + randomDirection * randomDistance;

            // Check if the point is walkable
            GraphNode node = AstarPath.active.GetNearest(randomPoint).node;
            foundPoint = node != null && node.Walkable;

            attempts++;
        }
        while (!foundPoint && attempts < maxAttempts);

        // If couldn't find point after max attempts, use a point closer to NPC
        if (!foundPoint)
        {
            randomPoint = (Vector2)transform.position + Random.insideUnitCircle.normalized * 3f;
        }

        // Set destination
        aiPath.destination = randomPoint;
        aiPath.SearchPath();
    }

    private void StartPause()
    {
        isPaused = true;
        pauseTimer = Random.Range(pauseTimeMin, pauseTimeMax);
        aiPath.canMove = false;
    }

    private void UpdateAnimation()
    {
        if (animator == null) return;

        // Get velocity from AIPath 
        Vector2 velocity = aiPath.velocity;

        // Pass velocity values directly to animator for blend tree
        animator.SetFloat("VelocityX", Mathf.Abs(velocity.x));
        animator.SetFloat("VelocityY", velocity.y);

        // Handle sprite flipping based on horizontal direction
        if (velocity.x < -0.1f)
        {
            spriteRenderer.flipX = true;
        }
        else if (velocity.x > 0.1f)
        {
            spriteRenderer.flipX = false;
        }
    }

    // Public method to stop wandering (for external systems)
    public void StopWandering()
    {
        aiPath.canMove = false;
    }

    // Public method to resume wandering (for external systems)
    public void ResumeWandering()
    {
        if (isPaused) return;

        aiPath.canMove = true;
        FindNewWanderTarget();
    }
}