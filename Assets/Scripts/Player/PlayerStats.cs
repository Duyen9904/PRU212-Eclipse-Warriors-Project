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

    [Header("Mana Settings")]
    public int maxMana = 100;
    private int currentMana;
    public Image manaBar;
    public float manaRegenRate = 5f;

    [Header("References")]
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private PlayerController playerController;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerController = GetComponent<PlayerController>();

        // Initialize health, stamina and mana
        currentHealth = maxHealth;
        currentStamina = maxStamina;
        currentMana = maxMana;

        // Update UI
        UpdateHealthBar();
        UpdateStaminaBar();
        UpdateManaBar();
    }

    private void Update()
    {
        // Handle stamina regeneration
        HandleStaminaRegen();
        // Handle mana regeneration
        HandleManaRegen();
    }

    // Get current health - used by UnifiedLevelManager.SavePlayerState()
    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    // Get current mana - used by UnifiedLevelManager.SavePlayerState()
    public int GetCurrentMana()
    {
        return currentMana;
    }

    // Set health - used by UnifiedLevelManager.ApplyPlayerData()
    public void SetHealth(int health)
    {
        currentHealth = Mathf.Clamp(health, 0, maxHealth);
        UpdateHealthBar();
    }

    // Set mana - used by UnifiedLevelManager.ApplyPlayerData()
    public void SetMana(int mana)
    {
        currentMana = Mathf.Clamp(mana, 0, maxMana);
        UpdateManaBar();
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
        if (AudioManager.Instance != null)
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
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySound("death");

        // Disable player controls
        playerController.enabled = false;

        // Disable physics
        GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        GetComponent<Collider2D>().enabled = false;

        // Notify level manager
        if (LevelManager.Instance != null)
        {
            // Wait a moment before showing game over
            StartCoroutine(DelayedGameOver());
        }
    }

    private IEnumerator DelayedGameOver()
    {
        yield return new WaitForSeconds(2f);
        LevelManager.Instance.HandleTriggerEvent("PlayerDied");
    }

    public bool HasEnoughStamina(int amount)
    {
        return currentStamina >= amount;
    }

    public bool HasEnoughMana(int amount)
    {
        return currentMana >= amount;
    }

    public void UseStamina(int amount)
    {
        currentStamina = Mathf.Max(0, currentStamina - amount);
        staminaRegenTimer = staminaRegenDelay;
        UpdateStaminaBar();
    }

    public void UseMana(int amount)
    {
        currentMana = Mathf.Max(0, currentMana - amount);
        UpdateManaBar();
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

    private void HandleManaRegen()
    {
        if (currentMana < maxMana)
        {
            currentMana = Mathf.Min(maxMana, currentMana + (int)(manaRegenRate * Time.deltaTime));
            UpdateManaBar();
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

    private void UpdateManaBar()
    {
        if (manaBar != null)
        {
            manaBar.fillAmount = (float)currentMana / maxMana;
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

    public void RestoreMana(int amount)
    {
        currentMana = Mathf.Min(maxMana, currentMana + amount);
        UpdateManaBar();
    }
}