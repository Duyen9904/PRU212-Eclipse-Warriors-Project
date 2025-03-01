
// LevelController.cs - Manages level progression, respawn, and game state
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;

public class LevelController : MonoBehaviour
{
    [Header("Level Settings")]
    public static LevelController Instance;
    public Transform playerSpawnPoint;
    public float respawnDelay = 2f;
    public int currentLevel = 1;
    public int totalLevels = 10;

    [Header("UI References")]
    public GameObject gameOverPanel;
    public GameObject levelCompletePanel;
    public GameObject pauseMenuPanel;
    public Text levelText;

    [Header("References")]
    private GameObject player;
    private bool isGamePaused = false;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Find player
        player = GameObject.FindGameObjectWithTag("Player");

        // Update level text
        if (levelText != null)
        {
            levelText.text = "Level " + currentLevel;
        }

        // Hide UI panels
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (levelCompletePanel != null) levelCompletePanel.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
    }

    private void Update()
    {
        // Handle pause menu
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePauseMenu();
        }
    }

    public void PlayerDied()
    {
        // Show game over after delay
        StartCoroutine(ShowGameOverAfterDelay());
    }

    private IEnumerator ShowGameOverAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);

        // Show game over panel
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
    }

    public void Respawn()
    {
        // Hide game over panel
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        // Restart level
        RestartLevel();
    }

    public void RestartLevel()
    {
        // Reload current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LevelComplete()
    {
        // Show level complete panel
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(true);
        }
    }

    public void NextLevel()
    {
        // Increment level
        currentLevel++;

        // Check if we completed all levels
        if (currentLevel > totalLevels)
        {
            // Load main menu or end game scene
            SceneManager.LoadScene("MainMenu");
        }
        else
        {
            // Load next level scene
            SceneManager.LoadScene("Level" + currentLevel);
        }
    }

    public void TogglePauseMenu()
    {
        isGamePaused = !isGamePaused;

        // Set time scale (0 = paused, 1 = normal)
        Time.timeScale = isGamePaused ? 0f : 1f;

        // Show/hide pause menu
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(isGamePaused);
        }
    }

    public void ReturnToMainMenu()
    {
        // Ensure time scale is reset
        Time.timeScale = 1f;

        // Load main menu scene
        SceneManager.LoadScene("MainMenu");
    }
}
