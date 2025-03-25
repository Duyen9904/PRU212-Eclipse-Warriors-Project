using System.Collections.Generic;
using UnityEngine;

public class BulletPool : MonoBehaviour
{
	public static BulletPool Instance;
	public GameObject bulletPrefab;
	public int poolSize = 10;

	private Queue<GameObject> bulletQueue = new Queue<GameObject>();

	private void Awake()
	{
		Instance = this;
		for (int i = 0; i < poolSize; i++)
		{
			GameObject bullet = Instantiate(bulletPrefab);
			bullet.SetActive(false);
			bulletQueue.Enqueue(bullet);
		}
	}

	public GameObject GetBullet()
	{
		Debug.Log("GetBullet");
		if (bulletQueue.Count > 0)
		{
			GameObject bullet = bulletQueue.Dequeue();
			bullet.SetActive(true);


			BulletImpact bulletScript = bullet.GetComponent<BulletImpact>();

			return bullet;
		}
		else
		{
			GameObject bullet = Instantiate(bulletPrefab);
			return bullet;
		}
	}

	public void ReturnBullet(GameObject bullet)
	{
		bullet.SetActive(false);
		bulletQueue.Enqueue(bullet);
	}
}
