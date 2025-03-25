using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

#if LDTK_IMPORTER
using LDtkUnity;
#endif

public class LevelManager : MonoBehaviour
{
    // Singleton instance
    public static LevelManager Instance;

    [Header("Level Configuration")]
    [SerializeField] private string[] levelSceneNames; // Names of Unity scenes for each level
    [SerializeField] private bool[] usesLDTK; // Which levels use LDtk vs. traditional Unity scenes
    [SerializeField] private int startingLevelIndex = 0;
    private int currentLevelIndex = 0;

    [Header("Player Lives Settings")]
    [SerializeField] private int maxLives = 3;
    private int currentLives;
    [SerializeField] private float respawnDelay = 2f;
    private bool isRespawning = false;

    [Header("UI References")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private UnityEngine.UI.Text livesText;
    [SerializeField] private GameObject pauseMenuPanel;

    [Header("LDtk Settings")]
#if LDTK_IMPORTER
    [SerializeField] private LDtkComponentProject ldtkProject;
    [SerializeField] private TextAsset[] ldtkLevelFiles; // Array of LDtk level files
#endif

    [Header("Traditional Level Settings")]
    [SerializeField] private Transform[] standardSpawnPoints;
    [SerializeField] private GameObject[] levelEnvironments;

    [Header("Player Prefabs")]
    [SerializeField] private GameObject wizardPrefab;
    [SerializeField] private GameObject roguePrefab;
    [SerializeField] private GameObject knightPrefab;

    [Header("Camera Settings")]
    [SerializeField] private CameraManager cameraManager;

    [Header("Loading Screen")]
    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private UnityEngine.UI.Slider loadingBar;

    // Runtime references
    private GameObject currentLevelInstance;
    private GameObject currentPlayerInstance;
    private PlayerController playerController;

    // Player persistent data
    [System.Serializable]
    public class PlayerData
    {
        public int health = 100;
        public float mana = 100;
        public int collectedRelics = 0;
        public List<string> inventory = new List<string>();
        public Vector3 lastPosition;
    }

    public PlayerData playerData = new PlayerData();

    private void Awake()
    {
        // Singleton setup
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

        // Initialize current level index
        currentLevelIndex = startingLevelIndex;

        // Initialize lives
        currentLives = maxLives;
        UpdateLivesUI();

        // Hook into scene load event
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        // Hide UI panels
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);

        // Check if we're already in one of our levels
        string currentSceneName = SceneManager.GetActiveScene().name;
        bool alreadyInLevel = false;

        for (int i = 0; i < levelSceneNames.Length; i++)
        {
            if (currentSceneName == levelSceneNames[i])
            {
                currentLevelIndex = i;
                alreadyInLevel = true;
                // Initialize the current level
                InitializeCurrentLevel();
                break;
            }
        }

        // If we're not in a game level, load the initial level
        if (!alreadyInLevel && levelSceneNames.Length > 0)
        {
            LoadLevel(currentLevelIndex);
        }
    }

    private void Update()
    {
        // Handle pause menu
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePauseMenu();
        }
    }

    /// <summary>
    /// Called when the player dies
    /// </summary>
    public void PlayerDied()
    {
        // Don't process if we're already respawning
        if (isRespawning) return;

        isRespawning = true;

        // Decrement lives
        currentLives--;
        UpdateLivesUI();

        Debug.Log("Player died! Lives remaining: " + currentLives);

        // Check if game over
        if (currentLives <= 0)
        {
            // Game over - show panel after delay
            StartCoroutine(ShowGameOverAfterDelay());
        }
        else
        {
            // Still have lives - respawn
            StartCoroutine(RespawnPlayerAfterDelay());
        }
    }

    private IEnumerator RespawnPlayerAfterDelay()
    {
        // Wait for animation/effects
        yield return new WaitForSeconds(respawnDelay);

        // Find the player spawn point
        Transform spawnPoint = FindSpawnPoint();

        if (currentPlayerInstance != null)
        {
            // Reset and reposition existing player
            currentPlayerInstance.transform.position = spawnPoint != null ?
                spawnPoint.position : Vector3.zero;

            // Reset player state
            PlayerController controller = currentPlayerInstance.GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.ResetPlayer();
            }

            // Apply saved player data
            ApplyPlayerData();
        }
        else
        {
            // Recreate player if instance was destroyed
            SetupPlayer();
        }

        // Reset the respawning flag
        isRespawning = false;
    }

    private IEnumerator ShowGameOverAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);

        // Show game over panel
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        else
        {
            // No game over panel - handle via GameSceneManager
            HandleTriggerEvent("PlayerDied");
        }
    }

    // Called by UI button
    public void RestartFromCheckpoint()
    {
        // Hide game over panel
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        // Reset lives and respawn
        currentLives = maxLives;
        UpdateLivesUI();
        isRespawning = false;

        // Reload current level
        ReloadCurrentLevel();
    }

    public void TogglePauseMenu()
    {
        bool isPaused = Time.timeScale < 0.1f;

        // Toggle pause state
        Time.timeScale = isPaused ? 1f : 0f;

        // Show/hide pause menu
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(!isPaused);
        }
    }

    // Update the lives UI text
    private void UpdateLivesUI()
    {
        if (livesText != null)
        {
            livesText.text = "Lives: " + currentLives;
        }
    }

    /// <summary>
    /// Loads a level by index with transition effects
    /// </summary>
    public void LoadLevel(int index)
    {
        // Save current player state before leaving
        SavePlayerState();

        // Bounds check
        if (index < 0 || index >= levelSceneNames.Length)
        {
            Debug.LogError("Level index out of range: " + index);
            return;
        }

        currentLevelIndex = index;
        StartCoroutine(LoadLevelRoutine(levelSceneNames[index]));
    }

    private void SpawnPlayer(Vector3 position)
    {
        // Get selected character from GameSceneManager
        GameSceneManager.CharacterType selectedCharacter =
            GameSceneManager.Instance != null ?
            GameSceneManager.Instance.SelectedCharacter :
            GameSceneManager.CharacterType.Knight; // Default

        // Spawn the appropriate character prefab
        GameObject prefabToSpawn = null;
        switch (selectedCharacter)
        {
            case GameSceneManager.CharacterType.Wizard:
                prefabToSpawn = wizardPrefab;
                break;
            case GameSceneManager.CharacterType.Rogue:
                prefabToSpawn = roguePrefab;
                break;
            case GameSceneManager.CharacterType.Knight:
            default:
                prefabToSpawn = knightPrefab;
                break;
        }

        if (prefabToSpawn != null)
        {
            currentPlayerInstance = Instantiate(prefabToSpawn, position, Quaternion.identity);
            DontDestroyOnLoad(currentPlayerInstance);

            // Get player controller reference
            playerController = currentPlayerInstance.GetComponent<PlayerController>();

            // Apply saved player data
            ApplyPlayerData();
        }
        else
        {
            Debug.LogError("Failed to spawn player character. No prefab available.");
        }
    }

    public void LoadScene(string sceneName)
    {
        // Hide player in non-gameplay scenes
        if (currentPlayerInstance != null)
        {
            bool isGameplayScene =
                sceneName == "GlacierBiome" ||
                sceneName == "VolcanoBiome" ||
                sceneName == "ForestBiome" ||
                sceneName == "DungeonBiome" ||
                sceneName == "AbyssalGate";

            currentPlayerInstance.SetActive(isGameplayScene);
        }

        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator LoadLevelRoutine(string sceneName)
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

        // Small delay for visual polish
        yield return new WaitForSeconds(0.5f);

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

        // Hide loading screen
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(false);
        }
    }

    /// <summary>
    /// Called after a scene is loaded to set up the level
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (currentLevelIndex >= 0 && currentLevelIndex < levelSceneNames.Length)
        {
            // Initialize level after loading
            InitializeCurrentLevel();
        }
    }

    /// <summary>
    /// Initializes the current level based on its type (LDtk or traditional)
    /// </summary>
    private void InitializeCurrentLevel()
    {
        // Check if the current level uses LDtk
        if (currentLevelIndex < usesLDTK.Length && usesLDTK[currentLevelIndex])
        {
            // Load LDtk level
            LoadLDtkLevel();
        }
        else
        {
            // Load traditional Unity scene level
            LoadTraditionalLevel();
        }
        FixColliders();

        CreateLevelBoundaries();

        // Find or create player
        SetupPlayer();

        // Configure camera for this level
        SetupCamera();

        // Call level events
        LevelEvents levelEvents = FindFirstObjectByType<LevelEvents>();
        if (levelEvents != null)
        {
            levelEvents.OnLevelStart();
        }
    }

    // Add this method to your LevelManager script
    private void FixColliders()
    {
        // Find all box colliders in the scene
        BoxCollider2D[] colliders = FindObjectsOfType<BoxCollider2D>();
        int fixedCount = 0;

        foreach (var collider in colliders)
        {
            // If it's set as a trigger, make it a solid collider
            if (collider.isTrigger)
            {
                collider.isTrigger = false;
                fixedCount++;
            }
        }

        Debug.Log($"Fixed {fixedCount} trigger colliders to be solid colliders");
    }


    /// <summary>
    /// Loads an LDtk-based level
    /// </summary>
    private void LoadLDtkLevel()
    {
#if LDTK_IMPORTER
        if (ldtkProject != null && currentLevelIndex < ldtkLevelFiles.Length && ldtkLevelFiles[currentLevelIndex] != null)
        {
            // Clean up any existing level instance
            if (currentLevelInstance != null)
            {
                Destroy(currentLevelInstance);
            }

            // Instantiate the LDtk level
            TextAsset levelFile = ldtkLevelFiles[currentLevelIndex];
            currentLevelInstance = ldtkProject.InstantiateLevel(levelFile.name);
            
            if (currentLevelInstance == null)
            {
                Debug.LogError("Failed to instantiate LDtk level: " + levelFile.name);
            }
        }
        else
        {
            Debug.LogError("Missing LDtk configuration for level: " + currentLevelIndex);
        }
#else
        Debug.LogError("Trying to load LDtk level but LDtk importer is not enabled. Add LDTK_IMPORTER to Script Define Symbols.");
#endif
    }

    /// <summary>
    /// Loads a traditional Unity scene-based level
    /// </summary>
    private void LoadTraditionalLevel()
    {
        if (currentLevelIndex < levelEnvironments.Length && levelEnvironments[currentLevelIndex] != null)
        {
            // Clean up any existing level instance
            if (currentLevelInstance != null)
            {
                Destroy(currentLevelInstance);
            }

            // Activate the pre-configured environment
            GameObject levelPrefab = levelEnvironments[currentLevelIndex];
            currentLevelInstance = Instantiate(levelPrefab);
        }
        else
        {
            // In this case, the level is likely part of the scene itself
            Debug.Log("Using scene-embedded level for index: " + currentLevelIndex);
        }
    }

    /// <summary>
    /// Sets up the player character in the level
    /// </summary>
    private void SetupPlayer()
    {
        Transform spawnPoint = FindSpawnPoint();

        // First check if player instance already exists
        if (currentPlayerInstance != null)
        {
            // Reposition existing player
            currentPlayerInstance.transform.position = spawnPoint != null ?
                spawnPoint.position : Vector3.zero;

            // Get player controller reference
            playerController = currentPlayerInstance.GetComponent<PlayerController>();

            // Apply saved player data
            ApplyPlayerData();
            return;
        }

        // Get selected character from GameSceneManager
        GameSceneManager.CharacterType selectedCharacter =
            GameSceneManager.Instance != null ?
            GameSceneManager.Instance.SelectedCharacter :
            GameSceneManager.CharacterType.Knight; // Default

        // Spawn the appropriate character prefab
        GameObject prefabToSpawn = null;
        switch (selectedCharacter)
        {
            case GameSceneManager.CharacterType.Wizard:
                prefabToSpawn = wizardPrefab;
                break;
            case GameSceneManager.CharacterType.Rogue:
                prefabToSpawn = roguePrefab;
                break;
            case GameSceneManager.CharacterType.Knight:
            default:
                prefabToSpawn = knightPrefab;
                break;
        }

        if (prefabToSpawn != null && spawnPoint != null)
        {
            currentPlayerInstance = Instantiate(prefabToSpawn, spawnPoint.position, Quaternion.identity);
            DontDestroyOnLoad(currentPlayerInstance);

            // Get player controller reference
            playerController = currentPlayerInstance.GetComponent<PlayerController>();

            // Apply saved player data
            ApplyPlayerData();
        }
        else
        {
            Debug.LogError("Failed to spawn player character. Prefab or spawn point is missing.");
        }
    }



    /// <summary>
    /// Finds an appropriate spawn point in the level
    /// </summary>
    private Transform FindSpawnPoint()
    {
        // First try to find a tagged spawn point in the scene
        GameObject taggedSpawn = GameObject.FindGameObjectWithTag("PlayerSpawn");
        if (taggedSpawn != null)
        {
            return taggedSpawn.transform;
        }

#if LDTK_IMPORTER
        // Check for LDtk spawn point if using LDtk
        if (currentLevelIndex < usesLDTK.Length && usesLDTK[currentLevelIndex] && currentLevelInstance != null)
        {
            // Look for LDtk entity with spawn point identifier
            LDtkEntityInstance[] entities = currentLevelInstance.GetComponentsInChildren<LDtkEntityInstance>();
            foreach (var entity in entities)
            {
                if (entity.EntityIdentifier == "PlayerSpawn" || entity.EntityIdentifier == "SpawnPoint")
                {
                    return entity.transform;
                }
            }
        }
#endif

        // Use configured spawn point for this level
        if (currentLevelIndex < standardSpawnPoints.Length && standardSpawnPoints[currentLevelIndex] != null)
        {
            return standardSpawnPoints[currentLevelIndex];
        }

        // Fallback: return origin
        Debug.LogWarning("No spawn point found for level " + currentLevelIndex);
        return null;
    }

    /// <summary>
    /// Sets up the camera for the current level
    /// </summary>
    private void SetupCamera()
    {
        if (cameraManager != null && currentPlayerInstance != null)
        {
            // Set camera to follow player
            cameraManager.SetTarget(currentPlayerInstance.transform);

            // Configure camera bounds based on level
            Collider2D levelBounds = FindLevelBounds();
            if (levelBounds != null)
            {
                cameraManager.UpdateCameraBounds(levelBounds);
            }
        }
    }

    /// <summary>
    /// Finds the level boundary collider for camera confinement
    /// </summary>
    private Collider2D FindLevelBounds()
    {
        // First look for GameObject with LevelBounds tag
        GameObject boundsObj = GameObject.FindGameObjectWithTag("LevelBounds");
        if (boundsObj != null && boundsObj.GetComponent<Collider2D>() != null)
        {
            return boundsObj.GetComponent<Collider2D>();
        }

        // Create bounds from tilemaps if no explicit bounds are set
        Collider2D generatedBounds = CreateBoundsFromTilemaps();
        if (generatedBounds != null)
        {
            return generatedBounds;
        }

        Debug.LogWarning("No level bounds found for camera confinement");
        return null;
    }

    /// <summary>
    /// Creates camera bounds from tilemap extents if no explicit bounds exist
    /// </summary>
    private Collider2D CreateBoundsFromTilemaps()
    {
        // Find all tilemaps in the scene
        UnityEngine.Tilemaps.Tilemap[] tilemaps = FindObjectsOfType<UnityEngine.Tilemaps.Tilemap>();

        if (tilemaps.Length == 0)
        {
            return null;
        }

        // Calculate the combined bounds of all tilemaps
        Bounds combinedBounds = new Bounds();
        bool firstBound = true;

        foreach (var tilemap in tilemaps)
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

            return boxCollider;
        }

        return null;
    }

    /// <summary>
    /// Saves the current player state into persistent data
    /// </summary>
    private void SavePlayerState()
    {
        if (playerController == null) return;

        // Save health and stats
        PlayerStats stats = playerController.GetComponent<PlayerStats>();
        if (stats != null)
        {
            playerData.health = stats.GetCurrentHealth();
            playerData.mana = stats.GetCurrentMana();
        }

        // Save position
        playerData.lastPosition = playerController.transform.position;

        // Get inventory if available
        if (playerController.HasMethod("GetInventoryItems"))
        {
            // This would need to be implemented in your PlayerController
            // playerData.inventory = playerController.GetInventoryItems();
        }
    }

    /// <summary>
    /// Applies saved player data to the current player instance
    /// </summary>
    private void ApplyPlayerData()
    {
        if (playerController == null) return;

        // Apply health and mana
        PlayerStats stats = playerController.GetComponent<PlayerStats>();
        if (stats != null)
        {
            stats.SetHealth(playerData.health);
            stats.SetMana((int)playerData.mana);
        }

        // Apply inventory if that method exists
        if (playerController.HasMethod("SetInventoryItems"))
        {
            // This would need to be implemented in your PlayerController
            // playerController.SetInventoryItems(playerData.inventory);
        }
    }

    /// <summary>
    /// Load the next level in sequence
    /// </summary>
    // In LevelManager.cs
    public void LoadNextLevel()
    {
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