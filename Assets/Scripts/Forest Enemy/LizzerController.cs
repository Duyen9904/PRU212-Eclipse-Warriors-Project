using System;
using System.Collections;
using UnityEngine;

public class LizzerController : MonoBehaviour
{
	[Header("Movement Settings")]
	public float moveSpeed = 3f;
	public float detectionRange = 15f;
	public float attackRange = 10f;
	public float stoppingDistance = 1f;
	public float moveRadius = 5f;

	private bool canAttack = true;

	[Header("Patrol Settings")]
	public Transform[] patrolPoints;
	public float patrolWaitTime = 1f;
	private int currentPatrolIndex = 0;

	[Header("References")]
	private Rigidbody2D rb;
	private Animator animator;
	private SpriteRenderer spriteRenderer;
	private Transform target; // Thay vì player


	private Vector2 randomDestination;
	private float changeDirectionTime = 2f;
	private float timer = 0f;

	public enum EnemyState { Patrol, Chase, Attack }
	public EnemyState currentState = EnemyState.Patrol;



	[Header("Skill")]
	[SerializeField] private BulletImpact bulletImpact;
	private bool isAttacking = false;
    public int health = 30; // Máu của Lizzer


    private Flash flash;

    private void Awake()
	{
        flash = GetComponent<Flash>();
        rb = GetComponent<Rigidbody2D>();
		animator = GetComponent<Animator>();
		spriteRenderer = GetComponent<SpriteRenderer>();
		target = GameObject.FindGameObjectWithTag("Player").transform;
	}

	private void Update()
	{
		if (isAttacking) return;
		float distanceToTarget = Vector2.Distance(transform.position, target.position);
		if (distanceToTarget <= attackRange)
		{
			AttackTarget();
		}
		else if (distanceToTarget <= detectionRange)
		{
			ChasePlayer();
		}
		else
		{
			WanderRandomly();
		}
	}

	private void WalkAround()
	{
		float distanceToTarget = Vector2.Distance(target.position, transform.position);
		if (distanceToTarget <= detectionRange)
		{
			ChasePlayer();
		}
		else
		{
			WanderRandomly();
		}
	}

	private void ChasePlayer()
	{
		animator.SetBool("IsWalking", true);
		Vector2 direction = (target.position - transform.position).normalized;
		rb.linearVelocity = direction * moveSpeed;
		UpdateSpriteDirection(direction.x);
	}

	private void WanderRandomly()
	{
		animator.SetBool("IsWalking", true);
		timer -= Time.deltaTime;

		if (Vector2.Distance(transform.position, randomDestination) < 0.5f || timer <= 0f)
		{
			Vector2 newDestination;
			float minDistance = 5f;
			float maxDistance = moveRadius * 2.5f;
			do
			{
				newDestination = (Vector2)transform.position +
								 UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(minDistance, maxDistance);
			} while (Vector2.Distance(transform.position, newDestination) < 2f);

			randomDestination = newDestination;

			timer = UnityEngine.Random.Range(5f, 10f);
		}

		Vector2 direction = (randomDestination - (Vector2)transform.position).normalized;
		rb.linearVelocity = direction * moveSpeed;
		UpdateSpriteDirection(direction.x);
	}



	private void AttackTarget()
	{
		if (isAttacking) return;

		isAttacking = true;

		rb.linearVelocity = Vector2.zero; // Dừng di chuyển khi tấn công
		UpdateSpriteDirection((target.position - transform.position).normalized.x);

		StartCoroutine(PerformAttack());
	}

	private IEnumerator PerformAttack()
	{
		//Debug.Log("Attacking");
		canAttack = false;
		animator.SetTrigger("Attack");
		animator.SetBool("IsWalking", false);
		yield return new WaitForSeconds(0.5f);

		animator.SetTrigger("Idle");

		if (!bulletImpact.gameObject.activeInHierarchy)
		{
			bulletImpact.gameObject.SetActive(true);
		}
		
		yield return StartCoroutine(bulletImpact.Impact());
		canAttack = true;
		isAttacking = false;
		animator.SetBool("IsWalking", true);
	}

	private void UpdateSpriteDirection(float directionX)
	{

		if (directionX > 0)
		{
			spriteRenderer.flipX = true;
		}
		else if (directionX < 0)
		{
			spriteRenderer.flipX = false;
		}
	}

    public void TakeDamage(int damage)
    {
        health -= damage;
        Debug.Log("Lizzer took " + damage + " damage! Current HP: " + health);
		StartCoroutine(flash.FlashRoutine());
        if (health <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        Debug.Log("Lizzer died!");
        Destroy(gameObject); 
    }


    private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(transform.position, detectionRange);

		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(transform.position, attackRange);
	}
}
