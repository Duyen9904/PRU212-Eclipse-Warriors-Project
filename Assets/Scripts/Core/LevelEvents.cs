// LevelEvents.cs - Handle level-specific events
using UnityEngine;

public class LevelEvents : MonoBehaviour
{
    [Header("Level Properties")]
    public string levelName;
    public string levelDescription;

    [Header("Level Events")]
    public UnityEngine.Events.UnityEvent onLevelStart;
    public UnityEngine.Events.UnityEvent onLevelComplete;

    [Header("Optional Delay")]
    public float initialDelay = 0.5f;

    public void OnLevelStart()
    {
        // Invoke the level start event with optional delay
        if (initialDelay > 0)
        {
            Invoke("InvokeLevelStart", initialDelay);
        }
        else
        {
            InvokeLevelStart();
        }
    }

    private void InvokeLevelStart()
    {
        onLevelStart?.Invoke();
    }

    public void CompleteLevel()
    {
        onLevelComplete?.Invoke();

        // Tell the level manager to load the next level
        LevelManager.Instance.LoadNextLevel();
    }
}