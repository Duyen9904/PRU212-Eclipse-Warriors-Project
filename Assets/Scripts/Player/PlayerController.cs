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
    [SerializeField] private float diagonalMovementModifier = 0.7071f; // 1/sqrt(2) if you want to manually scale diagonal

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

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
    private bool canMove = true;
    private bool isShooting = false;

    // Animation parameter hashes
    private int moveXHash;
    private int moveYHash;
    private int isMovingHash;
    private int isShootingHash;
    // For remembering last input direction
    private int lastInputXHash;
    private int lastInputYHash;

    // Store last non-zero input direction
    private float lastInputX = 0f;
    private float lastInputY = -1f; // Default facing down

    // Inventory
    private List<string> inventory = new List<string>();

    // Death state
    private PlayerHealth playerHealth;
    private bool isDead = false;

    protected override void Awake()
    {
        base.Awake();

        // Get components
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();

        // If animator reference is missing, try to get it on the same object
        // (If your Animator is on a child, change to GetComponentInChildren<Animator>())
        if (animator == null)
            animator = GetComponent<Animator>();

        // Same for SpriteRenderer
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        // Cache animation parameter hashes
        if (animator != null)
        {
            moveXHash = Animator.StringToHash("WalkInputX");
            moveYHash = Animator.StringToHash("WalkInputY");
            isMovingHash = Animator.StringToHash("isWalking");
            isShootingHash = Animator.StringToHash("isShooting");
            lastInputXHash = Animator.StringToHash("LastInputX");
            lastInputYHash = Animator.StringToHash("LastInputY");
        }

        // Make player persistent across scene loads
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
        // Get input
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        // Normalize diagonal movement
        if (input.magnitude > 1) input.Normalize();

        // Move character directly
        transform.position += new Vector3(input.x, input.y, 0) * moveSpeed * Time.deltaTime;

        // Update animation
        UpdateAnimation(input);
    }


    void FixedUpdate()
    {
        if (!canMove || isDead) return;

        // Calculate movement
        float currentMoveSpeed = moveSpeed * (isSprinting ? sprintMultiplier : 1f);
        Vector2 movement = movementInput * currentMoveSpeed;

        // Apply movement via velocity
        rb.linearVelocity = movement;
    }

    private void OnDestroy()
    {
        // Unsubscribe from events when destroyed
        if (playerHealth != null)
        {
            playerHealth.OnPlayerDeath -= HandlePlayerDeath;
        }
    }

    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
        Debug.Log($"Player move speed set to: {moveSpeed}");
    }

    private void UpdateSpriteDirection(Vector2 movementDir)
    {
        // Handle left/right flipping
        if (Mathf.Abs(movementDir.x) > 0.1f)
        {
            spriteRenderer.flipX = (movementDir.x < 0);
        }

        // Set animator parameters for all directions
        animator.SetFloat("WalkInputX", movementDir.x);
        animator.SetFloat("WalkInputY", movementDir.y);
        animator.SetBool("isWalking", movementDir.magnitude > 0.1f);
    }

    // Add this new method to your existing PlayerController.cs
    public void InitializeController()
    {
        // Reset movement state
        canMove = true;
        isDead = false;

        // Make sure components are properly referenced
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        // Verify Rigidbody settings
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.simulated = true;
            Debug.Log("PlayerController initialized with Rigidbody2D: " +
                      (rb.simulated ? "Simulated" : "Not Simulated"));
        }
        else
        {
            Debug.LogError("Rigidbody2D missing on PlayerController!");
        }

        // Load character data
        LoadSelectedCharacter();

        Debug.Log("PlayerController initialized successfully");
    }

    private void HandlePlayerDeath()
    {
        isDead = true;
        canMove = false;

        // Stop movement
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        // Play death animation if animator exists
        if (animator != null)
        {
            animator.SetTrigger("Die");
        }

        // Start a coroutine to handle death sequence (respawn, game over, etc.)
        StartCoroutine(HandleDeathSequence());
    }

    private IEnumerator HandleDeathSequence()
    {
        // Wait for death animation to finish
        yield return new WaitForSeconds(2f);

        // You could add your game over or respawn logic here
        // For now, we just destroy the player
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

    private void UpdateAnimation(Vector2 movementDir)
    {
        if (animator == null) return;

        // Use your parameter names
        animator.SetFloat("WalkInputX", movementDir.x);
        animator.SetFloat("WalkInputY", movementDir.y);
        animator.SetBool("isWalking", movementDir.magnitude > 0.1f);

        // Handle sprite flipping
        if (spriteRenderer != null && Mathf.Abs(movementDir.x) > 0.1f)
        {
            spriteRenderer.flipX = (movementDir.x < 0);
        }
    }

    // Optional: If you prefer manual sprite animation instead of Animator
    private void ManualAnimationUpdate()
    {
        if (selectedCharacter == null || spriteRenderer == null)
            return;

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

        // Simple sprite index based on time
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
        // Additional shooting logic here...
    }

    private void StopShooting()
    {
        isShooting = false;
        // Additional logic to stop shooting...
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

        // Debug visualization
        Debug.DrawLine(transform.position, interactPos, Color.red, 0.5f);

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
                break; // Only interact with one object
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

    // Public method to control the shooting state externally
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
