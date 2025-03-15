using System;
using System.Collections;
using UnityEngine;

public class LizzerController : MonoBehaviour
{
	[Header("Movement Settings")]
	public float moveSpeed = 3f;
	public float detectionRange = 10f;
	public float attackRange = 100f;
	public float stoppingDistance = 1f;
	public float moveRadius = 5f;

	[Header("Attack Settings")]
	public int attackDamage = 10;
	public float attackCooldown = 1.5f;
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

	public LayerMask playerLayer; // Thêm biến này


	private Vector2 randomDestination;
	private float changeDirectionTime = 2f;
	private float timer = 0f;

	public enum EnemyState { Patrol, Chase, Attack }
	public EnemyState currentState = EnemyState.Patrol;


	[Header("Skill")]
	[SerializeField] private BulletImpact bulletImpact;
	private bool isAttacking = false;


	private void Awake()
	{
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
			// Nếu trong tầm phát hiện, đuổi theo người chơi
			ChasePlayer();
		}
		else
		{
			// Nếu không phát hiện người chơi, di chuyển ngẫu nhiên
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
		Vector2 direction = (target.position - transform.position).normalized;
		rb.linearVelocity = direction * moveSpeed;
		UpdateSpriteDirection(direction.x);
	}

	private void WanderRandomly()
	{
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
		canAttack = false;
		animator.SetTrigger("Attack");

		yield return new WaitForSeconds(0.5f);
		if (!bulletImpact.gameObject.activeInHierarchy)
		{
			bulletImpact.gameObject.SetActive(true);
		}
		
		yield return StartCoroutine(bulletImpact.Impact());
		canAttack = true;
		isAttacking = false;
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

	//private void DealDamageToPlayer()
	//{

	//}


	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(transform.position, detectionRange);

		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(transform.position, attackRange);
	}
}
