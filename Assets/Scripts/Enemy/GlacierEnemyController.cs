using UnityEngine;
using System.Collections;

/// <summary>
/// Specialized enemy controller for Glacier biome enemies with ice-based abilities
/// </summary>
public class GlacierEnemyController : BaseEnemyController
{
    [Header("Glacier-Specific Settings")]
    [SerializeField] private bool usesIceProjectiles = false;
    [SerializeField] private GameObject iceProjectilePrefab;
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private float projectileSpeed = 8f;
    [SerializeField] private float projectileCooldown = 2f;
    [SerializeField] private bool canFreezePlayer = false;
    [SerializeField] private float freezeChance = 0.3f;
    [SerializeField] private float freezeDuration = 2f;
    [SerializeField] private GameObject iceEffectPrefab;
    [SerializeField] private float slowAmount = 0.5f; // How much to slow player (0.5 = 50% slower)

    private bool canShootProjectile = true;

    protected override void Awake()
    {
        base.Awake();

        // If no projectile spawn point set, use the transform position
        if (projectileSpawnPoint == null)
        {
            projectileSpawnPoint = transform;
        }
    }

    protected override void Update()
    {
        base.Update();

        // Add additional glacier-specific behavior here
    }

    protected override IEnumerator PerformAttack()
    {
        canAttack = false;
        isShooting = true;

        // Trigger attack animation
        animator.SetBool("isShooting", true);

        if (usesIceProjectiles && canShootProjectile)
        {
            // Launch ice projectile
            StartCoroutine(ShootIceProjectile());
            Debug.Log("Enemy entered attack state Start Coroutine");
        }
        else
        {
            // Melee attack with potential freeze effect
            yield return new WaitForSeconds(0.3f); // Wait for animation

            // Check if player is still in range
            if (target != null)
            {
                float distance = Vector2.Distance(transform.position, target.position);
                Debug.Log("Distance to player: " + distance);
                if (distance <= attackRange)
                {
                    // Apply damage
                    PlayerStats playerStats = target.GetComponent<PlayerStats>();
                    if (playerStats != null)
                    {
                        playerStats.TakeDamage(attackDamage);
                        Debug.Log("Player took damage");

                        // Chance to freeze player
                        if (canFreezePlayer && Random.value < freezeChance)
                        {
                            ApplyFreezeEffect();
                            Debug.Log("Player was frozen");
                        }
                    }
                }
            }
        }

        // Reset shooting animation
        yield return new WaitForSeconds(0.3f);
        isShooting = false;
        animator.SetBool("isShooting", false);

        // Wait for cooldown
        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }

    private IEnumerator ShootIceProjectile()
    {
        canShootProjectile = false;

        if (iceProjectilePrefab != null && target != null)
        {
            // Calculate direction to target
            Vector2 direction = (target.position - projectileSpawnPoint.position).normalized;

            // Spawn projectile
            GameObject projectile = Instantiate(iceProjectilePrefab, projectileSpawnPoint.position, Quaternion.identity);

            // Set up projectile
            IceProjectile iceProjectile = projectile.GetComponent<IceProjectile>();
            if (iceProjectile != null)
            {
                iceProjectile.Initialize(direction, projectileSpeed, attackDamage, canFreezePlayer, freezeChance, freezeDuration);
            }
            else
            {
                // If not using the custom IceProjectile component, try to use the generic Projectile component
                Projectile basicProjectile = projectile.GetComponent<Projectile>();
                if (basicProjectile != null)
                {
                    basicProjectile.Initialize(direction, projectileSpeed, attackDamage);
                }
                else
                {
                    // As a last resort, just add force to the projectile
                    Rigidbody2D projectileRb = projectile.GetComponent<Rigidbody2D>();
                    if (projectileRb != null)
                    {
                        projectileRb.linearVelocity = direction * projectileSpeed;
                    }
                }
            }

            // Play sound effect
            AudioManager audioManager = AudioManager.Instance;
            if (audioManager != null)
            {
                audioManager.PlaySound("ice_shoot");
            }
        }

        // Wait for projectile cooldown
        yield return new WaitForSeconds(projectileCooldown);
        canShootProjectile = true;
    }

    private void ApplyFreezeEffect()
    {
        if (target == null) return;

        // Apply visual freeze effect
        if (iceEffectPrefab != null)
        {
            Instantiate(iceEffectPrefab, target.position, Quaternion.identity, target);
            Debug.Log("Applied freeze effect to player");
        }

        // Slow down player
        PlayerController playerController = target.GetComponent<PlayerController>();
        if (playerController != null)
        {
            // Apply slow effect through a debuff system or directly
            // This depends on how your PlayerController is set up
            // For example:
            //playerController.ApplySpeedModifier(slowAmount, freezeDuration);
        }

        // Play freeze sound
        AudioManager audioManager = AudioManager.Instance;
        if (audioManager != null)
        {
            audioManager.PlaySound("freeze");
        }
    }

    // Additional glacier-specific methods below
}