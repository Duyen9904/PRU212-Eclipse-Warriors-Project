using UnityEngine;

/// <summary>
/// Handles spawning the player character in each level
/// </summary>
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
        PlayerController existingPlayer = FindFirstObjectByType<PlayerController>();
        if (existingPlayer != null)
        {
            // Player already exists, just position it at spawn point
            existingPlayer.transform.position = spawnPoint != null
                ? spawnPoint.position
                : transform.position; // Use this transform as fallback position

            activePlayer = existingPlayer;
            Debug.Log("Found existing player, repositioning at spawn point");

            // Reset player state and ensure components are enabled
            EnablePlayerComponents(existingPlayer.gameObject);
            existingPlayer.InitializeController();
            existingPlayer.ResetPlayer();
            return;
        }

        // No existing player found, spawn a new one
        SpawnNewPlayer();
    }

    private void SpawnNewPlayer()
    {
        // Check if player prefab is assigned
        if (playerPrefab == null)
        {
            Debug.LogError("No player prefab assigned to CharacterSpawner!");

            // Try to load the default player prefab as fallback
            playerPrefab = Resources.Load<GameObject>("Prefabs/Player/DefaultPlayer");

            if (playerPrefab == null)
            {
                Debug.LogError("Could not load fallback player prefab! Character spawning failed.");
                return;
            }
        }

        // Check if spawn point is assigned
        Vector3 spawnPosition;
        if (spawnPoint != null)
        {
            spawnPosition = spawnPoint.position;
        }
        else
        {
            spawnPosition = transform.position;
            Debug.LogWarning("No spawn point assigned to CharacterSpawner! Using spawner position instead.");
        }

        // Instantiate the player
        GameObject playerInstance = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);

        // Get PlayerController component
        activePlayer = playerInstance.GetComponent<PlayerController>();
        if (activePlayer == null)
        {
            Debug.LogError("Player prefab does not have a PlayerController component!");
            Destroy(playerInstance);
            return;
        }

        // Ensure the player's components are enabled and initialized
        EnablePlayerComponents(playerInstance);
        activePlayer.InitializeController();

        Debug.Log("Spawned new player at " + spawnPosition);
    }

    private void EnablePlayerComponents(GameObject player)
    {
        // Make sure all required components are enabled

        // Set up Rigidbody2D
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.simulated = true;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
        else
        {
            Debug.LogWarning("Player doesn't have a Rigidbody2D component!");
        }

        // Enable collider
        Collider2D collider = player.GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = true;
        }

        // Ensure PlayerController is enabled
        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.enabled = true;
        }

        // Ensure PlayerHealth is enabled
        PlayerHealth health = player.GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.enabled = true;
        }

        // Ensure Animator is enabled
        Animator anim = player.GetComponent<Animator>();
        if (anim != null)
        {
            anim.enabled = true;
        }
    }

    // Called by Inspector button or by other scripts
    public void RespawnPlayer()
    {
        if (activePlayer != null)
        {
            // Reset and reposition existing player
            activePlayer.transform.position = spawnPoint != null ? spawnPoint.position : transform.position;
            activePlayer.ResetPlayer();
        }
        else
        {
            // No active player, spawn a new one
            SpawnNewPlayer();
        }
    }

    // Get the currently active player (can be used by other scripts)
    public PlayerController GetActivePlayer()
    {
        return activePlayer;
    }
}