using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.AddressableAssets;
using Unity.Cinemachine;

public class GameSceneManager : MonoBehaviour
{
    // Singleton pattern
    public static GameSceneManager Instance { get; private set; }

    // Scene names - customize these to match your scene names
    public enum SceneType
    {
        MainMenu,
        CharacterSelection,
        HowToPlay,
        GlacierBiome,
        VolcanoBiome,
        ForestBiome,
        DungeonBiome,
        AbyssalGate,
        GameOver,
        Resume,
        Victory,
    }

    // Dictionary to map scene types to scene names
    private Dictionary<SceneType, string> sceneNames = new Dictionary<SceneType, string>
    {
        { SceneType.MainMenu, "MainMenu" },
        { SceneType.CharacterSelection, "CharacterSelection" },
        { SceneType.HowToPlay, "HowToPlay" },
        { SceneType.GlacierBiome, "GlacierBiome" },
        { SceneType.VolcanoBiome, "VolcanoBiome" },
        { SceneType.ForestBiome, "ForestBiome" },
        { SceneType.DungeonBiome, "DungeonBiome" },
        { SceneType.AbyssalGate, "AbyssalGate" },
        { SceneType.GameOver, "GameOver" },
        { SceneType.Resume, "Resume" },
        { SceneType.Victory, "WinningGame" }
    };

    private Dictionary<string, SceneInstance> loadedScenes = new Dictionary<string, SceneInstance>();

    // Keep track of the current level for progression
    public SceneType CurrentScene { get; private set; }

    // Fade transition references
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeDuration = 1.0f;

    // Selected character information to persist between scenes
    public enum CharacterType { Wizard, Rogue, Knight }
    public CharacterType SelectedCharacter { get; private set; }

    // Relic collection status
    public bool[] CollectedRelics { get; private set; } = new bool[4]; // One for each biome except Abyssal Gate

    [Header("Character Prefab")]
    [SerializeField] private GameObject characterPrefab;

    // Reference to currently spawned player
    private GameObject currentPlayerInstance;

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetupInitialState();

            // Subscribe to scene loaded event
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe when destroyed
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void SetupInitialState()
    {
        // Default to main menu when game starts
        CurrentScene = SceneType.MainMenu;
        SelectedCharacter = CharacterType.Wizard; // Default character

        // Reset relic collection
        for (int i = 0; i < CollectedRelics.Length; i++)
        {
            CollectedRelics[i] = false;
        }
    }

    // Method to load character selection scene
    public void LoadCharacterSelection()
    {
        LoadScene(SceneType.CharacterSelection);
    }

    // Method to set selected character
    public void SetSelectedCharacter(CharacterType character)
    {
        SelectedCharacter = character;
    }

    // Method to start new game (load first level)
    public void StartNewGame()
    {
        // Reset relic collection
        for (int i = 0; i < CollectedRelics.Length; i++)
        {
            CollectedRelics[i] = false;
        }

        // Load Glacier Biome (first level)
        LoadScene(SceneType.GlacierBiome);
    }

    // Method to load the next level based on current level
    public void LoadNextLevel()
    {
        SceneType nextScene;

        // Determine which level comes next
        switch (CurrentScene)
        {
            case SceneType.GlacierBiome:
                nextScene = SceneType.VolcanoBiome;
                CollectedRelics[0] = true; // Collected Glacier relic
                break;
            case SceneType.VolcanoBiome:
                nextScene = SceneType.ForestBiome;
                CollectedRelics[1] = true; // Collected Volcano relic
                break;
            case SceneType.ForestBiome:
                nextScene = SceneType.DungeonBiome;
                CollectedRelics[2] = true; // Collected Forest relic
                break;
            case SceneType.DungeonBiome:
                nextScene = SceneType.AbyssalGate;
                CollectedRelics[3] = true; // Collected Dungeon relic
                break;
            case SceneType.AbyssalGate:
                nextScene = SceneType.Victory; // Player beat the game
                break;
            default:
                nextScene = SceneType.MainMenu; // Fallback
                break;
        }

        LoadScene(nextScene);
    }

    // Method to load game over scene
    public void LoadGameOver()
    {
        LoadScene(SceneType.GameOver);
    }

    // Method to return to main menu
    public void LoadMainMenu()
    {
        LoadScene(SceneType.MainMenu);
    }

    // Method to load a specific scene with transition
    public void LoadScene(SceneType sceneType)
    {
        StartCoroutine(LoadSceneWithTransition(sceneType));
    }

    private IEnumerator LoadSceneWithTransition(SceneType sceneType)
    {
        // Fade out
        yield return StartCoroutine(FadeOut());

        // Update current scene
        CurrentScene = sceneType;

        // Load the scene
        SceneManager.LoadScene(sceneNames[sceneType]);

        // Fade in
        yield return StartCoroutine(FadeIn());
    }

    private IEnumerator FadeOut()
    {
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.gameObject.SetActive(true);

            float elapsedTime = 0f;
            while (elapsedTime < fadeDuration)
            {
                fadeCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            fadeCanvasGroup.alpha = 1f;
        }
        else
        {
            yield return null;
        }
    }

    private IEnumerator FadeIn()
    {
        if (fadeCanvasGroup != null)
        {
            float elapsedTime = 0f;
            while (elapsedTime < fadeDuration)
            {
                fadeCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.gameObject.SetActive(false);
        }
        else
        {
            yield return null;
        }
    }

    // Scene loaded event handler
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Check if this is a gameplay scene
        if (IsGameplayScene(scene.name))
        {
            StartCoroutine(SpawnPlayerAndSetupCamera());
        }
    }

    private bool IsGameplayScene(string sceneName)
    {
        return sceneName == "GlacierBiome" ||
               sceneName == "VolcanoBiome" ||
               sceneName == "ForestBiome" ||
               sceneName == "DungeonBiome" ||
               sceneName == "AbyssalGate";
    }

    private IEnumerator SpawnPlayerAndSetupCamera()
    {
        // Wait a frame to make sure scene is fully loaded
        yield return null;

        // Find spawn point
        GameObject spawnPoint = GameObject.FindGameObjectWithTag("PlayerSpawn");
       if (spawnPoint == null)
        {
            Debug.LogWarning("No player spawn point found in scene!");
        }
        Vector3 spawnPosition = spawnPoint != null ? spawnPoint.transform.position : Vector3.zero;

        // Spawn appropriate character based on selection
        if (currentPlayerInstance != null)
        {
            Destroy(currentPlayerInstance);
        }

        // Instantiate the character prefab
        currentPlayerInstance = Instantiate(characterPrefab, spawnPosition, Quaternion.identity);

        // Apply character customization based on selection
        CharacterCustomizer customizer = currentPlayerInstance.GetComponent<CharacterCustomizer>();
        if (customizer != null)
        {
            customizer.ApplyCharacterData((int)SelectedCharacter);
        }

        // Make player persist between scenes
        DontDestroyOnLoad(currentPlayerInstance);

        // Find and set up camera
        CinemachineCamera vcam = FindObjectOfType<CinemachineCamera>();
        if (vcam != null && currentPlayerInstance != null)
        {
            vcam.Follow = currentPlayerInstance.transform;
            Debug.Log("Camera tracking target set to: " + currentPlayerInstance.name);
        }
        else
        {
            Debug.LogWarning("Could not find CinemachineCamera or player is null");
        }
    }

    public void CollectRelic(int relicIndex)
    {
        if (relicIndex >= 0 && relicIndex < CollectedRelics.Length)
        {
            CollectedRelics[relicIndex] = true;
            Debug.Log($"Relic {relicIndex} collected!");

            // Save the collection state (optional)
            SaveRelicCollectionState();
        }
    }

    private void SaveRelicCollectionState()
    {
        for (int i = 0; i < CollectedRelics.Length; i++)
        {
            PlayerPrefs.SetInt("Relic_" + i, CollectedRelics[i] ? 1 : 0);
        }
        PlayerPrefs.Save();
    }

    // Method to load relic collection state
    private void LoadRelicCollectionState()
    {
        for (int i = 0; i < CollectedRelics.Length; i++)
        {
            CollectedRelics[i] = PlayerPrefs.GetInt("Relic_" + i, 0) == 1;
        }
    }

    // Method to check if all relics have been collected
    public bool HasAllRelics()
    {
        foreach (bool relic in CollectedRelics)
        {
            if (!relic) return false;
        }
        return true;
    }

    // Method to load how to play scene
    public void LoadHowToPlay()
    {
        LoadScene(SceneType.HowToPlay);
    }

    // Method to continue game (resume from last level)
    public void ContinueGame()
    {
        // Load the current scene again (or use saved scene info if you implement save/load)
        LoadScene(CurrentScene);
    }

    // Public method to get current player instance
    public GameObject GetCurrentPlayer()
    {
        return currentPlayerInstance;
    }
}   