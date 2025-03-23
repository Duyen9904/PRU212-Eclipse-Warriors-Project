using UnityEngine;
using Cinemachine;

public class CameraManager : MonoBehaviour
{
    // Singleton pattern for easy access
    public static CameraManager Instance { get; private set; }

    [Header("Camera References")]
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    [SerializeField] private Transform defaultTarget;

    [Header("Camera Settings")]
    [SerializeField] private float defaultDistance = 10f;
    [SerializeField] private float characterSelectionDistance = 5f;
    [SerializeField] private float gameplayDistance = 8f;

    [Header("Environmental Settings")]
    [SerializeField] private Vector2 glacierBiomeBounds = new Vector2(100f, 30f);
    [SerializeField] private Vector2 volcanoBiomeBounds = new Vector2(100f, 30f);
    [SerializeField] private Vector2 forestBiomeBounds = new Vector2(100f, 30f);
    [SerializeField] private Vector2 dungeonBiomeBounds = new Vector2(100f, 30f);
    [SerializeField] private Vector2 abyssalGateBounds = new Vector2(100f, 30f);

    // Current scene reference
    private string currentScene;
    private Transform currentTarget;

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

        // Setup default camera
        if (virtualCamera == null)
        {
            virtualCamera = GetComponentInChildren<CinemachineVirtualCamera>();
        }
    }

    /// <summary>
    /// Setup camera for current scene
    /// </summary>
    /// <param name="sceneName">Current scene name</param>
    public void SetupForScene(string sceneName)
    {
        currentScene = sceneName;

        switch (sceneName)
        {
            case "MainMenu":
                SetupMainMenuCamera();
                break;
            case "CharacterSelection":
                SetupCharacterSelectionCamera();
                break;
            case "GlacierBiome":
                SetupGameplayCamera(glacierBiomeBounds);
                break;
            case "VolcanoBiome":
                SetupGameplayCamera(volcanoBiomeBounds);
                break;
            case "ForestBiome":
                SetupGameplayCamera(forestBiomeBounds);
                break;
            case "DungeonBiome":
                SetupGameplayCamera(dungeonBiomeBounds);
                break;
            case "AbyssalGate":
                SetupGameplayCamera(abyssalGateBounds);
                break;
            case "WinningGame":
            case "GameOver":
                SetupEndScreenCamera();
                break;
            default:
                SetDefaultCamera();
                break;
        }
    }

    /// <summary>
    /// Set camera to follow a specific target (player)
    /// </summary>
    /// <param name="target">Transform to follow</param>
    public void SetTarget(Transform target)
    {
        currentTarget = target;

        if (virtualCamera != null)
        {
            virtualCamera.Follow = target;

            // If it's a gameplay scene, also set the camera to look at the target
            if (IsGameplayScene())
            {
                virtualCamera.LookAt = target;
            }
        }
    }

    private void SetupMainMenuCamera()
    {
        virtualCamera.Follow = defaultTarget;
        virtualCamera.LookAt = null;

        // Adjust camera settings
        var composer = virtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
        if (composer != null)
        {
            composer.m_CameraDistance = defaultDistance;
        }
    }

    private void SetupCharacterSelectionCamera()
    {
        virtualCamera.Follow = defaultTarget;
        virtualCamera.LookAt = defaultTarget;

        // Adjust camera settings for character selection
        var composer = virtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
        if (composer != null)
        {
            composer.m_CameraDistance = characterSelectionDistance;
        }
    }

    private void SetupGameplayCamera(Vector2 bounds)
    {
        // Set camera bounds based on level
        var confiner = virtualCamera.GetComponent<CinemachineConfiner>();
        if (confiner != null)
        {
            // Create a temporary bounding box
            var boundingBox = new Bounds(Vector3.zero, new Vector3(bounds.x, bounds.y, 100f));

            // Here you would set the actual bounding volume - typically you'd use a polygon collider
            // For demonstration, we're just showing the concept
            Debug.Log($"Setting camera bounds for {currentScene}: {bounds}");
        }

        // Adjust camera settings for gameplay
        var composer = virtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
        if (composer != null)
        {
            composer.m_CameraDistance = gameplayDistance;
        }
    }

    private void SetupEndScreenCamera()
    {
        virtualCamera.Follow = defaultTarget;
        virtualCamera.LookAt = defaultTarget;

        // Adjust camera settings
        var composer = virtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
        if (composer != null)
        {
            composer.m_CameraDistance = defaultDistance;
        }
    }

    private void SetDefaultCamera()
    {
        virtualCamera.Follow = defaultTarget;
        virtualCamera.LookAt = null;

        // Adjust camera settings
        var composer = virtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
        if (composer != null)
        {
            composer.m_CameraDistance = defaultDistance;
        }
    }

    private bool IsGameplayScene()
    {
        return currentScene == "GlacierBiome" ||
               currentScene == "VolcanoBiome" ||
               currentScene == "ForestBiome" ||
               currentScene == "DungeonBiome" ||
               currentScene == "AbyssalGate";
    }
}