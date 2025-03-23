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
    [SerializeField] private float diagonalMovementModifier = 0.7071f; // Approximately 1/sqrt(2)

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    // Player Stats are now handled by PlayerHealth component

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
            moveXHash = Animator.StringToHash("MoveX");
            moveYHash = Animator.StringToHash("MoveY");
            isMovingHash = Animator.StringToHash("isMoving");
            isShootingHash = Animator.StringToHash("isShooting");
        }

        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Get the player health component
        playerHealth = GetComponent<PlayerHealth>();

        if (playerHealth != null)
        {
            // Subscribe to the player death event
            playerHealth.OnPlayerDeath += HandlePlayerDeath;
        }

        // Load the selected character from PlayerPrefs
        LoadSelectedCharacter();
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

        // Calculate movement
        float currentMoveSpeed = moveSpeed * (isSprinting ? sprintMultiplier : 1f);
        Vector2 movement = movementInput * currentMoveSpeed;

        // Apply movement
        rb.linearVelocity = movement;

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

    private void OnDestroy()
    {
        // Unsubscribe from events when destroyed
        if (playerHealth != null)
        {
            playerHealth.OnPlayerDeath -= HandlePlayerDeath;
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
            animator.SetTrigger("Die");
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
        // GameManager.Instance.GameOver();

        // Option 2: Respawn
        // Respawn();

        // Option 3: Destroy the player
        Destroy(gameObject);
    }

    private void LoadSelectedCharacter()
    {
        if (characterDatabase == null)
        {
            Debug.LogError("Character Database not assigned to PlayerController!");
            return;
        }

        // Get the selected character index from PlayerPrefs
        selectedCharacterIndex = PlayerPrefs.GetInt("SelectedOption", 0);

        // Ensure the index is valid
        if (selectedCharacterIndex >= characterDatabase.characterCount)
        {
            selectedCharacterIndex = 0;
            PlayerPrefs.SetInt("SelectedOption", 0);
        }

        // Get the selected character data
        selectedCharacter = characterDatabase.GetCharacter(selectedCharacterIndex);

        // Apply character visuals and animations
        ApplyCharacterVisuals();
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

    protected virtual void UpdateAnimation()
    {
        if (animator == null) return;

        // Don't update movement animations if dead
        if (isDead)
        {
            // We let the death animation play and don't override it
            return;
        }

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

        // Update shooting parameter
        animator.SetBool(isShootingHash, isShooting);
    }

    // Manual animation handling (alternative to Animator component)
    private void ManualAnimationUpdate()
    {
        if (selectedCharacter == null || spriteRenderer == null)
            return;

        // Determine which animation to use based on player state
        Sprite[] currentAnimation;

        if (isShooting && selectedCharacter.shootSprites != null && selectedCharacter.shootSprites.Length > 0)
        {
            currentAnimation = selectedCharacter.shootSprites;
        }
        else if (movementInput.magnitude > 0.1f && selectedCharacter.walkSprites != null && selectedCharacter.walkSprites.Length > 0)
        {
            currentAnimation = selectedCharacter.walkSprites;
        }
        else if (selectedCharacter.idleSprites != null && selectedCharacter.idleSprites.Length > 0)
        {
            currentAnimation = selectedCharacter.idleSprites;
        }
        else
        {
            // No animation sprites available, just use the default sprite
            spriteRenderer.sprite = selectedCharacter.characterSprite;
            return;
        }

        // Simple animation based on time
        // For a real game, you would want to manage frame rates properly
        int frameIndex = (int)(Time.time * 10) % currentAnimation.Length;
        spriteRenderer.sprite = currentAnimation[frameIndex];

        // Handle sprite flipping
        if (movementInput.x < 0)
            spriteRenderer.flipX = true;
        else if (movementInput.x > 0)
            spriteRenderer.flipX = false;
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

    // These health methods are no longer needed since we're using PlayerHealth
    // The HandlePlayerDeath method will be called via the PlayerHealth event system

    private IEnumerator RespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Reset player state
        isDead = false;
        canMove = true;

        // Note: Health reset should be handled by PlayerHealth
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
}

/// <summary>
/// Interface for interactable objects
/// </summary>
public interface IInteractable
{
    void Interact(PlayerController player);
}