// ======================================================
// SCENE MANAGEMENT INTEGRATION
// ======================================================

/*
Here we're providing integration with Addressables, which is Unity's recommended
approach for scene management. The example below is for a game that requires 
loading different maps/scenes dynamically.
*/

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using System.Collections.Generic;

// SceneController: Manages scene and map loading
public class SceneController : MonoBehaviour
{
    // Singleton instance
    public static SceneController Instance;

    [Header("Scene References")]
    public string mainMenuSceneKey = "MainMenu";
    public string gameUISceneKey = "GameUI";

    [Header("Loading Screen")]
    public GameObject loadingScreen;
    public UnityEngine.UI.Slider loadingBar;

    // Keep track of loaded scenes
    private Dictionary<string, SceneInstance> loadedScenes = new Dictionary<string, SceneInstance>();
    private string currentMapKey;

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

    // Load a new map and unload the current one
    public async void LoadMap(string mapKey)
    {
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(true);
        }

        // Unload current map if one is loaded
        if (!string.IsNullOrEmpty(currentMapKey) && loadedScenes.ContainsKey(currentMapKey))
        {
            await Addressables.UnloadSceneAsync(loadedScenes[currentMapKey]).Task;
            loadedScenes.Remove(currentMapKey);
        }

        // Load the new map
        AsyncOperationHandle<SceneInstance> loadOperation = Addressables.LoadSceneAsync(mapKey, LoadSceneMode.Additive);

        // Update loading progress
        if (loadingBar != null)
        {
            while (!loadOperation.IsDone)
            {
                loadingBar.value = loadOperation.PercentComplete;
                await System.Threading.Tasks.Task.Delay(10);
            }
            loadingBar.value = 1f;
        }

        // Store the loaded scene
        loadedScenes[mapKey] = loadOperation.Result;
        currentMapKey = mapKey;

        // Make sure the GameUI scene is loaded
        if (!loadedScenes.ContainsKey(gameUISceneKey))
        {
            AsyncOperationHandle<SceneInstance> uiLoadOperation = Addressables.LoadSceneAsync(gameUISceneKey, LoadSceneMode.Additive);
            loadedScenes[gameUISceneKey] = await uiLoadOperation.Task;
        }

        if (loadingScreen != null)
        {
            loadingScreen.SetActive(false);
        }
    }

    // Load the main menu
    public async void LoadMainMenu()
    {
        // Unload all loaded scenes
        foreach (var scene in loadedScenes)
        {
            await Addressables.UnloadSceneAsync(scene.Value).Task;
        }
        loadedScenes.Clear();
        currentMapKey = null;

        // Load main menu scene
        AsyncOperationHandle<SceneInstance> loadOperation = Addressables.LoadSceneAsync(mainMenuSceneKey, LoadSceneMode.Single);
        await loadOperation.Task;
    }

    // Get the current map key
    public string GetCurrentMapKey()
    {
        return currentMapKey;
    }
}

// ======================================================
// MAP MANAGEMENT WITH LDtk INTEGRATION
// ======================================================

/*
This section demonstrates integration with LDtk, a modern level design tool
that's becoming increasingly popular for 2D games.
To use this, you need to import the LDtk Unity package from the Asset Store.
*/