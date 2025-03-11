using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void StartGame()
    {
        SceneManager.LoadScene("VolcanoBiome"); // Change "GameScene" to your actual game scene name
    }

    public void OpenSettings()
    {
        Debug.Log("Open Settings Menu"); // Later, replace this with a settings UI.
    }

    public void QuitGame()
    {
        Debug.Log("Game Quit");
        Application.Quit(); // Won't work in the Unity Editor, only in a built application.
    }
}
