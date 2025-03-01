// PlayerController.cs - Main script for top-down player movement
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float dashSpeed = 10f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;

    [Header("References")]
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private PlayerStats playerStats;

    [Header("State Tracking")]
    private Vector2 moveDirection;
    private Vector2 lastMoveDirection;
    private bool isDashing;
    private bool canDash = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerStats = GetComponent<PlayerStats>();
    }

    private void Update()
    {
        // Get input for movement
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        // Calculate movement direction
        moveDirection = new Vector2(moveX, moveY).normalized;

        // Store last non-zero direction for attacks and dashing
        if (moveDirection.magnitude > 0.1f)
        {
            lastMoveDirection = moveDirection;
        }

        // Handle dash input
        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash && !isDashing && playerStats.HasEnoughStamina(20))
        {
            StartCoroutine(Dash());
        }

        // Update animator
        UpdateAnimator();

        // Handle sprite flipping (only for horizontal movement)
        if (moveX > 0)
        {
            spriteRenderer.flipX = false;
        }
        else if (moveX < 0)
        {
            spriteRenderer.flipX = true;
        }
    }

    private void FixedUpdate()
    {
        // Apply movement
        if (!isDashing)
        {
            rb.linearVelocity = moveDirection * moveSpeed;
        }
    }

    private System.Collections.IEnumerator Dash()
    {
        // Use stamina
        playerStats.UseStamina(20);

        // Set dash state
        isDashing = true;
        canDash = false;

        // Play dash animation
        animator.SetTrigger("Dash");

        // Play sound
        AudioManager.Instance.PlaySound("dash");

        // Apply dash velocity
        rb.linearVelocity = lastMoveDirection * dashSpeed;

        // Wait for dash duration
        yield return new WaitForSeconds(dashDuration);

        // End dash
        isDashing = false;

        // Cooldown
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    private void UpdateAnimator()
    {
        // Set movement parameters
        animator.SetFloat("MoveX", moveDirection.x);
        animator.SetFloat("MoveY", moveDirection.y);
        animator.SetFloat("MoveMagnitude", moveDirection.magnitude);

        // Set last direction parameters (for idle state)
        animator.SetFloat("LastMoveX", lastMoveDirection.x);
        animator.SetFloat("LastMoveY", lastMoveDirection.y);
    }

    public Vector2 GetFacingDirection()
    {
        return lastMoveDirection;
    }

    // Get a normalized direction to the mouse cursor
    public Vector2 GetDirectionToMouse()
    {
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = (mousePosition - transform.position).normalized;
        return direction;
    }
}
