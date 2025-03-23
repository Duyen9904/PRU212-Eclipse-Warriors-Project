using UnityEngine;
public class CharacterSpawner : MonoBehaviour
{
    [Header("Spawning")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private CharacterDatabase characterDatabase;

    [Header("Runtime Reference")]
    [SerializeField] private PlayerController activePlayer;

    private void Awake()
    {
        // Check if a Player instance already exists (from DontDestroyOnLoad)
        PlayerController existingPlayer = FindObjectOfType<PlayerController>();
        if (existingPlayer != null)
        {
            // Player already exists, just position it at spawn point
            existingPlayer.transform.position = spawnPoint != null
                ? spawnPoint.position
                : Vector3.zero;
            activePlayer = existingPlayer;
            Debug.Log("Found existing player, repositioning at spawn point");
            
            // Ensure the player's components are enabled
            EnablePlayerComponents(existingPlayer.gameObject);
            return;
        }
        // No existing player found, spawn a new one
        SpawnNewPlayer();
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
        
        // Ensure the player's components are enabled
        EnablePlayerComponents(playerInstance);
        
        Debug.Log("Spawned new player at " + spawnPosition);
    }
    
    private void EnablePlayerComponents(GameObject player)
    {
        // Make sure Rigidbody2D is not kinematic and has proper settings
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.simulated = true;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation; // Common 2D setting
        }
        
        // Ensure PlayerController is enabled
        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.enabled = true;
            pc.InitializeController(); // New method to call in PlayerController
        }
    }
}