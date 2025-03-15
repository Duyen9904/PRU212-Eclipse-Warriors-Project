using System.Collections;
using UnityEngine;

public class LizzerAI : MonoBehaviour
{
	// Start is called once before the first execution of Update after the MonoBehaviour is created
	[SerializeField] private float roamChangeDirFloat = 2f;

	private enum State
	{
		Roaming,
	}

	private State state;
	private PathFinding enemyPathFinding;

	private void Awake()
	{
		enemyPathFinding = GetComponent<PathFinding>();
		state = State.Roaming;
	}

	private void Start()
	{
		StartCoroutine(RoamingRoutine());
	}

	private IEnumerator RoamingRoutine()
	{
		while (state == State.Roaming)
		{
			Vector2 roamPosition = GetRoamingPosition();
			enemyPathFinding.MoveTo(roamPosition);
			yield return new WaitForSeconds(roamChangeDirFloat);
		}
	}

	private Vector2 GetRoamingPosition()
	{
		return new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
	}
}
