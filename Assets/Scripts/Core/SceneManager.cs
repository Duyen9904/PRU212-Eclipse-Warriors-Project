using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

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

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetupInitialState();
        }
        else
        {
            Destroy(gameObject);
        }
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

    private IEnumerator FadeIn()
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
}