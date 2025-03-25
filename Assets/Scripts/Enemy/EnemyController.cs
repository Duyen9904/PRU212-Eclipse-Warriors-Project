using UnityEngine;
using System.Collections;

public class DirectionalEnemyController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float detectionRange = 8f;
    public float attackRange = 1.5f;

    [Header("Attack Settings")]
    public int attackDamage = 10;
    public float attackCooldown = 1.5f;
    private bool canAttack = true;

    [Header("Direction Sprites")]
    public Sprite idleUp;
    public Sprite idleDown;
    public Sprite idleLeft;
    public Sprite idleRight;

    public Sprite walkUp;
    public Sprite walkDown;
    public Sprite walkLeft;
    public Sprite walkRight;

    public Sprite attackUp;
    public Sprite attackDown;
    public Sprite attackLeft;
    public Sprite attackRight;

    [Header("References")]
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Transform player;
    private Vector2 facingDirection = Vector2.down; // Default facing down

    // State tracking
    private enum EnemyState { Idle, Moving, Attacking }
    private EnemyState currentState = EnemyState.Idle;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (player == null)
        {
            Debug.LogError("Player not found! Make sure your player has the 'Player' tag.");
        }
    }

    private void Update()
    {
        if (player == null) return;

        // Get distance to player
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // Determine state based on distance
        if (distanceToPlayer <= attackRange && canAttack)
        {
            // Attack state
            rb.linearVelocity = Vector2.zero;
            currentState = EnemyState.Attacking;
            StartCoroutine(Attack());
        }
        else if (distanceToPlayer <= detectionRange)
        {
            // Chase state
            currentState = EnemyState.Moving;
            ChasePlayer();
        }
        else
        {
            // Idle state
            rb.linearVelocity = Vector2.zero;
            currentState = EnemyState.Idle;
        }

        // Update sprite based on state and direction
        UpdateSprite();
    }

    private void ChasePlayer()
    {
        if (player == null) return;

        // Calculate direction to player
        Vector2 direction = (player.position - transform.position).normalized;

        // Set velocity
        rb.linearVelocity = direction * moveSpeed;

        // Update facing direction
        if (direction.magnitude > 0.1f)
        {
            facingDirection = direction;
        }
    }

    [Header("Effects")]
    public AttackEffectController effectController;

    private IEnumerator Attack()
    {
        if (!canAttack) yield break;

        // Set attacking state
        currentState = EnemyState.Attacking;
        canAttack = false;

        // Calculate direction to player for attack
        Vector2 attackDirection = Vector2.down;
        if (player != null)
        {
            attackDirection = (player.position - transform.position).normalized;
            facingDirection = attackDirection;
        }

        // Stop movement during attack
        rb.linearVelocity = Vector2.zero;

        // Show attack effect
        if (effectController != null)
        {
            effectController.ShowAttackEffect(attackDirection);
        }

        // Wait for attack animation duration (approximately 0.5 seconds)
        yield return new WaitForSeconds(0.5f);

        // Deal damage if player is still in range
        // (rest of the method remains the same)
    }
    private void UpdateSprite()
    {
        // Determine the primary direction (up, down, left, right)
        Direction primaryDir = GetPrimaryDirection(facingDirection);

        // Set sprite based on state and direction
        switch (currentState)
        {
            case EnemyState.Idle:
                SetIdleSprite(primaryDir);
                break;
            case EnemyState.Moving:
                SetWalkSprite(primaryDir);
                break;
            case EnemyState.Attacking:
                SetAttackSprite(primaryDir);
                break;
        }
    }

    private enum Direction { Up, Down, Left, Right }

    private Direction GetPrimaryDirection(Vector2 direction)
    {
        // Determine which direction is dominant
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            // Horizontal movement is dominant
            return direction.x > 0 ? Direction.Right : Direction.Left;
        }
        else
        {
            // Vertical movement is dominant
            return direction.y > 0 ? Direction.Up : Direction.Down;
        }
    }

    private void SetIdleSprite(Direction dir)
    {
        switch (dir)
        {
            case Direction.Up:
                spriteRenderer.sprite = idleUp;
                break;
            case Direction.Down:
                spriteRenderer.sprite = idleDown;
                break;
            case Direction.Left:
                spriteRenderer.sprite = idleLeft;
                spriteRenderer.flipX = false;
                break;
            case Direction.Right:
                // If you don't have a separate right sprite, flip the left sprite
                if (idleRight == null)
                {
                    spriteRenderer.sprite = idleLeft;
                    spriteRenderer.flipX = true;
                }
                else
                {
                    spriteRenderer.sprite = idleRight;
                    spriteRenderer.flipX = false;
                }
                break;
        }
    }

    private void SetWalkSprite(Direction dir)
    {
        switch (dir)
        {
            case Direction.Up:
                spriteRenderer.sprite = walkUp;
                break;
            case Direction.Down:
                spriteRenderer.sprite = walkDown;
                break;
            case Direction.Left:
                spriteRenderer.sprite = walkLeft;
                spriteRenderer.flipX = false;
                break;
            case Direction.Right:
                // If you don't have a separate right sprite, flip the left sprite
                if (walkRight == null)
                {
                    spriteRenderer.sprite = walkLeft;
                    spriteRenderer.flipX = true;
                }
                else
                {
                    spriteRenderer.sprite = walkRight;
                    spriteRenderer.flipX = false;
                }
                break;
        }
    }

    private void SetAttackSprite(Direction dir)
    {
        switch (dir)
        {
            case Direction.Up:
                spriteRenderer.sprite = attackUp;
                break;
            case Direction.Down:
                spriteRenderer.sprite = attackDown;
                break;
            case Direction.Left:
                spriteRenderer.sprite = attackLeft;
                spriteRenderer.flipX = false;
                break;
            case Direction.Right:
                // If you don't have a separate right sprite, flip the left sprite
                if (attackRight == null)
                {
                    spriteRenderer.sprite = attackLeft;
                    spriteRenderer.flipX = true;
                }
                else
                {
                    spriteRenderer.sprite = attackRight;
                    spriteRenderer.flipX = false;
                }
                break;
        }
    }

    // Draw gizmos to visualize attack and detection ranges
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}