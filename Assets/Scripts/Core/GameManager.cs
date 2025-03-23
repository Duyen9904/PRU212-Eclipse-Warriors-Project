using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // Singleton pattern for easy access
    public static GameManager Instance { get; private set; }

    [Header("Character Prefabs")]
    [SerializeField] private GameObject wizardPrefab;
    [SerializeField] private GameObject roguePrefab;
    [SerializeField] private GameObject knightPrefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform characterSelectionSpawnPoint;

    // Current player data
    private GameObject currentPlayerInstance;
    private string selectedCharacter;

    // Reference to Database
    private Character playerData;

    // Player progress tracking
    private int currentBiomeIndex = 0;
    private string[] biomeScenes = { "GlacierBiome", "VolcanoBiome", "ForestBiome", "DungeonBiome", "AbyssalGate" };

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Initialize player data manager
            playerData = GetComponent<Character>();
            if (playerData == null)
            {
                playerData = gameObject.AddComponent<PlayerDataManager>();
            }
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Subscribe to scene load event
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        // Unsubscribe from scene load event
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// Event handler for when a scene is loaded
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Setup camera for current scene
        if (CameraManager.Instance != null)
        {
            CameraManager.Instance.SetupForScene(scene.name);
        }

        // Handle different scenes
        switch (scene.name)
        {
            case "CharacterSelection":
                SetupCharacterSelectionScene();
                break;
            case "GlacierBiome":
            case "VolcanoBiome":
            case "ForestBiome":
            case "DungeonBiome":
            case "AbyssalGate":
                SetupGameplayScene(scene.name);
                break;
        }
    }

    /// <summary>
    /// Setup character selection scene
    /// </summary>
    private void SetupCharacterSelectionScene()
    {
        // Clear any existing player instance
        if (currentPlayerInstance != null)
        {
            Destroy(currentPlayerInstance);
            currentPlayerInstance = null;
        }

        // Find spawn point if not set
        if (characterSelectionSpawnPoint == null)
        {
            GameObject spawnObj = GameObject.FindWithTag("CharacterSpawnPoint");
            if (spawnObj != null)
            {
                characterSelectionSpawnPoint = spawnObj.transform;
            }
        }
    }

    /// <summary>
    /// Setup gameplay scene with appropriate player and camera
    /// </summary>
    /// <param name="sceneName">The current gameplay scene</param>
    private void SetupGameplayScene(string sceneName)
    {
        // Find player spawn point
        GameObject spawnObj = GameObject.FindWithTag("PlayerSpawnPoint");
        if (spawnObj == null)
        {
            Debug.LogError("No PlayerSpawnPoint found in scene: " + sceneName);
            return;
        }

        // Spawn player if not already present
        if (currentPlayerInstance == null)
        {
            SpawnPlayer(spawnObj.transform.position);
        }
        else
        {
            // Reposition existing player
            currentPlayerInstance.transform.position = spawnObj.transform.position;
        }

        // Set camera to follow player
        if (CameraManager.Instance != null && currentPlayerInstance != null)
        {
            CameraManager.Instance.SetTarget(currentPlayerInstance.transform);
        }
    }

    /// <summary>
    /// Select character and store in database
    /// </summary>
    public void SelectCharacter(string characterType)
    {
        selectedCharacter = characterType;

        // Save to player data
        if (playerData != null)
        {
            playerData.SaveSelectedCharacter(characterType);
        }
    }

    /// <summary>
    /// Spawn player character based on selection
    /// </summary>
    private void SpawnPlayer(Vector3 position)
    {
        // If no character is selected, try to load from data
        if (string.IsNullOrEmpty(selectedCharacter) && playerData != null)
        {
            selectedCharacter = playerData.LoadSelectedCharacter();
        }

        // Default to knight if no selection
        if (string.IsNullOrEmpty(selectedCharacter))
        {
            selectedCharacter = "Knight";
        }

        // Instantiate the appropriate prefab
        GameObject prefabToSpawn = null;
        switch (selectedCharacter)
        {
            case "Wizard":
                prefabToSpawn = wizardPrefab;
                break;
            case "Rogue":
                prefabToSpawn = roguePrefab;
                break;
            case "Knight":
            default:
                prefabToSpawn = knightPrefab;
                break;
        }

        if (prefabToSpawn != null)
        {
            currentPlayerInstance = Instantiate(prefabToSpawn, position, Quaternion.identity);

            // Make the player persist between scenes
            DontDestroyOnLoad(currentPlayerInstance);
        }
    }

    /// <summary>
    /// Start the game with the selected character
    /// </summary>
    public void StartGame()
    {
        currentBiomeIndex = 0;
        LoadScene(biomeScenes[currentBiomeIndex]);
    }

    /// <summary>
    /// Advance to the next level
    /// </summary>
    public void AdvanceToNextLevel()
    {
        currentBiomeIndex++;

        if (currentBiomeIndex < biomeScenes.Length)
        {
            LoadScene(biomeScenes[currentBiomeIndex]);
        }
        else
        {
            // If we've completed all biomes, go to the winning scene
            LoadScene("WinningGame");
        }
    }

    /// <summary>
    /// Game over, return to main menu
    /// </summary>
    public void GameOver()
    {
        // Clean up player instance
        if (currentPlayerInstance != null)
        {
            Destroy(currentPlayerInstance);
            currentPlayerInstance = null;
        }

        LoadScene("GameOver");
    }

    /// <summary>
    /// Load a scene by name
    /// </summary>
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}