using System.Collections;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class NpcWanderer : MonoBehaviour
{
    [Header("Wanderer Settings")]
    public float wandererSpeed = 3f;
    //public float wandererRadius = 5f;
    public float minWandererWaitTime = 1f;
    public float maxWandererWaitTime = 2f;

    [Header("Avoidance Settings")]
    //public float wandererAvoidanceRadius = 1f;
    //public float wandererAvoidanceStrength = 1f;
    public LayerMask obstacleLayer;

   // [Header("Boundary Settings")]
   // public BoxCollider2D boundaryBox;
   // public LayerMask obstacleLayer;

    //  private Bounds boundaryBounds;
    private Vector2 wanderingPoint;
    private bool isWandering;
    void Start()
    {
        // boundaryBounds = boundaryBox.bounds;
        //PickDirection();
        StartCoroutine(MoveRoutine());
    }

    /*
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

      Collider2D[] obstacles = Physics2D.OverlapCircleAll(transform.position, wandererAvoidanceRadius, obstacleLayer);
      foreach (var  obstacle in obstacles)
      {
          Vector2 away = (Vector2)transform.position - (Vector2)obstacle.transform.position;
          avoidance += away.normalized * wandererAvoidanceStrength;
      }

      Vector2 finalDirertion = (direction + avoidance * wandererAvoidanceStrength).normalized;


              Vector2 nextPosition = (Vector2)transform.position + (Vector2)finalDirertion * wandererSpeed * Time.deltaTime;

              RaycastHit2D hitInfo = Physics2D.Raycast(transform.position, finalDirertion, wandererRadius, obstacleLayer);
              if (hitInfo.collider)
              {
                  PickDirection();
                  return;
              }

              if (!boundaryBounds.Contains(nextPosition))
              {
                  PickDirectionAwayFromBoundary();

                  return;
              }


              transform.position = nextPosition;

              transform.position += (Vector3)finalDirertion * wandererSpeed * Time.deltaTime;
              if (Vector2.Distance(transform.position, wanderingPoint) < 0.1f)
              {
                  StartCoroutine(WaitAndMove());
              }
          }
      */
    public IEnumerator MoveRoutine()
    {
        while (true)
        {
            if (!isWandering)
            {
                Vector2 nextPos = GetNextValidPosition();
                if (nextPos != (Vector2)transform.position)
                {
                    yield return StartCoroutine(MoveToPosition(nextPos));
                }
                else
                {
                    yield return new WaitForSeconds(Random.Range(minWandererWaitTime, maxWandererWaitTime));
                }
            }
            yield return null;
        }
    }
    public Vector2 GetNextValidPosition()
    {
        Vector2[] randomDirection = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        Vector2 currentPos = transform.position;

        for (int i = 0; i < randomDirection.Length; i++)
        {
            int randIndex = Random.Range(i, randomDirection.Length);
            Vector2 temp = randomDirection[i];
            randomDirection[i] = randomDirection[randIndex];
            randomDirection[randIndex] = temp;
        }

        foreach (Vector2 dir in randomDirection)
        {
            Vector2 checkPos = currentPos + dir;
            if (!Physics2D.OverlapCircle(checkPos, 0.1f, obstacleLayer))
            {
                return checkPos;
            }
        }

        return currentPos;
    }
    private IEnumerator MoveToPosition(Vector2 targetPos)
    {
        isWandering = true;
        while ((Vector2)transform.position != targetPos)
        {
            transform.position = Vector2.MoveTowards(transform.position, targetPos, wandererSpeed * Time.deltaTime);

            yield return null;
        }
        isWandering = false;
    }
    /*
    void PickDirection()
    {
        
        for (int i = 0; i < 10; i++)
        {
            Vector2 ranDirection = Random.insideUnitCircle * wandererRadius;

            Vector2 vector2 = (Vector2)transform.position + ranDirection;

            RaycastHit2D hit = Physics2D.Raycast(transform.position, ranDirection, wandererRadius, obstacleLayer);
            if (!hit.collider)
            {
                wanderingPoint = vector2;
                return;
            }
        }
        
        Vector2 randomDirection = Random.insideUnitCircle.normalized * Random.Range(1f, wandererRadius);
        wanderingPoint = (Vector2)transform.position + randomDirection;
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
    */

}
