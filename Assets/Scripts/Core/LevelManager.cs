// LevelManager.cs - Native Unity approach for level management
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

public class LevelManager : MonoBehaviour
{
    [Header("Scene References")]
    public static LevelManager Instance;

    [Header("Level Settings")]
    public string[] levelScenes; // Array of scene names to load in sequence
    public int currentLevelIndex = 0;

    [Header("Player Settings")]
    public GameObject playerPrefab;
    public string playerSpawnPointTag = "PlayerSpawn";

    [Header("Camera Settings")]
    public CameraManager cameraManager;

    [Header("Loading Screen")]
    public GameObject loadingScreen;
    public UnityEngine.UI.Slider loadingBar;

    [Header("Runtime References")]
    private GameObject currentPlayer;
    private List<AsyncOperation> loadOperations = new List<AsyncOperation>();

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Start the game with the first level if we're not already in it
        if (SceneManager.GetActiveScene().name != levelScenes[currentLevelIndex])
        {
            LoadLevel(currentLevelIndex);
        }
        else
        {
            // We're already in the correct scene, just initialize
            InitializeLevel();
        }
    }

    // Load a specific level by index
    public void LoadLevel(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= levelScenes.Length)
        {
            Debug.LogError("Invalid level index: " + levelIndex);
            return;
        }

        currentLevelIndex = levelIndex;
        StartCoroutine(LoadLevelAsync(levelScenes[levelIndex]));
    }

    // Load the next level in sequence
    public void LoadNextLevel()
    {
        int nextLevel = currentLevelIndex + 1;

        // Check if we've reached the end of all levels
        if (nextLevel >= levelScenes.Length)
        {
            Debug.Log("All levels completed!");
            // You could load a game completion scene or restart from level 0
            LoadMainMenu();
            return;
        }

        LoadLevel(nextLevel);
    }

    // Reload the current level (after death for example)
    public void ReloadCurrentLevel()
    {
        LoadLevel(currentLevelIndex);
    }

    // Load the main menu
    public void LoadMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    // Asynchronous level loading with loading screen
    private IEnumerator LoadLevelAsync(string sceneName)
    {
        // Show loading screen
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(true);
            if (loadingBar != null)
            {
                loadingBar.value = 0f;
            }
        }

        // Start loading the scene asynchronously
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false; // Don't switch scenes immediately

        // Wait until the scene is almost loaded
        while (asyncLoad.progress < 0.9f)
        {
            // Update loading bar
            if (loadingBar != null)
            {
                loadingBar.value = asyncLoad.progress;
            }

            yield return null;
        }

        // Set loading bar to almost complete
        if (loadingBar != null)
        {
            loadingBar.value = 0.9f;
        }

        // Do any additional loading work here if needed
        yield return new WaitForSeconds(0.5f); // Small delay for visual polish

        // Finish loading
        asyncLoad.allowSceneActivation = true;

        // Wait for scene to fully load
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // Update loading bar to complete
        if (loadingBar != null)
        {
            loadingBar.value = 1f;
        }

        // Initialize level
        InitializeLevel();

        // Hide loading screen
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(false);
        }
    }

    // Initialize the current level, spawning player and setting up camera
    private void InitializeLevel()
    {
        // Find the player spawn point
        GameObject spawnPoint = GameObject.FindGameObjectWithTag(playerSpawnPointTag);
        Vector3 spawnPosition = spawnPoint != null ? spawnPoint.transform.position : Vector3.zero;

        // Spawn player
        if (playerPrefab != null)
        {
            if (currentPlayer != null)
            {
                Destroy(currentPlayer);
            }

            currentPlayer = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);

            // Set up camera to follow player
            if (cameraManager != null)
            {
                cameraManager.SetCameraTarget(currentPlayer.transform);
            }
        }

        // Find and set up camera bounds
        SetupCameraBounds();

        // Call level events (you can add your own level specific initialization here)
        LevelEvents levelEvents = FindFirstObjectByType<LevelEvents>();
        if (levelEvents != null)
        {
            levelEvents.OnLevelStart();
        }
    }

    // Setup camera bounds based on the level colliders
    private void SetupCameraBounds()
    {
        if (cameraManager != null)
        {
            // Find the level bounds object
            GameObject boundaryObject = GameObject.FindGameObjectWithTag("LevelBounds");

            if (boundaryObject != null && boundaryObject.GetComponent<Collider2D>() != null)
            {
                // Set camera bounds
                cameraManager.UpdateCameraBounds(boundaryObject.GetComponent<Collider2D>());
            }
            else
            {
                Debug.LogWarning("No level bounds found! Creating automatic bounds from tilemaps.");

                // Create bounds from the tilemap size if no explicit bounds are set
                CreateBoundsFromTilemaps();
            }
        }
    }

    // Create camera bounds automatically based on the tilemap extents
    private void CreateBoundsFromTilemaps()
    {
        // Find all tilemaps in the scene
        Tilemap[] tilemaps = FindObjectsOfType<Tilemap>();

        if (tilemaps.Length == 0)
        {
            Debug.LogError("No tilemaps found in the scene!");
            return;
        }

        // Calculate the combined bounds of all tilemaps
        Bounds combinedBounds = new Bounds();
        bool firstBound = true;

        foreach (Tilemap tilemap in tilemaps)
        {
            // Skip empty tilemaps
            if (tilemap.cellBounds.size == Vector3Int.zero)
                continue;

            if (firstBound)
            {
                combinedBounds = new Bounds(tilemap.localBounds.center + tilemap.transform.position, tilemap.localBounds.size);
                firstBound = false;
            }
            else
            {
                // Convert local bounds to world space and combine
                Bounds worldBounds = new Bounds(
                    tilemap.localBounds.center + tilemap.transform.position,
                    tilemap.localBounds.size
                );
                combinedBounds.Encapsulate(worldBounds);
            }
        }

        // If we found valid bounds
        if (!firstBound)
        {
            // Create a new GameObject for the bounds
            GameObject boundsObject = new GameObject("LevelBounds");
            boundsObject.tag = "LevelBounds";

            // Add a box collider with the combined size
            BoxCollider2D boxCollider = boundsObject.AddComponent<BoxCollider2D>();
            boxCollider.offset = combinedBounds.center;
            boxCollider.size = combinedBounds.size;
            boxCollider.isTrigger = true;

            // Apply to camera
            cameraManager.UpdateCameraBounds(boxCollider);
        }
    }

    // Save the current level state (for returning after a sub-area)
    public void SaveLevelState()
    {
        // You can implement save state logic here
        // For example, record player health, inventory, position, etc.
    }

    // Load a saved level state
    public void LoadLevelState()
    {
        // You can implement load state logic here
        // Save current player before leaving
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // Don't destroy, just make it persistent
            DontDestroyOnLoad(player);
        }

        int nextIndex = currentLevelIndex + 1;
        if (nextIndex < levelSceneNames.Length)
        {
            LoadLevel(nextIndex);
        }
        else
        {
            // All levels completed, load victory scene
            SceneManager.LoadScene("WinningGame");
        }
    }
    private void SetupGameplayScene(string sceneName)
    {
        // Find player spawn point
        GameObject spawnObj = GameObject.FindWithTag("PlayerSpawn");
        if (spawnObj == null)
        {
            Debug.LogError("No PlayerSpawn found in scene: " + sceneName);
            return;
        }

        // Log player status for debugging
        Debug.Log($"Player status: {(currentPlayerInstance != null ? (currentPlayerInstance.activeSelf ? "Active" : "Inactive") : "Null")}");

        // Check if player exists but is inactive (from main menu)
        if (currentPlayerInstance != null && !currentPlayerInstance.activeSelf)
        {
            currentPlayerInstance.SetActive(true);
            currentPlayerInstance.transform.position = spawnObj.transform.position;
        }
        // Check if player exists and is active
        else if (currentPlayerInstance != null && currentPlayerInstance.activeSelf)
        {
            currentPlayerInstance.transform.position = spawnObj.transform.position;
        }
        // No player exists, create new one
        else
        {
            SpawnPlayer(spawnObj.transform.position);
        }

        // Set camera to follow player
        if (CameraManager.Instance != null && currentPlayerInstance != null)
        {
            CameraManager.Instance.SetTarget(currentPlayerInstance.transform);
        }
    }
    /// <summary>
    /// Reload the current level (after death)
    /// </summary>
    public void ReloadCurrentLevel()
    {
        LoadLevel(currentLevelIndex);
    }

    /// <summary>
    /// Save game progress to PlayerPrefs
    /// </summary>
    public void SaveProgress()
    {
        PlayerPrefs.SetInt("CurrentLevel", currentLevelIndex);
        PlayerPrefs.SetInt("PlayerHealth", playerData.health);
        PlayerPrefs.SetFloat("PlayerMana", playerData.mana);
        PlayerPrefs.SetInt("CollectedRelics", playerData.collectedRelics);
        PlayerPrefs.SetInt("PlayerLives", currentLives);
        PlayerPrefs.SetString("PlayerInventory", string.Join(",", playerData.inventory));
        PlayerPrefs.Save();

        Debug.Log("Game progress saved");
    }


    // Add this to your LevelManager.cs - within the class

    [Header("Level Boundaries")]
    [SerializeField] private bool useCustomBounds = true;
    [SerializeField] private Vector2 levelBoundsSize = new Vector2(784f, 512f); // Default size based on 49x32 cells

    public void CreateLevelBoundaries()
    {
        // Create empty GameObject for boundaries
        GameObject boundaries = new GameObject("LevelBoundaries");
        boundaries.tag = "LevelBounds";

        // Add a composite collider for better performance (optional)
        Rigidbody2D rb = boundaries.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;

        // Create individual colliders
        // Bottom boundary
        CreateBoundaryCollider(boundaries, new Vector2(0, -levelBoundsSize.y / 2 - 1), new Vector2(levelBoundsSize.x, 2));

        // Top boundary
        CreateBoundaryCollider(boundaries, new Vector2(0, levelBoundsSize.y / 2 + 1), new Vector2(levelBoundsSize.x, 2));

        // Left boundary
        CreateBoundaryCollider(boundaries, new Vector2(-levelBoundsSize.x / 2 - 1, 0), new Vector2(2, levelBoundsSize.y));

        // Right boundary
        CreateBoundaryCollider(boundaries, new Vector2(levelBoundsSize.x / 2 + 1, 0), new Vector2(2, levelBoundsSize.y));

        Debug.Log($"Created level boundaries: {levelBoundsSize.x}x{levelBoundsSize.y}");

        // Setup camera bounds if camera manager exists
        if (cameraManager != null)
        {
            BoxCollider2D boundsCollider = boundaries.AddComponent<BoxCollider2D>();
            boundsCollider.isTrigger = true;
            boundsCollider.size = levelBoundsSize;
            cameraManager.UpdateCameraBounds(boundsCollider);
        }
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
    /// Load game progress from PlayerPrefs
    /// </summary>
    public void LoadProgress()
    {
        if (PlayerPrefs.HasKey("CurrentLevel"))
        {
            currentLevelIndex = PlayerPrefs.GetInt("CurrentLevel");
            playerData.health = PlayerPrefs.GetInt("PlayerHealth", 100);
            playerData.mana = PlayerPrefs.GetFloat("PlayerMana", 100);
            playerData.collectedRelics = PlayerPrefs.GetInt("CollectedRelics", 0);
            currentLives = PlayerPrefs.GetInt("PlayerLives", maxLives);
            UpdateLivesUI();

            string invString = PlayerPrefs.GetString("PlayerInventory", "");
            if (!string.IsNullOrEmpty(invString))
            {
                playerData.inventory = new List<string>(invString.Split(','));
            }

            // Load the saved level
            LoadLevel(currentLevelIndex);
        }
        else
        {
            // No saved game, start a new game
            LoadLevel(startingLevelIndex);
        }
    }

    /// <summary>
    /// Creates a new save game by resetting progress and starting from the beginning
    /// </summary>
    public void NewGame()
    {
        // Reset player data
        playerData = new PlayerData();

        // Reset lives
        currentLives = maxLives;
        UpdateLivesUI();

        // Reset to first level
        currentLevelIndex = startingLevelIndex;

        // Start from the beginning
        LoadLevel(startingLevelIndex);
    }


    /// <summary>
    /// Handle trigger events in the level (level completion, checkpoints, etc.)
    /// </summary>
    public void HandleTriggerEvent(string triggerID)
    {
        switch (triggerID)
        {
            case "LevelEnd":
                // Level completed, save state and proceed to next level
                SavePlayerState();
                SaveProgress();
                LoadNextLevel();
                break;

            case "Checkpoint":
                // Save at checkpoint
                SavePlayerState();
                SaveProgress();
                break;

            case "CollectRelic":
                // Collect a relic
                playerData.collectedRelics++;
                SaveProgress();
                break;

            case "PlayerDied":
                // Player died, show game over
                if (GameSceneManager.Instance != null)
                {
                    GameSceneManager.Instance.LoadGameOver();
                }
                else
                {
                    // Fallback if scene manager not found
                    ReloadCurrentLevel();
                }
                break;

                // Add other trigger handlers as needed
        }
    }

    /// <summary>
    /// Handles level completion and transitions to the next level
    /// </summary>
    public void LevelComplete()
    {
        // Log the level completion
        Debug.Log($"Level {currentLevelIndex + 1} completed!");

        // Save player state before leaving
        SavePlayerState();

        // Optional: Play level complete sound
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound("level_complete");
        }

        // Optional: Display a level complete UI element temporarily
        // If you had a UI panel for level completion, you would show it here

        // Progress to the next level after a short delay
        StartCoroutine(LoadNextLevelAfterDelay(2f));
    }

    private IEnumerator LoadNextLevelAfterDelay(float delay)
    {
        // Wait for the specified delay
        yield return new WaitForSeconds(delay);

        // Load the next level
        LoadNextLevel();
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}

// Extension method to safely check if a method exists
public static class ComponentExtensions
{
    public static bool HasMethod(this Component component, string methodName)
    {
        return component.GetType().GetMethod(methodName) != null;
    }
}