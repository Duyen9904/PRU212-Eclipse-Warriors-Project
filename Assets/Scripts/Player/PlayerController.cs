using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Top-down RPG player controller with character selection support
/// </summary>
public class PlayerController : Singleton<PlayerController>
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintMultiplier = 1.5f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    // Character Selection
    [Header("Character Selection")]
    [SerializeField] private CharacterDatabase characterDatabase;
    private Character selectedCharacter;
    private int selectedCharacterIndex;

    // Components
    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;

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

    // Death state
    private PlayerHealth playerHealth;
    private bool isDead = false;

    // State
    private bool canMove = true;
    private bool isShooting = false;
    private bool isInitialized = false;

    private Vector2 lastMovementDirection = Vector2.down;

    [Header("Attack")]
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private float attackCooldown = 2f;
    private float lastShootTime = 0f;

    protected override void Awake()
    {
        base.Awake();
        // Get components
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();

        // If animator reference is missing, try to get it
        if (animator == null)
            animator = GetComponent<Animator>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        // Cache animation parameter hashes
        if (animator != null)
        {
            moveXHash = Animator.StringToHash("WalkInputX");
            moveYHash = Animator.StringToHash("WalkInputY");
            isMovingHash = Animator.StringToHash("isWalking");
            isShootingHash = Animator.StringToHash("isShooting");
        }

        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (!isInitialized)
        {
            InitializeController();
        }
    }

    /// <summary>
    /// Initializes the player controller, loading character data and setting up components
    /// Called by CharacterSpawner when instantiating a new player or repositioning an existing one
    /// </summary>
    public void InitializeController()
    {
        // Get the player health component
        playerHealth = GetComponent<PlayerHealth>();

        if (playerHealth != null)
        {
            // Subscribe to the player death event
            playerHealth.OnPlayerDeath += HandlePlayerDeath;
        }
        else
        {
            Debug.LogWarning("PlayerHealth component not found on player!");
        }

        // Load the selected character from PlayerPrefs
        LoadSelectedCharacter();

        // Reset state variables
        isDead = false;
        canMove = true;

        // Set up rigidbody constraints
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        isInitialized = true;

        Debug.Log("Player controller initialized successfully");
    }

    void Update()
    {
        if (isDead) return;
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

    private void FixedUpdate()
    {
        if (isDead) return;
        if (!canMove) return;

        // Calculate movement
        float currentMoveSpeed = moveSpeed * (isSprinting ? sprintMultiplier : 1f);
        Vector2 movement = movementInput * currentMoveSpeed;

        // Apply movement
        rb.linearVelocity = movement;  // Changed from rb.linearVelocity to rb.velocity
    }
    private void OnDestroy()
    {
        // Unsubscribe from events when destroyed
        if (playerHealth != null)
        {
            playerHealth.OnPlayerDeath -= HandlePlayerDeath;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Relic"))
        {
            // Get the Relic component
            Relic relic = other.GetComponent<Relic>();
            if (relic != null)
            {
                // Tell the GameSceneManager that this relic was collected
                GameSceneManager.Instance.CollectRelic(relic.relicIndex);

                // Destroy the relic
                Destroy(other.gameObject);

                Debug.Log("Collected relic: " + relic.relicName);
            }
        }
    }

    private void HandlePlayerDeath()
    {
        isDead = true;

        // Stop movement
        canMove = false;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        // Play death animation if animator exists
        if (animator != null)
        {
            animator.SetTrigger("isDead");
        }

        // You could start a coroutine for respawn logic or game over
        StartCoroutine(HandleDeathSequence());
    }

    private IEnumerator HandleDeathSequence()
    {
        // Wait for death animation to play
        yield return new WaitForSeconds(2f);

        // Here you can implement different behaviors:
        // Option 1: Game Over
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.PlayerDied();
        }
        else if (GameSceneManager.Instance != null)
        {
            GameSceneManager.Instance.LoadGameOver();
        }
        else
        {
            Debug.LogWarning("No level controller or scene manager found for game over handling");
            // Fallback: Destroy the player
            Destroy(gameObject);
        }
    }

    private void LoadSelectedCharacter()
    {
        if (characterDatabase == null)
        {
            Debug.LogError("Character Database not assigned to PlayerController!");
            return;
        }

        // Get the selected character index from PlayerPrefs
        selectedCharacterIndex = PlayerPrefs.GetInt("SelectedCharacter", 0);

        // Ensure the index is valid
        if (selectedCharacterIndex >= characterDatabase.characterCount)
        {
            selectedCharacterIndex = 0;
            PlayerPrefs.SetInt("SelectedCharacter", 0);
        }

        // Get the selected character data
        selectedCharacter = characterDatabase.GetCharacter(selectedCharacterIndex);

        // Apply character visuals and animations
        ApplyCharacterVisuals();

        // Apply character stats
        ApplyCharacterStats();


    }

    private void ApplyCharacterVisuals()
    {
        if (selectedCharacter == null || spriteRenderer == null || animator == null)
            return;

        // Set the default sprite
        spriteRenderer.sprite = selectedCharacter.characterSprite;

        // Set the animator controller if available
        if (selectedCharacter.animatorController != null)
        {
            animator.runtimeAnimatorController = selectedCharacter.animatorController;
        }
        else
        {
            Debug.LogWarning("Selected character doesn't have an animator controller assigned!");
        }
    }

    private void ApplyCharacterStats()
    {
        if (selectedCharacter == null)
            return;

        // Apply movement speed
        moveSpeed = selectedCharacter.moveSpeed;

        // Apply health if PlayerHealth component exists
        PlayerHealth health = GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.SetMaxHealth(selectedCharacter.health);
        }

        // Apply any other character-specific stats
        // This could include attack damage, special abilities, etc.
    }

    private void UpdateAnimation()
    {
        if (animator == null) return;

        // Don't update movement animations if dead
        if (isDead) return;

        // Update movement animation parameters
        bool isMoving = movementInput.magnitude > 0.1f;
        animator.SetBool(isMovingHash, isMoving);

        // Save the last movement direction when moving
        if (isMoving)
        {
            lastMovementDirection = movementInput.normalized;
            animator.SetFloat(moveXHash, movementInput.x);
            animator.SetFloat(moveYHash, movementInput.y);
        }
        else
        {
            // When idle, use the last known direction
            animator.SetFloat(moveXHash, lastMovementDirection.x);
            animator.SetFloat(moveYHash, lastMovementDirection.y);
        }

        // Flip sprite based on last movement direction
        if (spriteRenderer != null)
        {
            if (lastMovementDirection.x < 0)
                spriteRenderer.flipX = true;
            else if (lastMovementDirection.x > 0)
                spriteRenderer.flipX = false;
        }

        // Update shooting parameter
        animator.SetBool(isShootingHash, isShooting);
    }

    private void StartShooting()
    {
        if (Time.time - lastShootTime < attackCooldown) return; 

        lastShootTime = Time.time; 
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0f; 

        Vector2 playerPosition = transform.position;

        GameObject arrowInstance = Instantiate(arrowPrefab, playerPosition, Quaternion.identity);
        arrowInstance.SetActive(true);

        ArrowAttack arrowAttack = arrowInstance.GetComponent<ArrowAttack>();
        arrowAttack.UpdateArrowSprite(selectedCharacter.shootSprites[0]);
        arrowAttack.Shoot(playerPosition, mousePosition);
    }

    private void StopShooting()
    {
        isShooting = false;
        // Add any other logic to stop shooting
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
            if (interactable != null)
            {
                interactable.Interact(this);
                break; // Only interact with one object at a time
            }
        }
    }

    // Inventory management
    public void AddToInventory(string itemId)
    {
        inventory.Add(itemId);
    }

    public bool RemoveFromInventory(string itemId)
    {
        return inventory.Remove(itemId);
    }

    public bool HasItem(string itemId)
    {
        return inventory.Contains(itemId);
    }

    // Public method to control the shooting state
    public void SetShooting(bool shooting)
    {
        isShooting = shooting;
    }

    // Reset player to full health and initial state
    public void ResetPlayer()
    {
        isDead = false;
        canMove = true;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        //if (playerHealth != null)
        //{
        //    playerHealth.Heal(9999); // Full heal
        //}
    }

    public float MoveSpeed
    {
        get { return moveSpeed; }
        set { moveSpeed = value; }
    }

    public float SprintMultiplier
    {
        get { return sprintMultiplier; }
        set { sprintMultiplier = value; }
    }

    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
        Debug.Log($"Player move speed set to: {moveSpeed}");
    }


    // Teleport player to new position
    public void TeleportTo(Vector3 position)
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        transform.position = position;
    }
}

/// <summary>
/// Interface for interactable objects
/// </summary>
public interface IInteractable
{
    void Interact(PlayerController player);
}

