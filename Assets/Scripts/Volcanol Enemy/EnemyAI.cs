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
    private SpriteRenderer spriteRenderer;
    private Transform target; // Thay vì player

    public enum EnemyState { Patrol, Chase, Attack }
    public EnemyState currentState = EnemyState.Patrol;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Nếu chưa có Player, tạo một dummy target để enemy hướng đến
        if (GameObject.FindGameObjectWithTag("Player") != null)
        {
            target = GameObject.FindGameObjectWithTag("Player").transform;
        }
        else
        {
            GameObject dummyTarget = new GameObject("DummyTarget");
            dummyTarget.transform.position = transform.position + new Vector3(5f, 0f, 0f); // Tạo mục tiêu ảo cách 5 đơn vị
            target = dummyTarget.transform;
        }
    }

    private void Update()
    {
        float distanceToTarget = Vector2.Distance(transform.position, target.position);

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

        Transform targetPoint = patrolPoints[currentPatrolIndex];

        if (Vector2.Distance(transform.position, targetPoint.position) < 0.1f)
        {
            StartCoroutine(WaitAtPatrolPoint());
            return;
        }

        Vector2 direction = (targetPoint.position - transform.position).normalized;
        rb.velocity = direction * moveSpeed;

        UpdateSpriteDirection(direction.x);
    }

    private IEnumerator WaitAtPatrolPoint()
    {
        isWaiting = true;
        rb.velocity = Vector2.zero;

        yield return new WaitForSeconds(patrolWaitTime);

        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        isWaiting = false;
    }

    private void ChaseTarget()
    {
        Vector2 direction = (target.position - transform.position).normalized;
        rb.velocity = direction * moveSpeed;

        UpdateSpriteDirection(direction.x);
    }

    private void AttackTarget()
    {
        rb.velocity = Vector2.zero;

        Vector2 direction = (target.position - transform.position).normalized;
        UpdateSpriteDirection(direction.x);

        if (canAttack)
        {
            StartCoroutine(PerformAttack(direction));
        }
    }

    private IEnumerator PerformAttack(Vector2 direction)
    {
        canAttack = false;
        animator.SetTrigger("Attack");

        // Chọn animation attack theo hướng
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            animator.Play("slashX");
            spriteRenderer.flipX = direction.x < 0;
        }
        else if (direction.y > 0)
        {
            animator.Play("slashBack");
        }
        else
        {
            animator.Play("slashY");
        }

        yield return new WaitForSeconds(0.3f);

        float distanceToTarget = Vector2.Distance(transform.position, target.position);
        if (distanceToTarget <= attackRange)
        {
            Debug.Log("Enemy attacked!");
        }

        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }

    private void UpdateAnimator()
    {
        Vector2 velocity = rb.velocity;
        animator.SetFloat("Horizontal", velocity.x);
        animator.SetFloat("Vertical", velocity.y);
        animator.SetBool("IsMoving", velocity.magnitude > 0);
        animator.SetBool("IsAttacking", currentState == EnemyState.Attack);

        // Chọn animation di chuyển
        if (velocity.magnitude > 0)
        {
            if (Mathf.Abs(velocity.x) > Mathf.Abs(velocity.y))
            {
                animator.Play("walkX");
                spriteRenderer.flipX = velocity.x < 0;
            }
            else if (velocity.y > 0)
            {
                animator.Play("wakBack");
            }
            else
            {
                animator.Play("walkY");
            }
        }
        else if (currentState != EnemyState.Attack)
        {
            // Khi không di chuyển, về trạng thái idle
            if (Mathf.Abs(velocity.x) > Mathf.Abs(velocity.y))
            {
                animator.Play("idleX");
                spriteRenderer.flipX = velocity.x < 0;
            }
            else if (velocity.y > 0)
            {
                animator.Play("idleBack");
            }
            else
            {
                animator.Play("idleY");
            }
        }
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
