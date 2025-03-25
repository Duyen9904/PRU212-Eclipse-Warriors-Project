using UnityEngine;
using System.Collections;

/// <summary>
/// Base class for all enemies with common functionality
/// </summary>
public class BaseEnemyController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] protected float moveSpeed = 3f;
    [SerializeField] protected float detectionRange = 8f;
    [SerializeField] protected float attackRange = 1.5f;
    [SerializeField] protected float stoppingDistance = 1f;

    [Header("Attack Settings")]
    [SerializeField] protected int attackDamage = 10;
    [SerializeField] protected float attackCooldown = 1.5f;
    protected bool canAttack = true;

    [Header("Patrol Settings")]
    [SerializeField] protected Transform[] patrolPoints;
    [SerializeField] protected float patrolWaitTime = 1f;
    protected int currentPatrolIndex = 0;
    protected bool isWaiting = false;

    [Header("References")]
    protected Rigidbody2D rb;
    protected Animator animator;
    protected SpriteRenderer spriteRenderer;
    protected Transform target; // Player target

    // State machine
    public enum EnemyState { Patrol, Chase, Attack, Stunned }
    [SerializeField] protected EnemyState currentState = EnemyState.Patrol;

    // Animation parameters
    protected bool isWalking = false;
    protected bool isShooting = false;
    protected bool isDead = false;
    protected float lastInputX = 0;
    protected float lastInputY = -1; // Default facing down

    protected virtual void Awake()
    {
        // Get required components
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            target = playerObj.transform;
        }
        else
        {
            Debug.LogWarning("Player not found! Enemy will not chase.");
        }
    }

    protected virtual void Update()
    {
        if (isDead) return;

        // Update state based on distance to player
        if (target != null)
        {
            float distanceToTarget = Vector2.Distance(transform.position, target.position);

            // Update state based on distance
            if (distanceToTarget <= attackRange)
            {
                currentState = EnemyState.Attack;
            }
            else if (distanceToTarget <= detectionRange)
            {
                currentState = EnemyState.Chase;
            }
            else
            {
                currentState = EnemyState.Patrol;
            }
        }

        // Handle behavior based on current state
        switch (currentState)
        {
            case EnemyState.Patrol:
                Patrol();
                break;
            case EnemyState.Chase:
                ChaseTarget();
                break;
            case EnemyState.Attack:
                AttackTarget();
                break;
            case EnemyState.Stunned:
                // Do nothing when stunned
                rb.linearVelocity = Vector2.zero;
                break;
        }

        // Update animator
        UpdateAnimator();
    }

    protected virtual void Patrol()
    {
        // If no patrol points or currently waiting, don't move
        if (patrolPoints == null || patrolPoints.Length == 0 || isWaiting)
        {
            rb.linearVelocity = Vector2.zero;
            isWalking = false;
            return;
        }

        // Get current patrol point
        Transform targetPoint = patrolPoints[currentPatrolIndex];
        if (targetPoint == null)
        {
            rb.linearVelocity = Vector2.zero;
            isWalking = false;
            return;
        }

        // Calculate distance to current patrol point
        float distance = Vector2.Distance(transform.position, targetPoint.position);

        // If reached the patrol point
        if (distance < 0.1f)
        {
            StartCoroutine(WaitAtPatrolPoint());
            return;
        }

        // Move towards patrol point
        Vector2 direction = (targetPoint.position - transform.position).normalized;
        rb.linearVelocity = direction * moveSpeed;

        // Set animation parameters
        isWalking = true;
        UpdateFacingDirection(direction);
    }

    protected virtual void ChaseTarget()
    {
        if (target == null) return;

        // Calculate direction to target
        Vector2 direction = (target.position - transform.position).normalized;

        // If within stopping distance, stop
        float distance = Vector2.Distance(transform.position, target.position);
        if (distance <= stoppingDistance)
        {
            rb.linearVelocity = Vector2.zero;
            isWalking = false;
        }
        else
        {
            // Move towards target
            rb.linearVelocity = direction * moveSpeed;
            isWalking = true;
        }

        // Update facing direction
        UpdateFacingDirection(direction);
    }

    protected virtual void AttackTarget()
    {
        if (target == null) return;

        // Stop moving
        rb.linearVelocity = Vector2.zero;
        isWalking = false;

        // Face the target
        Vector2 direction = (target.position - transform.position).normalized;
        UpdateFacingDirection(direction);

        // Attack if possible
        if (canAttack)
        {
            StartCoroutine(PerformAttack());
        }
    }

    protected virtual IEnumerator PerformAttack()
    {
        canAttack = false;
        isShooting = true;

        // Trigger attack animation
        animator.SetBool("isShooting", true);

        // Wait for animation to play
        yield return new WaitForSeconds(0.3f);

        // Deal damage if target is still in range
        if (target != null)
        {
            float distance = Vector2.Distance(transform.position, target.position);
            if (distance <= attackRange)
            {
                PlayerStats playerStats = target.GetComponent<PlayerStats>();
                if (playerStats != null)
                {
                    playerStats.TakeDamage(attackDamage);
                }
            }
        }

        // Reset shooting state
        isShooting = false;
        animator.SetBool("isShooting", false);

        // Wait for cooldown
        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }

    protected virtual IEnumerator WaitAtPatrolPoint()
    {
        isWaiting = true;
        rb.linearVelocity = Vector2.zero;
        isWalking = false;

        yield return new WaitForSeconds(patrolWaitTime);

        // Move to next patrol point
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        isWaiting = false;
    }

    protected virtual void UpdateFacingDirection(Vector2 direction)
    {
        if (direction.magnitude > 0.1f)
        {
            lastInputX = direction.x;
            lastInputY = direction.y;

            // Update animator parameters
            animator.SetFloat("LastInputX", lastInputX);
            animator.SetFloat("LastInputY", lastInputY);
            animator.SetFloat("WalkInputX", lastInputX);
            animator.SetFloat("WalkInputY", lastInputY);

            // Flip sprite if needed
            if (direction.x < 0)
            {
                spriteRenderer.flipX = true;
            }
            else if (direction.x > 0)
            {
                spriteRenderer.flipX = false;
            }
        }
    }

    protected virtual void UpdateAnimator()
    {
        // Set walking state
        animator.SetBool("isWalking", isWalking);

        // Dead state
        animator.SetBool("isDead", isDead);
    }

    public virtual void TakeDamage(int damage)
    {
        // This should be implemented by the EnemyHealth component
        // But we can add visual feedback here
        StartCoroutine(FlashSprite());
    }

    protected virtual IEnumerator FlashSprite()
    {
        if (spriteRenderer == null) yield break;

        Color originalColor = spriteRenderer.color;
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = originalColor;
    }

    public virtual void Die()
    {
        // Stop movement
        rb.linearVelocity = Vector2.zero;

        // Disable physics
        rb.simulated = false;
        if (GetComponent<Collider2D>() != null)
        {
            GetComponent<Collider2D>().enabled = false;
        }

        // Set dead state
        isDead = true;
        animator.SetBool("isDead", true);

        // Destroy after animation plays
        Destroy(gameObject, 2f);
    }

    protected virtual void OnDrawGizmosSelected()
    {
        // Draw detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Draw patrol path
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] != null)
                {
                    Vector3 pos = patrolPoints[i].position;

                    // Draw point
                    Gizmos.DrawSphere(pos, 0.2f);

                    // Draw line to next point
                    if (i < patrolPoints.Length - 1 && patrolPoints[i + 1] != null)
                    {
                        Gizmos.DrawLine(pos, patrolPoints[i + 1].position);
                    }
                    else if (i == patrolPoints.Length - 1 && patrolPoints[0] != null)
                    {
                        // Connect last to first
                        Gizmos.DrawLine(pos, patrolPoints[0].position);
                    }
                }
            }
        }
    }
}