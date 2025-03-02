// EnemyHealth.cs - Basic enemy health system
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    private int currentHealth;

    [Header("Visual Feedback")]
    public GameObject deathEffect;
    public float hitFlashDuration = 0.1f;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    [Header("Drops")]
    public GameObject[] possibleDrops;
    [Range(0f, 1f)]
    public float dropChance = 0.3f;

    private Animator animator;

    private void Awake()
    {
        this.currentHealth = this.maxHealth;
        this.animator = GetComponent<Animator>();
        this.spriteRenderer = GetComponent<SpriteRenderer>();

        if (this.spriteRenderer != null)
        {
            this.originalColor = this.spriteRenderer.color;
        }
    }

    public void TakeDamage(int damage)
    {
        this.currentHealth = Mathf.Max(0, this.currentHealth - damage);

        // Visual feedback
        if (this.animator != null)
        {
            this.animator.SetTrigger("Hit");
        }

        // Flash effect
        if (this.spriteRenderer != null)
        {
            StartCoroutine(FlashRoutine());
        }

        // Check for death
        if (this.currentHealth <= 0)
        {
            Die();
        }
    }

    private System.Collections.IEnumerator FlashRoutine()
    {
        this.spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(this.hitFlashDuration);
        this.spriteRenderer.color = this.originalColor;
    }

    private void Die()
    {
        // Play death animation if available
        if (this.animator != null && this.animator.HasState(0, Animator.StringToHash("Death")))
        {
            this.animator.SetTrigger("Death");

            // Disable components
            GetComponent<Collider2D>().enabled = false;

            // If enemy has AI, disable it
            Behaviour[] components = GetComponents<Behaviour>();
            foreach (Behaviour component in components)
            {
                if (component != this && component != this.animator)
                {
                    component.enabled = false;
                }
            }

            // Destroy after animation
            Destroy(gameObject, this.animator.GetCurrentAnimatorStateInfo(0).length);
        }
        else
        {
            // Instant death with effect
            if (this.deathEffect != null)
            {
                Instantiate(this.deathEffect, transform.position, Quaternion.identity);
            }

            // Handle drops
            SpawnDrops();

            // Destroy immediately
            Destroy(gameObject);
        }
    }

    private void SpawnDrops()
    {
        if (this.possibleDrops.Length > 0 && Random.value <= this.dropChance)
        {
            int randomIndex = Random.Range(0, this.possibleDrops.Length);
            Instantiate(this.possibleDrops[randomIndex], transform.position, Quaternion.identity);
        }
    }
}
