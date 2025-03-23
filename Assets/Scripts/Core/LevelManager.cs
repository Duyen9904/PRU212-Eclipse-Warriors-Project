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
                cameraManager.SetTarget(currentPlayer.transform);
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
    }
}