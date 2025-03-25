using Unity.VisualScripting;
using UnityEngine;

public class PlayerHealth : Singleton<PlayerHealth>
{
	[SerializeField] private int maxHealth = 30;

	public bool isDead { get; private set; }
	private int currentHealth;
	private bool canTakeDamage = true;

	private void Start()
	{
		currentHealth = maxHealth;
		isDead = false;
	}

	public void TakeDamage(int damageAmount, Transform hitTransform)
	{
		if (!canTakeDamage) { return; }
		canTakeDamage = false;
		currentHealth -= damageAmount;
		CheckIfPlayerDeath();
	}

	private void CheckIfPlayerDeath()
	{
		if (currentHealth <= 0 && !isDead)
		{
			isDead = true;
			currentHealth = 0;
			Destroy(gameObject);
		}
	}
}
