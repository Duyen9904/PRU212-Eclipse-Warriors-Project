using System;
using System.Collections;
using UnityEngine;

public class LizzerController : MonoBehaviour
{
	[Header("Movement Settings")]
	public float moveSpeed = 3f;
	public float detectionRange = 10f;
	public float attackRange = 3f;
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

	private void Awake()
	{
		rb = GetComponent<Rigidbody2D>();
		animator = GetComponent<Animator>();
		spriteRenderer = GetComponent<SpriteRenderer>();
		target = GameObject.FindGameObjectWithTag("Player").transform;
	}

	private void Update()
	{
		float distanceToTarget = Vector2.Distance(transform.position, target.position);
		if (distanceToTarget <= attackRange)
		{
			// Nếu trong tầm đánh, thực hiện tấn công
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
			// Nếu người chơi trong detectionRange -> Chase
			ChasePlayer();
		}
		else
		{
			// Nếu người chơi ngoài detectionRange -> Di chuyển ngẫu nhiên
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
		rb.linearVelocity = Vector2.zero; // Dừng di chuyển khi tấn công
		UpdateSpriteDirection((target.position - transform.position).normalized.x);

		if (canAttack)
		{
			StartCoroutine(PerformAttack());
		}
	}

	private IEnumerator PerformAttack()
	{
		canAttack = false;
		animator.SetTrigger("Attack");

		yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).IsName("Attacking"));
		yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.5f); // Giữa animation


		DealDamageToPlayer();


		yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f);

		yield return new WaitForSeconds(attackCooldown);
		canAttack = true;
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

	private void DealDamageToPlayer()
	{

		Debug.Log("Player Layer: " + LayerMask.LayerToName(target.gameObject.layer));
		Debug.Log("Expected Layer: " + LayerMask.LayerToName(playerLayer));

		Collider2D hitPlayer = Physics2D.OverlapCircle(transform.position + (Vector3.right * (spriteRenderer.flipX ? -1 : 1)), attackRange, playerLayer);
		
		if (hitPlayer != null)
		{
			PlayerController playerHealth = hitPlayer.GetComponent<PlayerController>();
			if (playerHealth != null)
			{
				playerHealth.TakeDamage(attackDamage);
				Debug.Log("Lizzer dealt " + attackDamage + " damage to Player!");
			}
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
