using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class KnightControlller : Singleton<KnightControlller>
{
	[Header("Movement Settings")]
	[SerializeField] private float moveSpeed = 5f;
	[SerializeField] private float sprintMultiplier = 1.5f;
	[SerializeField] private float diagonalMovementModifier = 0.7071f; // Approximately 1/sqrt(2)

	[Header("Animation")]
	[SerializeField] private Animator animator;
	[SerializeField] private SpriteRenderer spriteRenderer;

	[Header("Player Stats")]
	[SerializeField] private int maxHealth = 100;
	[SerializeField] private int currentHealth = 100;

	// Components
	private Rigidbody2D rb;
	private BoxCollider2D boxCollider;

	[Header("Skill")]
	[SerializeField] private BulletImpact bulletImpact;

	// Movement
	private Vector2 movementInput;
	private bool isSprinting = false;

	// Inventory
	private List<string> inventory = new List<string>();

	// Animation parameter hashes (for performance)
	private int moveXHash;
	private int moveYHash;
	private int isMovingHash;
	private int isShootingHash;

	// State
	private bool canMove = true;
	private bool isShooting = false; // Add this field

	// Add this method to control the shooting state
	public void SetShooting(bool shooting)
	{
		isShooting = shooting;
	}

	protected override void Awake()
	{
		base.Awake();
		// Get components
		rb = GetComponent<Rigidbody2D>();
		boxCollider = GetComponent<BoxCollider2D>();

		animator = GetComponent<Animator>();
		spriteRenderer = GetComponent<SpriteRenderer>();

		// Cache animation parameter hashes
		if (animator != null)
		{
			moveXHash = Animator.StringToHash("MoveX");
			moveYHash = Animator.StringToHash("MoveY");
			isMovingHash = Animator.StringToHash("isMoving");
			isShootingHash = Animator.StringToHash("isShooting");
		}
		DontDestroyOnLoad(gameObject);
	}

	private void Start()
	{
		// Register with level manager if it exists
		//if (LTDKLevelManager.Instance != null)
		//{
		//    // Apply any existing player data
		//    ApplyPlayerData(LTDKLevelManager.Instance.playerData);
		//}
	}

	private void Update()
	{
		if (!canMove) return;

		// Get input
		movementInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

		// Normalize diagonal movement to prevent faster diagonal speed
		if (movementInput.magnitude > 1)
		{
			movementInput.Normalize();
		}

		// Sprint
		isSprinting = Input.GetKey(KeyCode.LeftShift);

		// Shooting input (for example, using left mouse button)
		if (Input.GetMouseButtonDown(0))
		{
			StartShooting();
		}
		else if (Input.GetMouseButtonUp(0))
		{
			StopShooting();
		}

		// Update animations
		UpdateAnimation();

		// Interaction input
		if (Input.GetKeyDown(KeyCode.E))
		{
			Interact();
		}
	}

	private void StartShooting()
	{
		isShooting = true;
		// Add any other shooting logic here
	}

	private void StopShooting()
	{
		isShooting = false;
		// Add any other logic to stop shooting
	}

	private void FixedUpdate()
	{
		if (!canMove) return;

		// Calculate movement
		float currentMoveSpeed = moveSpeed * (isSprinting ? sprintMultiplier : 1f);
		Vector2 movement = movementInput * currentMoveSpeed;

		// Apply movement
		rb.linearVelocity = movement;
	}

	private void UpdateAnimation()
	{
		if (animator == null) return;

		// Update movement animation parameters
		bool isMoving = movementInput.magnitude > 0.1f;

		animator.SetBool(isMovingHash, isMoving);

		// Only update direction when actually moving
		if (isMoving)
		{
			animator.SetFloat(moveXHash, movementInput.x);
			animator.SetFloat(moveYHash, movementInput.y);

			// Flip sprite if needed (if your art faces right by default)
			if (spriteRenderer != null)
			{
				if (movementInput.x < 0)
					spriteRenderer.flipX = true;
				else if (movementInput.x > 0)
					spriteRenderer.flipX = false;
			}
		}

		// Update shooting parameter - you'll need to call SetShooting(true/false) when appropriate
		animator.SetBool(isShootingHash, isShooting);
	}

	private void Interact()
	{
		// Cast a small box in front of the player to detect interactable objects
		Vector2 facingDirection = new Vector2(
			animator.GetFloat(moveXHash),
			animator.GetFloat(moveYHash)
		).normalized;

		if (facingDirection.magnitude < 0.1f)
		{
			// Default to facing down if no recent movement
			facingDirection = Vector2.down;
		}

		// Size and distance of interaction box
		Vector2 interactSize = new Vector2(0.5f, 0.5f);
		float interactDistance = 0.5f;

		// Calculate position of interaction box
		Vector2 interactPos = (Vector2)transform.position + (facingDirection * interactDistance);

		// Debug visualization of the interaction area
		Debug.DrawLine(transform.position, (Vector2)transform.position + facingDirection * interactDistance, Color.red, 0.5f);

		// Perform the overlap box check
		Collider2D[] results = Physics2D.OverlapBoxAll(interactPos, interactSize, 0f);

		foreach (var collider in results)
		{
			// Skip player's own collider
			if (collider.gameObject == gameObject) continue;

			// Check if the object is interactable
			IInteractable interactable = collider.GetComponent<IInteractable>();
			//if (interactable != null)
			//{
			//	interactable.Interact(this);
			//	break; // Only interact with one object at a time
			//}
		}
	}

	// Health management
	public void TakeDamage(int damage)
	{
		currentHealth -= damage;

		//if (currentHealth <= 0)
		//{
		//    Die();
		//}

		//// Update LTDK player data
		//if (LTDKLevelManager.Instance != null)
		//{
		//    LTDKLevelManager.Instance.playerData.health = currentHealth;
		//}
	}

	public void Heal(int amount)
	{
		currentHealth = Mathf.Min(currentHealth + amount, maxHealth);

		//// Update LTDK player data
		//if (LTDKLevelManager.Instance != null)
		//{
		//    LTDKLevelManager.Instance.playerData.health = currentHealth;
		//}
	}

	private void Die()
	{
		// Handle player death
		canMove = false;
		rb.linearVelocity = Vector2.zero;

		// Play death animation if available
		if (animator != null)
		{
			animator.SetTrigger("Die");
		}

		// Respawn or game over logic
		StartCoroutine(RespawnAfterDelay(2f));
	}

	private IEnumerator RespawnAfterDelay(float delay)
	{
		yield return new WaitForSeconds(delay);

		// Reset health
		currentHealth = maxHealth;

		//// Update LTDK player data
		//if (LTDKLevelManager.Instance != null)
		//{
		//    LTDKLevelManager.Instance.playerData.health = currentHealth;

		//    // Reload current level or go to checkpoint
		//    string currentLevel = LTDKLevelManager.Instance.GetCurrentLevelId();
		//    LTDKLevelManager.Instance.LoadLevel(currentLevel);
		//}

		canMove = true;
	}

	// Inventory management
	public void AddToInventory(string itemId)
	{
		inventory.Add(itemId);

		//// Update LTDK player data
		//if (LTDKLevelManager.Instance != null)
		//{
		//    LTDKLevelManager.Instance.playerData.inventory.Add(itemId);
		//}
	}

	public bool RemoveFromInventory(string itemId)
	{
		bool removed = inventory.Remove(itemId);

		//// Update LTDK player data
		//if (removed && LTDKLevelManager.Instance != null)
		//{
		//    LTDKLevelManager.Instance.playerData.inventory.Remove(itemId);
		//}

		return removed;
	}

	public bool HasItem(string itemId)
	{
		return inventory.Contains(itemId);
	}

	//// IPlayerController implementation
	//public void ApplyPlayerData(LTDKLevelManager.PlayerData data)
	//{
	//    currentHealth = data.health;
	//    maxHealth = data.maxHealth;
	//    inventory = new List<string>(data.inventory);
	//}

	//    public void GetCurrentPlayerData(LTDKLevelManager.PlayerData data)
	//    {
	//        data.health = currentHealth;
	//        data.maxHealth = maxHealth;
	//        data.inventory = new List<string>(inventory);
	//        data.lastPosition = transform.position;
	//    }
	//}
}
