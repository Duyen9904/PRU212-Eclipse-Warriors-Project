using System.Collections;
using UnityEngine;

public class BulletImpact : MonoBehaviour
{
	[SerializeField] private int damage = 2;
	[SerializeField] private float moveSpeed = 10f;
	[SerializeField] private float skillDuration = 2f;
	public bool IsActingComplete { get; private set; }
	private Animator animator;


	private void Awake()
	{
		animator = GetComponent<Animator>();
	}

	public void Run()
	{
		StartCoroutine(Impact());
	}

	public IEnumerator Impact()
	{
		if (KnightControlller.Instance != null && transform.parent != null)
		{
			Vector3 startPosition = transform.parent.position;
			Vector3 targetPosition = KnightControlller.Instance.transform.position;

			Vector3 direction = (targetPosition - startPosition).normalized;

			float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
			transform.rotation = Quaternion.Euler(0, 0, angle);


			transform.position = startPosition;
			//transform.localScale = new Vector3(facingDirection, transform.localScale.y, transform.localScale.z);

			animator.SetFloat("AnimationSpeed", 1f / skillDuration);
			animator.SetTrigger("StartAnimation");

			while (Vector3.Distance(transform.position, targetPosition) > 0.1f)
			{
				float step = moveSpeed * Time.deltaTime; // Tính toán bước di chuyển
				transform.position = Vector3.MoveTowards(transform.position, targetPosition, step);

				AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
				if (stateInfo.IsName("Shoot Animation") && stateInfo.normalizedTime >= 1f)
				{
					break;
				}

				yield return null; // Chờ frame tiếp theo
			}

			yield return new WaitForSeconds(0.5f); // Đợi thêm chút để animation hoàn tất

			gameObject.SetActive(false);
		}

	}


	private void OnTriggerEnter2D(Collider2D collision)
	{
		Debug.Log("Va chạm với: " + collision.gameObject.name);
		if (collision.gameObject.GetComponent<PlayerController>())
		{
			PlayerHealth playerHealth = PlayerController.Instance.GetComponent<PlayerHealth>();
			Debug.Log("take dmg from the shoot skill: " + damage);
			playerHealth.TakeDamage(damage, this.transform);
		}
	}
}
