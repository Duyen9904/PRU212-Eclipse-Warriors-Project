using UnityEngine;
public class CharacterSpawner : MonoBehaviour
{
    [Header("Spawning")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private CharacterDatabase characterDatabase;

    [Header("Runtime Reference")]
    [SerializeField] private PlayerController activePlayer;

    // In CharacterSpawner.cs
    private void Awake()
    {
        PlayerController existingPlayer = FindObjectOfType<PlayerController>();
        if (existingPlayer != null)
        {
            // Position existing player and use it
            existingPlayer.transform.position = spawnPoint != null
                ? spawnPoint.position
                : Vector3.zero;
            activePlayer = existingPlayer;
            Debug.Log("Using existing player at " + existingPlayer.transform.position);
        }
        else
        {
            SpawnNewPlayer();
        }
    }

    private void SpawnNewPlayer()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("No player prefab assigned to CharacterSpawner!");
            return;
        }
        // Spawn player at spawn point or default position
        Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        GameObject playerInstance = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
        // Get PlayerController component
        activePlayer = playerInstance.GetComponent<PlayerController>();
        if (activePlayer == null)
        {
            Debug.LogError("Player prefab does not have a PlayerController component!");
            return;
        }
        Debug.Log("Spawned new player at " + spawnPosition);
    }
}