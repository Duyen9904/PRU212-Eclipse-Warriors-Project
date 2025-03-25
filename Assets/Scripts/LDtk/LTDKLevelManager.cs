using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
// Cinemachine namespace is needed if we reference Cinemachine classes
using Unity.Cinemachine;
using LDtkUnity;
/// <summary>
/// LDtkLevelManager: Manages level transitions, saving/loading, and Cinemachine camera for a top-down game.
/// </summary>
public class LDtkLevelManager : MonoBehaviour
{
    public static LDtkLevelManager Instance;

    [Header("Level Management")]
    [SerializeField] private string[] levelSceneNames;
    // Names of Unity scenes for each level (if using separate scenes). 
    // If using an LDtk world in one scene, this could be LDtk level identifiers instead.

    [SerializeField] private int startingLevelIndex = 0;
    private int currentLevelIndex = 0;

    [Header("Player & Camera")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private CinemachineVirtualCamera cineCamera;
    [SerializeField] private CinemachineConfiner2D cameraConfiner;

    // Player persistent data (health, inventory, etc.) to carry across levels
    public PlayerData playerData = new PlayerData();

    // Example: mapping LDtk entity names to prefabs for spawning
    [Header("Prefabs")]
    public GameObject[] enemyPrefabs;
    // You could use a Dictionary<string, GameObject> for mapping entity IDs to prefabs for flexibility.

    [Header("Level Boundaries")]
    [SerializeField] private Vector2 levelBoundsSize = new Vector2(100f, 100f); // Default size for fallback boundaries

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);  // persist across scene loads
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Initialize current level
        currentLevelIndex = startingLevelIndex;

        // Hook into scene load event to reconfigure each new level
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        // Find player if not assigned (e.g., if player is part of the scene)
        if (playerController == null)
            playerController = FindObjectOfType<PlayerController>();

        // Setup Cinemachine follow target
        if (cineCamera != null && playerController != null)
        {
            cineCamera.Follow = playerController.transform;
        }

        // Load the initial level (unless the starting level is already in the scene)
        LoadLevel(currentLevelIndex);
    }

    /// <summary>
    /// Loads a level by index (transition to new level).
    /// </summary>
    public void LoadLevel(int index)
    {
        // Save current player state before leaving (if a level is already active)
        if (playerController != null)
        {
            SavePlayerState();
        }

        // Bounds check
        if (index < 0 || index >= levelSceneNames.Length)
        {
            Debug.LogWarning("Level index out of range!");
            return;
        }
        currentLevelIndex = index;

        // Load the specified level scene
        string sceneName = levelSceneNames[index];
        SceneManager.LoadScene(sceneName);
        // Note: The OnSceneLoaded event will trigger after this, handling post-load setup.
    }

    /// <summary>
    /// Callback after a new scene is loaded. Re-wire player and camera to the new level.
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Find the new scene's player object and set up references
        playerController = FindObjectOfType<PlayerController>();
        if (playerController == null)
        {
            Debug.LogError("Player not found in the loaded scene.");
            return;
        }

        // Apply persistent player data (health, inventory, etc.) to the new player instance
        ApplyPlayerDataToPlayer();

        // Reassign the Cinemachine follow target to the new player
        if (cineCamera != null)
        {
            cineCamera.Follow = playerController.transform;
        }

        // Configure camera confiner to the new level's bounds (assuming there's a PolygonCollider2D named "LevelBounds" in the scene)
        if (cameraConfiner != null)
        {
            GameObject boundsObj = GameObject.Find("LevelBounds");
            if (boundsObj != null)
            {
                PolygonCollider2D bounds = boundsObj.GetComponent<PolygonCollider2D>();
                cameraConfiner.BoundingShape2D = bounds;
                cameraConfiner.InvalidateCache();  // Recalculate confiner with new shape
            }
        }
        CreateLevelBoundaries();

        // Spawn enemies/objects for this level if not already present (for dynamic loading scenarios)
        SpawnLevelEntities();

        // (Optional) Trigger any "level start" events, like playing music or showing level title
        Debug.Log($"Level {currentLevelIndex + 1} loaded.");
    }

    /// <summary>
    /// Spawns enemies and interactive objects for the current level based on LDtk data.
    /// If using the LDtk importer with automatic entity instantiation, this may not be needed.
    /// </summary>
    private void SpawnLevelEntities()
    {
        // This method would use LDtk data to instantiate prefabs. 
        // For example, you might have a script or component that holds references to LDtk-placed entities.
        // Pseudo-code example (actual implementation depends on LDtk Unity integration):
        /*
        LDtkLevelData levelData = ... // get from LDtk
        foreach (var entity in levelData.entities) {
            if (entity.name == "EnemyGoblin") {
                Instantiate(enemyGoblinPrefab, entity.position, Quaternion.identity);
            }
            else if (entity.name == "Chest") {
                Instantiate(chestPrefab, entity.position, Quaternion.identity);
            }
            // ... handle other entity types
        }
        */
    }

    /// <summary>
    /// Save the player's current state (health, inventory, position, etc.) into playerData.
    /// </summary>
    private void SavePlayerState()
    {
        if (playerController == null) return;
        // Example: save health and inventory
        PlayerStats stats = playerController.GetComponent<PlayerStats>();
        if (stats != null)
        {
            //playerData.health = stats.GetCurrentHealth();
        }
        //playerData.inventory = playerController.GetInventoryItems();
        // (Add other data as needed, e.g., position if you want to save checkpoints within a level)
    }

    /// <summary>
    /// Apply the saved player data to the current player (after loading a new level).
    /// </summary>
    private void ApplyPlayerDataToPlayer()
    {
        if (playerController == null) return;
        PlayerStats stats = playerController.GetComponent<PlayerStats>();
        //if (stats != null)
        //{
        //    stats.SetCurrentHealth(playerData.health);
        //}
        ////playerController.SetInventoryItems(playerData.inventory);
        // You might also set player position if continuing in the same level at a saved location.
    }

    /// <summary>
    /// Handle a trigger event by ID (called from trigger colliders in the level).
    /// </summary>
    public void HandleTriggerEvent(string triggerID)
    {
        Debug.Log("Trigger event: " + triggerID);
        switch (triggerID)
        {
            case "LevelEnd":
                // Reached end of level, proceed to next level
                SavePlayerState();
                // (Optional: show a "Level Complete" UI or fade-out here)
                LoadNextLevel();
                break;
            case "LevelStart":
                // Example: could be used to initiate something at level start
                break;
            case "SecretArea":
                // Example: player found a secret area, trigger some event
                // e.g., grant bonus or play a secret reveal animation
                break;
                // Add cases for any custom triggers defined in LDtk
        }
    }

    public void LoadNextLevel()
    {
        int nextIndex = currentLevelIndex + 1;
        if (nextIndex < levelSceneNames.Length)
        {
            LoadLevel(nextIndex);
        }
        else
        {
            Debug.Log("All levels completed!");
            // Here you could load a victory screen or loop back to main menu.
        }
    }

    /// <summary>
    /// Save game progress (current level and player data) to persistent storage.
    /// </summary>
    public void SaveProgress()
    {
        // Save the current level index
        PlayerPrefs.SetInt("CurrentLevel", currentLevelIndex);
        // Save player stats (for example, health and inventory)
        PlayerPrefs.SetInt("PlayerHealth", playerData.health);
        PlayerPrefs.SetString("PlayerInventory", string.Join(",", playerData.inventory));
        PlayerPrefs.Save();  // write to disk

        Debug.Log("Progress saved.");
    }

    // In your LDtkLevelManager.cs
    public void CreateLevelBoundaries()
    {
        // Create empty GameObject for boundaries
        GameObject boundaries = new GameObject("LevelBoundaries");
        boundaries.tag = "LevelBounds";

        // Get level dimensions - find the LDtkComponentLevel first
        var ldtkLevel = FindObjectOfType<LDtkUnity.LDtkComponentLevel>();

        // If LDtk level exists, use its dimensions
        if (ldtkLevel != null)
        {
            var levelBounds = ldtkLevel.GetComponent<Renderer>()?.bounds;
            if (levelBounds.HasValue)
            {
                float levelWidth = levelBounds.Value.size.x;
                float levelHeight = levelBounds.Value.size.y;
                Vector3 center = levelBounds.Value.center;

                // Create colliders around level perimeter
                CreateBoundaryCollider(boundaries,
                    new Vector2(center.x, center.y - levelHeight / 2 - 1),
                    new Vector2(levelWidth, 2));
                CreateBoundaryCollider(boundaries,
                    new Vector2(center.x, center.y + levelHeight / 2 + 1),
                    new Vector2(levelWidth, 2));
                CreateBoundaryCollider(boundaries,
                    new Vector2(center.x - levelWidth / 2 - 1, center.y),
                    new Vector2(2, levelHeight));
                CreateBoundaryCollider(boundaries,
                    new Vector2(center.x + levelWidth / 2 + 1, center.y),
                    new Vector2(2, levelHeight));

                // Create camera bounds collider
                BoxCollider2D cameraBounds = boundaries.AddComponent<BoxCollider2D>();
                cameraBounds.isTrigger = true;
                cameraBounds.size = new Vector2(levelWidth, levelHeight);
                cameraBounds.offset = center - boundaries.transform.position;
            }
        }
        else
        {
            // Fallback to predefined size
            CreateBoundaryCollider(boundaries, new Vector2(0, -levelBoundsSize.y / 2 - 1), new Vector2(levelBoundsSize.x, 2));
            CreateBoundaryCollider(boundaries, new Vector2(0, levelBoundsSize.y / 2 + 1), new Vector2(levelBoundsSize.x, 2));
            CreateBoundaryCollider(boundaries, new Vector2(-levelBoundsSize.x / 2 - 1, 0), new Vector2(2, levelBoundsSize.y));
            CreateBoundaryCollider(boundaries, new Vector2(levelBoundsSize.x / 2 + 1, 0), new Vector2(2, levelBoundsSize.y));

            // Add camera bounds
            BoxCollider2D cameraBounds = boundaries.AddComponent<BoxCollider2D>();
            cameraBounds.isTrigger = true;
            cameraBounds.size = levelBoundsSize;
        }

        Debug.Log("Level boundaries created successfully");
    }

    private void CreateBoundaryCollider(GameObject parent, Vector2 position, Vector2 size)
    {
        GameObject colliderObj = new GameObject("BoundaryCollider");
        colliderObj.transform.parent = parent.transform;
        colliderObj.transform.position = position;

        BoxCollider2D collider = colliderObj.AddComponent<BoxCollider2D>();
        collider.size = size;
        collider.isTrigger = false;
    }
    /// <summary>
    /// Load game progress from persistent storage.
    /// </summary>
    public void LoadProgress()
    {
        if (PlayerPrefs.HasKey("CurrentLevel"))
        {
            currentLevelIndex = PlayerPrefs.GetInt("CurrentLevel");
        }
        else
        {
            currentLevelIndex = startingLevelIndex;
        }
        // Load player stats if available
        if (PlayerPrefs.HasKey("PlayerHealth"))
        {
            playerData.health = PlayerPrefs.GetInt("PlayerHealth");
        }
        if (PlayerPrefs.HasKey("PlayerInventory"))
        {
            string invString = PlayerPrefs.GetString("PlayerInventory");
            playerData.inventory = new List<string>(invString.Split(','));
        }
        // After loading progress, you might want to actually load the saved level:
        LoadLevel(currentLevelIndex);
    }
}

/// <summary>
/// Simple container for player data that persists between levels.
/// </summary>
[System.Serializable]
public class PlayerData
{
    public int health = 100;
    public List<string> inventory = new List<string>();
    // You can include other fields like position, mana, etc., as needed.
}
