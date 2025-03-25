using UnityEngine;

/// <summary>
/// Specialized projectile for glacier enemies that can freeze the player
/// </summary>
public class IceProjectile : MonoBehaviour
{
    private Vector2 direction;
    private float speed;
    private int damage;
    private bool canFreeze;
    private float freezeChance;
    private float freezeDuration;
    private Rigidbody2D rb;

    [SerializeField] private GameObject impactEffect;
    [SerializeField] private float lifetime = 5f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        Destroy(gameObject, lifetime);
    }

    public void Initialize(Vector2 dir, float spd, int dmg, bool freeze, float chance, float duration)
    {
        direction = dir;
        speed = spd;
        damage = dmg;
        canFreeze = freeze;
        freezeChance = chance;
        freezeDuration = duration;

        // Set rotation to match direction
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        // Set velocity
        if (rb != null)
        {
            rb.linearVelocity = direction * speed;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Apply damage to player
            PlayerStats playerStats = other.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                playerStats.TakeDamage(damage);

                // Chance to freeze player
                if (canFreeze && Random.value < freezeChance)
                {
                    // Apply freeze effect
                    PlayerController playerController = other.GetComponent<PlayerController>();
                    if (playerController != null)
                    {
                        // Apply slow effect through a debuff system or directly
                        // playerController.ApplySpeedModifier(0.5f, freezeDuration);
                    }

                    // Spawn ice effect on player
                    Transform iceEffectParent = GameObject.FindGameObjectWithTag("EffectsParent")?.transform ?? null;
                    GameObject iceEffect = Resources.Load<GameObject>("Effects/IceEffect");
                    if (iceEffect != null)
                    {
                        Instantiate(iceEffect, other.transform.position, Quaternion.identity, iceEffectParent);
                    }
                }
            }

            // Spawn impact effect
            if (impactEffect != null)
            {
                Instantiate(impactEffect, transform.position, Quaternion.identity);
            }

            // Destroy the projectile
            Destroy(gameObject);
        }
        else if (!other.CompareTag("Enemy"))
        {
            // Hit something other than an enemy or player
            if (impactEffect != null)
            {
                Instantiate(impactEffect, transform.position, Quaternion.identity);
            }

            // Destroy the projectile
            Destroy(gameObject);
        }
    }
}