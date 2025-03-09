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
    public EnemyState currentState = EnemyState.Patrol;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogWarning("⚠ EnemyAI: Không tìm thấy Player, sẽ chỉ tuần tra!");
        }
    }

    private void Update()
    {
        if (player == null)
        {
            currentState = EnemyState.Patrol;
        }
        else
        {
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
        }

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

        UpdateAnimator();
    }

    private void Patrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        if (isWaiting)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        Transform target = patrolPoints[currentPatrolIndex];

        if (Vector2.Distance(transform.position, target.position) < 0.1f)
        {
            StartCoroutine(WaitAtPatrolPoint());
            return;
        }

        Vector2 direction = (target.position - transform.position).normalized;
        rb.velocity = direction * moveSpeed;

        UpdateSpriteDirection(direction);
    }

    private IEnumerator WaitAtPatrolPoint()
    {
        isWaiting = true;
        rb.velocity = Vector2.zero;

        yield return new WaitForSeconds(patrolWaitTime);

        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        isWaiting = false;
    }

    private void ChasePlayer()
    {
        if (player == null) return;

        Vector2 direction = (player.position - transform.position).normalized;
        rb.velocity = direction * moveSpeed;

        UpdateSpriteDirection(direction);
    }

    private void AttackPlayer()
    {
        rb.velocity = Vector2.zero;

        if (player == null) return;

        Vector2 direction = (player.position - transform.position).normalized;
        UpdateSpriteDirection(direction);

        if (canAttack)
        {
            StartCoroutine(PerformAttack(direction));
        }
    }

    private IEnumerator PerformAttack(Vector2 direction)
    {
        canAttack = false;
        animator.SetTrigger("Attack");

        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y)) // Nếu tấn công ngang
        {
            animator.Play("Slash_Side");
            spriteRenderer.flipX = direction.x < 0;
        }
        else if (direction.y > 0)
        {
            animator.Play("Slash_Back");
        }
        else
        {
            animator.Play("Slash_Front");
        }

        yield return new WaitForSeconds(0.3f);

        if (player != null && Vector2.Distance(transform.position, player.position) <= attackRange)
        {
            player.GetComponent<PlayerStats>()?.TakeDamage(attackDamage);
        }

        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }

    private void UpdateAnimator()
    {
        animator.SetFloat("MoveX", rb.velocity.x);
        animator.SetFloat("MoveY", rb.velocity.y);
        animator.SetFloat("MoveMagnitude", rb.velocity.magnitude);

        animator.SetBool("IsChasing", currentState == EnemyState.Chase);
        animator.SetBool("IsAttacking", currentState == EnemyState.Attack);
    }

    private void UpdateSpriteDirection(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y)) // Nếu di chuyển ngang
        {
            animator.Play("Walk_Side");
            spriteRenderer.flipX = direction.x < 0; // Lật nếu đi trái
        }
        else if (direction.y > 0)
        {
            animator.Play("Walk_Back");
        }
        else
        {
            animator.Play("Walk_Front");
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] != null)
                {
                    Vector3 pos = patrolPoints[i].position;
                    Gizmos.DrawSphere(pos, 0.3f);

                    if (i < patrolPoints.Length - 1 && patrolPoints[i + 1] != null)
                    {
                        Gizmos.DrawLine(pos, patrolPoints[i + 1].position);
                    }
                    else if (patrolPoints[0] != null)
                    {
                        Gizmos.DrawLine(pos, patrolPoints[0].position);
                    }
                }
            }
        }
    }
}
