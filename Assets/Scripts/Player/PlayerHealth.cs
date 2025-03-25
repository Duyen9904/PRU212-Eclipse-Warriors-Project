using UnityEngine;

public class PlayerHealth : Singleton<PlayerHealth>
{
    [SerializeField] private int maxHealth = 30;
    public bool isDead { get; private set; }

    private int currentHealth;
    private bool canTakeDamage = true;
    private float invincibilityTime = 0.5f; // Time after taking damage when player can't take more damage

    // Event that other scripts can subscribe to
    public delegate void PlayerDeathHandler();
    public event PlayerDeathHandler OnPlayerDeath;

    // Event for damage taken
    public delegate void PlayerDamageHandler(int currentHealth, int maxHealth);
    public event PlayerDamageHandler OnPlayerDamage;

    private Flash flash;


    protected override void Awake()
    {
        base.Awake();
        flash = GetComponent<Flash>();
    }

    private void Start()
    {
        
        currentHealth = maxHealth;
        Debug.Log("Player health: " + currentHealth);
        isDead = false;
    }

    public void TakeDamage(int damageAmount, Transform hitTransform = null)
    {
        if (!canTakeDamage || isDead) { return; }

        // Apply damage
        currentHealth -= damageAmount;
        Debug.Log("Player took " + damageAmount + " damage. Current health: " + currentHealth);
        StartCoroutine(flash.FlashRoutine());
        // Notify subscribers about damage
        OnPlayerDamage?.Invoke(currentHealth, maxHealth);

        // Add temporary invincibility
        canTakeDamage = false;
        Invoke(nameof(ResetDamageProtection), invincibilityTime);

        // Check for death
        CheckIfPlayerDeath();
    }

    private void ResetDamageProtection()
    {
        canTakeDamage = true;
    }

    private void CheckIfPlayerDeath()
    {
        if (currentHealth <= 0 && !isDead)
        {
            isDead = true;
            currentHealth = 0;

            // Notify other components about death
            OnPlayerDeath?.Invoke();

            // Don't destroy immediately - let the PlayerController handle the death animation
            // The PlayerController can destroy or respawn the player after the animation
        }
    }

    public void Heal(int amount)
    {
        if (isDead) return;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        OnPlayerDamage?.Invoke(currentHealth, maxHealth);
    }

    public float GetHealthPercentage()
    {
        return (float)currentHealth / maxHealth;
    }

    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    // Add to PlayerHealth class
    public void SetMaxHealth(int newMaxHealth)
    {
        maxHealth = newMaxHealth;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        OnPlayerDamage?.Invoke(currentHealth, maxHealth);
    }
}