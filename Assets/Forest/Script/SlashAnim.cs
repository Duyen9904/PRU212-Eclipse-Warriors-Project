using UnityEngine;

public class SlashAnim : MonoBehaviour
{
	private ParticleSystem ps;

	private void Awake()
	{
		ps = GetComponent<ParticleSystem>();
	}

	private void Update()
	{
		if (ps && !ps.IsAlive())
		{
			Destroy(gameObject);
		}
	}
}
