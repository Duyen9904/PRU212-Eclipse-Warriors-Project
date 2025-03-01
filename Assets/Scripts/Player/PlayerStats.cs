
// PlayerStats.cs - Handles player health, stamina, and damage
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerStats : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    private int currentHealth;
    public Image healthBar;
    public float invincibilityTime = 1f;
    private bool isInvincible = false;

    [Header("Stamina Settings")]
    public int maxStamina = 100;
    private int currentStamina;
    public Image staminaBar;
    public float staminaRegenRate = 10f;
    public float staminaRegenDelay = 1f;
    private float staminaRegenTimer;

    [Header("References")]
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private PlayerController playerController;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerController = GetComponent<PlayerController>();

        // Initialize health and stamina
        currentHealth = maxHealth;
        currentStamina = maxStamina;

        // Update UI
        UpdateHealthBar();
        UpdateStaminaBar();
    }

    private void Update()
    {
        // Handle stamina regeneration
        HandleStaminaRegen();
    }

    public void TakeDamage(int damage)
    {
        if (isInvincible) return;

        // Apply damage
        currentHealth = Mathf.Max(0, currentHealth - damage);

        // Update UI
        UpdateHealthBar();

        // Play hit animation
        animator.SetTrigger("Hit");

        // Play sound
        AudioManager.Instance.PlaySound("hurt");

        // Start invincibility
        StartCoroutine(InvincibilityFrames());

        // Check for death
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // Play death animation
        animator.SetTrigger("Die");

        // Play sound
        AudioManager.Instance.PlaySound("death");

        // Disable player controls
        playerController.enabled = false;
        GetComponent<PlayerCombat>().enabled = false;

        // Disable physics
        GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        GetComponent<Collider2D>().enabled = false;

        // Notify level controller
        LevelController.Instance.PlayerDied();
    }

    public bool HasEnoughStamina(int amount)
    {
        return currentStamina >= amount;
    }

    public void UseStamina(int amount)
    {
        currentStamina = Mathf.Max(0, currentStamina - amount);
        staminaRegenTimer = staminaRegenDelay;
        UpdateStaminaBar();
    }

    private void HandleStaminaRegen()
    {
        if (staminaRegenTimer > 0)
        {
            staminaRegenTimer -= Time.deltaTime;
            return;
        }

        if (currentStamina < maxStamina)
        {
            currentStamina = Mathf.Min(maxStamina, currentStamina + (int)(staminaRegenRate * Time.deltaTime));
            UpdateStaminaBar();
        }
    }

    private void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.fillAmount = (float)currentHealth / maxHealth;
        }
    }

    private void UpdateStaminaBar()
    {
        if (staminaBar != null)
        {
            staminaBar.fillAmount = (float)currentStamina / maxStamina;
        }
    }

    private IEnumerator InvincibilityFrames()
    {
        isInvincible = true;

        // Flash effect
        for (float i = 0; i < invincibilityTime; i += 0.2f)
        {
            spriteRenderer.color = new Color(1f, 1f, 1f, 0.5f);
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.1f);
        }

        isInvincible = false;
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        UpdateHealthBar();
    }
}