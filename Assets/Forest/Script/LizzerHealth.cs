using UnityEngine;
using UnityEngine.SceneManagement;

public class LizzerHealth : MonoBehaviour
{
	[SerializeField] public int startingHealth = 3;
	[SerializeField] private GameObject deathVFXPrefab;
	[SerializeField] private float knockBackThrust = 15f;
	[SerializeField] public float score;

	public int currentHealth;
	//private Knockback knockback;
	//private Flash flash;

	private void Start()
	{
		currentHealth = startingHealth;
	}
	private void Awake()
	{
		//knockback = GetComponent<Knockback>();
		//flash = GetComponent<Flash>();
	}
	public void TakeDamage(int damage)
	{
		currentHealth -= damage;
		//knockback.GetKnockedBack(PlayerController3.Instance.transform, knockBackThrust);
		//StartCoroutine(flash.FlashRoutine());
		//StartCoroutine(CheckDetectDeathRoutine());
	}

	//private IEnumerator CheckDetectDeathRoutine()
	//{
	//	yield return new WaitForSeconds(flash.GetRestoreMatTime());
	//	DetectDeath();
	//}
	public void DetectDeath()
	{
		if (currentHealth <= 0)
		{
			Instantiate(deathVFXPrefab, transform.position, Quaternion.identity);
			//GetComponent<PickupSpawner>().DropItems();
			Destroy(gameObject);
			//ScoreManager.Instance.IncreaseScore(score);
		}
	}
}
