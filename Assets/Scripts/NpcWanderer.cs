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
    [SerializeField] private bool drawGizmos = true;           // Whether to draw debug gizmos

    // State variables
    private bool isPaused = false;
    private float pauseTimer = 0f;
    private Vector3 currentTarget;

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
        // Ensure A* is initialized
        if (AstarPath.active == null)
        {
            Debug.LogError("A* Pathfinding System not found in the scene!");
            enabled = false;
            return;
        }

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
        // Using a better approach with PointOnGraph
        GraphNode randomNode = GetRandomWalkableNode();

        if (randomNode != null)
        {
            // Get the position from the node
            Vector3 targetPoint = (Vector3)randomNode.position;
            currentTarget = targetPoint;

            // Set as destination
            aiPath.destination = targetPoint;
            aiPath.SearchPath();
        }
        else
        {
            // Fallback if no node found
            FallbackTargetSelection();
        }
    }

    private GraphNode GetRandomWalkableNode()
    {
        // Try to get a random walkable node within the wander radius
        GraphNode startNode = AstarPath.active.GetNearest(transform.position).node;
        if (startNode == null) return null;

        // Create a constraint for walkable nodes only
        NNConstraint constraint = NNConstraint.Walkable;

        // Try several random directions
        for (int i = 0; i < 10; i++)
        {
            // Get random direction and distance
            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            float randomDistance = Random.Range(3f, wanderRadius);

            // Calculate target position
            Vector3 targetPosition = transform.position + new Vector3(randomDirection.x, randomDirection.y, 0) * randomDistance;

            // Find the nearest node to this position
            NNInfo info = AstarPath.active.GetNearest(targetPosition, constraint);

            if (info.node != null && info.node.Walkable)
            {
                // Check if we can actually reach this node
                if (PathUtilities.IsPathPossible(startNode, info.node))
                {
                    return info.node;
                }
            }
        }

        return null;
    }

    private void FallbackTargetSelection()
    {
        // Fallback approach - get a nearby random position
        int attempts = 0;
        const int maxAttempts = 10;
        bool foundValidPoint = false;

        while (!foundValidPoint && attempts < maxAttempts)
        {
            attempts++;

            // Get random direction and distance (shorter distance for fallback)
            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            float randomDistance = Random.Range(1f, 5f);

            // Calculate candidate position
            Vector3 candidatePosition = transform.position + new Vector3(randomDirection.x, randomDirection.y, 0) * randomDistance;

            // Find the nearest node on the graph
            NNInfo nearestNodeInfo = AstarPath.active.GetNearest(candidatePosition, NNConstraint.Walkable);

            // Check if the node is walkable
            if (nearestNodeInfo.node != null && nearestNodeInfo.node.Walkable)
            {
                // Use the constrained position from the node 
                Vector3 constrainedPosition = (Vector3)nearestNodeInfo.position;

                // Set target point
                currentTarget = constrainedPosition;
                aiPath.destination = constrainedPosition;
                aiPath.SearchPath();
                foundValidPoint = true;
                break;
            }
        }

        if (!foundValidPoint)
        {
            // If all else fails, try to find ANY walkable position very nearby
            NNInfo nearestNodeInfo = AstarPath.active.GetNearest(transform.position, NNConstraint.Walkable);
            if (nearestNodeInfo.node != null && nearestNodeInfo.node.Walkable)
            {
                Vector3 safePosition = (Vector3)nearestNodeInfo.position;
                currentTarget = safePosition;
                aiPath.destination = safePosition;
                aiPath.SearchPath();
            }
            else
            {
                Debug.LogWarning("Could not find any walkable position for NPC: " + gameObject.name);
            }
        }
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

    // private void OnDrawGizmos()
    // {
    //     if (!drawGizmos || !Application.isPlaying) return;

    //     // Draw wandering radius
    //     Gizmos.color = Color.yellow;
    //     Gizmos.DrawWireSphere(transform.position, wanderRadius);

    //     // Draw current target
    //     if (currentTarget != Vector3.zero)
    //     {
    //         Gizmos.color = Color.green;
    //         Gizmos.DrawSphere(currentTarget, 0.3f);
    //         Gizmos.DrawLine(transform.position, currentTarget);
    //     }
    // }
}