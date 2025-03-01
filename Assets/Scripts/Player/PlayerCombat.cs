// PlayerCombat.cs - Handles player attacks, projectiles, and combat for top-down game
using System.Collections;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("Melee Attack Settings")]
    public float attackRange = 1.2f;
    public float attackAngle = 90f; // Attack arc in degrees
    public int attackDamage = 20;
    public float attackCooldown = 0.5f;
    public LayerMask enemyLayers;

    [Header("Projectile Settings")]
    public GameObject projectilePrefab;
    public float projectileOffset = 0.5f; // Spawn distance from player
    public float projectileSpeed = 15f;
    public int projectileDamage = 10;
    public float projectileCooldown = 0.7f;

    [Header("References")]
    private Animator animator;
    private PlayerController playerController;
    private PlayerStats playerStats;

    [Header("Cooldowns")]
    private bool canAttack = true;
    private bool canShoot = true;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        playerController = GetComponent<PlayerController>();
        playerStats = GetComponent<PlayerStats>();
    }

    private void Update()
    {
        // Melee attack
        if (Input.GetMouseButtonDown(0) && canAttack && playerStats.HasEnoughStamina(10))
        {
            MeleeAttack();
        }

        // Projectile attack
        if (Input.GetMouseButtonDown(1) && canShoot && playerStats.HasEnoughStamina(15))
        {
            ShootProjectile();
        }
    }

    private void MeleeAttack()
    {
        // Use stamina
        playerStats.UseStamina(10);

        // Get attack direction (use mouse direction)
        Vector2 attackDirection = playerController.GetDirectionToMouse();

        // Set attack trigger with direction
        animator.SetTrigger("Attack");
        animator.SetFloat("AttackX", attackDirection.x);
        animator.SetFloat("AttackY", attackDirection.y);

        // Start cooldown
        StartCoroutine(AttackCooldown());

        // Find all enemies in range
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, attackRange, enemyLayers);

        // Check which enemies are within attack arc
        foreach (Collider2D enemy in hitEnemies)
        {
            Vector2 directionToEnemy = (enemy.transform.position - transform.position).normalized;
            float angleToEnemy = Vector2.Angle(attackDirection, directionToEnemy);

            // Only damage enemies within attack arc
            if (angleToEnemy <= attackAngle / 2)
            {
                enemy.GetComponent<EnemyHealth>()?.TakeDamage(attackDamage);

                // Optional: Add knockback
                Rigidbody2D enemyRb = enemy.GetComponent<Rigidbody2D>();
                if (enemyRb != null)
                {
                    enemyRb.AddForce(directionToEnemy * 5f, ForceMode2D.Impulse);
                }
            }
        }

        // Play sound
        AudioManager.Instance.PlaySound("slash");
    }

    private void ShootProjectile()
    {
        // Use stamina
        playerStats.UseStamina(15);

        // Get shoot direction (use mouse direction)
        Vector2 shootDirection = playerController.GetDirectionToMouse();

        // Set shoot trigger with direction
        animator.SetTrigger("Shoot");
        animator.SetFloat("ShootX", shootDirection.x);
        animator.SetFloat("ShootY", shootDirection.y);

        // Start cooldown
        StartCoroutine(ShootCooldown());

        // Calculate spawn position (offset from player in shoot direction)
        Vector2 spawnPosition = (Vector2)transform.position + shootDirection * projectileOffset;

        // Instantiate projectile
        GameObject projectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
        Projectile projectileScript = projectile.GetComponent<Projectile>();

        // Set projectile direction and damage
        if (projectileScript != null)
        {
            projectileScript.Initialize(shootDirection, projectileSpeed, projectileDamage);
        }

        // Play sound
        AudioManager.Instance.PlaySound("shoot");
    }

    private IEnumerator AttackCooldown()
    {
        canAttack = false;
        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }

    private IEnumerator ShootCooldown()
    {
        canShoot = false;
        yield return new WaitForSeconds(projectileCooldown);
        canShoot = true;
    }

    // Helper method to visualize attack range in editor
    private void OnDrawGizmosSelected()
    {
        // Draw attack range circle
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Draw attack arc if we have a direction
        if (Application.isPlaying && playerController != null)
        {
            Vector2 direction = playerController.GetDirectionToMouse();

            // Calculate arc points
            float halfAngle = attackAngle / 2;
            int segments = 10;

            Vector3 prevPoint = transform.position;
            for (int i = 0; i <= segments; i++)
            {
                float angle = -halfAngle + (attackAngle * i / segments);
                Vector2 rotatedDir = RotateVector(direction, angle);
                Vector3 point = transform.position + (Vector3)(rotatedDir * attackRange);

                // Draw segment
                Gizmos.DrawLine(prevPoint, point);
                prevPoint = point;
            }

            // Draw line back to center
            Gizmos.DrawLine(prevPoint, transform.position);
        }
    }

    // Helper to rotate vector by angle in degrees
    private Vector2 RotateVector(Vector2 v, float angle)
    {
        float radian = angle * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radian);
        float cos = Mathf.Cos(radian);
        return new Vector2(
            v.x * cos - v.y * sin,
            v.x * sin + v.y * cos
        );
    }
}