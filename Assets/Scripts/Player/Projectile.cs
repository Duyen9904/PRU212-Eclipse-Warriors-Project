// Projectile.cs - Handles projectile behavior for top-down game
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Settings")]
    private Vector2 direction;
    private float speed;
    private int damage;
    public float lifetime = 5f;
    public GameObject impactEffect;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        Destroy(gameObject, lifetime);
    }

    public void Initialize(Vector2 dir, float spd, int dmg)
    {
        direction = dir;
        speed = spd;
        damage = dmg;

        // Set rotation based on direction (for proper sprite orientation)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        // Apply velocity
        rb.linearVelocity = direction * speed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Don't collide with player or other projectiles
        if (other.CompareTag("Player") || other.CompareTag("Projectile"))
            return;

        // Deal damage to enemy
        if (other.CompareTag("Enemy"))
        {
            other.GetComponent<EnemyHealth>()?.TakeDamage(damage);
        }

        // Spawn impact effect
        if (impactEffect != null)
        {
            Instantiate(impactEffect, transform.position, Quaternion.identity);
        }

        // Play sound
        AudioManager.Instance.PlaySound("impact");

        // Destroy projectile
        Destroy(gameObject);
    }
}