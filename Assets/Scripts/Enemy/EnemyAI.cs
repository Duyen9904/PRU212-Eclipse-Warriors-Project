// EnemyAI.cs - Basic enemy AI for top-down games
using UnityEngine;
using System.Collections;

public class EnemyAI : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float detectionRange = 8f;
    public float attackRange = 1.5f;
    public float stoppingDistance = 1f;

    [Header("Attack Settings")]
    public int attackDamage = 10;
    public float attackCooldown = 1.5f;
    private bool canAttack = true;

    [Header("Patrol Settings")]
    public Transform[] patrolPoints;
    public float patrolWaitTime = 1f;
    private int currentPatrolIndex = 0;
    private bool isWaiting = false;

    [Header("References")]
    private Rigidbody2D rb;
    private Animator animator;
    private Transform player;
    private SpriteRenderer spriteRenderer;

    public enum EnemyState { Patrol, Chase, Attack }

    [Header("State Tracking")]
    public EnemyState currentState = EnemyState.Patrol;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    private void Update()
    {
        // Update state based on distance to player
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= attackRange)
        {
            currentState = EnemyState.Attack;
        }
        else if (distanceToPlayer <= detectionRange)
        {
            currentState = EnemyState.Chase;
        }
        else
        {
            currentState = EnemyState.Patrol;
        }

        // Handle different states
        switch (currentState)
        {
            case EnemyState.Patrol:
                Patrol();
                break;
            case EnemyState.Chase:
                ChasePlayer();
                break;
            case EnemyState.Attack:
                AttackPlayer();
                break;
        }

        // Update animations
        UpdateAnimator();
    }

    private void Patrol()
    {
        // If no patrol points, stand still
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // If waiting at patrol point, don't move
        if (isWaiting)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Get current patrol target
        Transform target = patrolPoints[currentPatrolIndex];

        // Check if we reached the target
        if (Vector2.Distance(transform.position, target.position) < 0.1f)
        {
            // Start waiting
            StartCoroutine(WaitAtPatrolPoint());
            return;
        }

        // Move towards patrol point
        Vector2 direction = (target.position - transform.position).normalized;
        rb.linearVelocity = direction * moveSpeed;

        // Update sprite direction
        UpdateSpriteDirection(direction.x);
    }

    private IEnumerator WaitAtPatrolPoint()
    {
        isWaiting = true;
        rb.linearVelocity = Vector2.zero;

        yield return new WaitForSeconds(patrolWaitTime);

        // Move to next patrol point
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        isWaiting = false;
    }

    private void ChasePlayer()
    {
        // Calculate direction to player
        Vector2 direction = (player.position - transform.position).normalized;

        // Move towards player
        rb.linearVelocity = direction * moveSpeed;

        // Update sprite direction
        UpdateSpriteDirection(direction.x);
    }

    private void AttackPlayer()
    {
        // Stop moving when attacking
        rb.linearVelocity = Vector2.zero;

        // Face player
        Vector2 direction = (player.position - transform.position).normalized;
        UpdateSpriteDirection(direction.x);

        // Attack if not on cooldown
        if (canAttack)
        {
            StartCoroutine(PerformAttack());
        }
    }

    private IEnumerator PerformAttack()
    {
        canAttack = false;

        // Play attack animation
        animator.SetTrigger("Attack");

        // Wait for animation to reach damage frame (approximately 0.3 seconds)
        yield return new WaitForSeconds(0.3f);

        // Check if player is still in range
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        if (distanceToPlayer <= attackRange)
        {
            // Damage player
            player.GetComponent<PlayerStats>()?.TakeDamage(attackDamage);
        }

        // Wait for cooldown
        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }

    private void UpdateAnimator()
    {
        // Set movement parameters
        animator.SetFloat("MoveX", rb.linearVelocity.x);
        animator.SetFloat("MoveY", rb.linearVelocity.y);
        animator.SetFloat("MoveMagnitude", rb.linearVelocity.magnitude);

        // Set state parameters
        animator.SetBool("IsChasing", currentState == EnemyState.Chase);
        animator.SetBool("IsAttacking", currentState == EnemyState.Attack);
    }

    private void UpdateSpriteDirection(float directionX)
    {
        if (directionX > 0)
        {
            spriteRenderer.flipX = false;
        }
        else if (directionX < 0)
        {
            spriteRenderer.flipX = true;
        }
    }

    // Helper method to visualize ranges in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Draw patrol path if available
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] != null)
                {
                    Vector3 pos = patrolPoints[i].position;
                    Gizmos.DrawSphere(pos, 0.3f);

                    // Draw lines between patrol points
                    if (i < patrolPoints.Length - 1 && patrolPoints[i + 1] != null)
                    {
                        Gizmos.DrawLine(pos, patrolPoints[i + 1].position);
                    }
                    else if (patrolPoints[0] != null) // Connect last to first
                    {
                        Gizmos.DrawLine(pos, patrolPoints[0].position);
                    }
                }
            }
        }
    }
}