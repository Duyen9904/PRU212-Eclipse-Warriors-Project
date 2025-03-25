using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelExit : MonoBehaviour
{
    [Header("Requirements")]
    public bool requiresRelic = true;
    public int requiredRelicIndex = 0;

    [Header("Visual")]
    public GameObject lockedVisual;
    public GameObject unlockedVisual;
    public GameObject activationEffect;

    [Header("Message")]
    public string lockedMessage = "You need the relic to unlock this gate!";

    private bool isPlayerInRange = false;

    private void Start()
    {
        UpdateVisuals();
    }

    private void Update()
    {
        // Check for interaction when player is in range
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.E))
        {
            AttemptToExit();
        }
    }

    private void UpdateVisuals()
    {
        bool unlocked = !requiresRelic ||
            (GameSceneManager.Instance != null && GameSceneManager.Instance.CollectedRelics[requiredRelicIndex]);

        if (lockedVisual != null)
            lockedVisual.SetActive(!unlocked);

        if (unlockedVisual != null)
            unlockedVisual.SetActive(unlocked);
    }

    private void AttemptToExit()
    {
        if (!requiresRelic || (GameSceneManager.Instance != null &&
            GameSceneManager.Instance.CollectedRelics[requiredRelicIndex]))
        {
            // Play activation effect
            if (activationEffect != null)
            {
                Instantiate(activationEffect, transform.position, Quaternion.identity);
            }

            // Show level complete UI or transition effect
            ShowLevelCompleteUI();

            // Load next level after delay
            Invoke("LoadNextLevel", 2f);
        }
        else
        {
            // Show locked message
            Debug.Log(lockedMessage);
            // You can implement UI message here
        }
    }

    private void ShowLevelCompleteUI()
    {
        // Implement level complete UI
        Debug.Log("Level complete!");
    }

    private void LoadNextLevel()
    {
        if (GameSceneManager.Instance != null)
        {
            GameSceneManager.Instance.LoadNextLevel();
        }
        else
        {
            // Fallback if scene manager not found
            int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
            SceneManager.LoadScene(currentSceneIndex + 1);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Exit trigger entered by: " + other.gameObject.name);

        if (other.CompareTag("Player"))
        {
            Debug.Log("Player entered exit. Checking for relic...");

            if (GameSceneManager.Instance == null)
            {
                Debug.LogError("GameSceneManager.Instance is null!");
                return;
            }

            bool hasRelic = GameSceneManager.Instance.CollectedRelics[requiredRelicIndex];
            Debug.Log("Player has required relic: " + hasRelic);

            if (hasRelic)
            {
                Debug.Log("Loading next level...");
                GameSceneManager.Instance.LoadNextLevel();
            }
            else
            {
                Debug.Log(lockedMessage);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
        }
    }
}