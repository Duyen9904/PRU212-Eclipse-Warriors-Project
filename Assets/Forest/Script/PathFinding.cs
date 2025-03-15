using UnityEngine;

public class PathFinding : MonoBehaviour
{
	[SerializeField] private float moveSpeed = 2f;

	private Rigidbody2D rb;
	private Vector2 moveDir;
	//private Knockback knockback;

	private void Awake()
	{
		rb = GetComponent<Rigidbody2D>();
		//knockback = GetComponent<Knockback>();
	}

	private void FixedUpdate()
	{
		//if (knockback.GettingKnockedBack) return;
		rb.MovePosition(rb.position + moveDir * (moveSpeed * Time.fixedDeltaTime));
	}

	public void MoveTo(Vector2 targetPosition)
	{
		moveDir = targetPosition;
	}

	public void StopMoving()
	{
		moveDir = Vector3.zero;
	}
}
