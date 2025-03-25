using UnityEngine;
using Unity.Cinemachine;

public class CameraManager : MonoBehaviour
{
    // Singleton pattern for easy access
    public static CameraManager Instance { get; private set; }

    [Header("Camera References")]
    [SerializeField] private CinemachineCamera virtualCamera;
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
            virtualCamera = GetComponentInChildren<CinemachineCamera>();
        }

        // Initialize with default settings if we have a camera
        if (virtualCamera != null)
        {
            SetDefaultCamera();
        }
        else
        {
            Debug.LogError("No CinemachineCamera found on CameraManager. Please assign one in the inspector.");
        }
    }

    private void Start()
    {
        // Initial setup based on current scene
        if (string.IsNullOrEmpty(currentScene))
        {
            currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            SetupForScene(currentScene);
        }
    }

    /// <summary>
    /// Setup camera for current scene
    /// </summary>
    /// <param name="sceneName">Current scene name</param>
    public void SetupForScene(string sceneName)
    {
        if (virtualCamera == null)
        {
            Debug.LogError("Cannot setup camera for scene: virtualCamera is null");
            return;
        }

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
        if (target == null)
        {
            Debug.LogWarning("Attempted to set null target for camera");
            return;
        }

        if (virtualCamera == null)
        {
            Debug.LogError("Cannot set target: virtualCamera is null");
            return;
        }

        currentTarget = target;
        virtualCamera.Follow = target;

        // If it's a gameplay scene, also set the camera to look at the target
        if (IsGameplayScene())
        {
            virtualCamera.LookAt = target;
        }
    }

    private void SetupMainMenuCamera()
    {
        virtualCamera.Follow = defaultTarget;
        virtualCamera.LookAt = null;
    }

    private void SetupCharacterSelectionCamera()
    {
        virtualCamera.Follow = defaultTarget;
        virtualCamera.LookAt = defaultTarget;
    }

    private void SetupGameplayCamera(Vector2 bounds)
    {
        // Find the player if we haven't set a current target
        if (currentTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                currentTarget = player.transform;
            }
            else
            {
                Debug.LogWarning("No player found for gameplay camera setup");
                currentTarget = defaultTarget;
            }
        }

        // Set target to follow
        virtualCamera.Follow = currentTarget;
        virtualCamera.LookAt = currentTarget;

        // Set camera bounds based on level
        var confiner = virtualCamera.GetComponent<CinemachineConfiner2D>();
        if (confiner != null)
        {
            // Create bounds or use existing bounds
            CreateOrUpdateBoundaryForScene(confiner, bounds);
        }
    }

    private void CreateOrUpdateBoundaryForScene(CinemachineConfiner2D confiner, Vector2 bounds)
    {
        // This method would create a polygon collider or other boundary shape
        // based on the current scene's needs

        // Example code to create a simple rectangular boundary:
        GameObject boundaryObj = GameObject.Find("CameraBoundary");
        if (boundaryObj == null)
        {
            boundaryObj = new GameObject("CameraBoundary");
            boundaryObj.layer = LayerMask.NameToLayer("CameraBounds"); // Make sure this layer exists
        }

        PolygonCollider2D boundary = boundaryObj.GetComponent<PolygonCollider2D>();
        if (boundary == null)
        {
            boundary = boundaryObj.AddComponent<PolygonCollider2D>();
        }

        // Create a simple rectangle based on the bounds
        Vector2[] points = new Vector2[4];
        points[0] = new Vector2(-bounds.x / 2, -bounds.y / 2);  // Bottom-left
        points[1] = new Vector2(bounds.x / 2, -bounds.y / 2);   // Bottom-right
        points[2] = new Vector2(bounds.x / 2, bounds.y / 2);    // Top-right
        points[3] = new Vector2(-bounds.x / 2, bounds.y / 2);   // Top-left

        boundary.points = points;

        // Assign the boundary to the confiner
        confiner.BoundingShape2D = boundary;
    }

    private void SetupEndScreenCamera()
    {
        virtualCamera.Follow = defaultTarget;
        virtualCamera.LookAt = defaultTarget;
    }

    private void SetDefaultCamera()
    {
        virtualCamera.Follow = defaultTarget ?? transform;
        virtualCamera.LookAt = null;
    }

    private bool IsGameplayScene()
    {
        return currentScene == "GlacierBiome" ||
               currentScene == "VolcanoBiome" ||
               currentScene == "ForestBiome" ||
               currentScene == "DungeonBiome" ||
               currentScene == "AbyssalGate";
    }

    // Method to handle scene transitions
    public void OnSceneLoaded(string sceneName)
    {
        SetupForScene(sceneName);
    }

    // This can be called from a game manager when the player is instantiated
    public void FindAndFollowPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            SetTarget(player.transform);
        }
        else
        {
            Debug.LogWarning("No player found to follow");
        }
    }

    // Method to update camera bounds
    public void UpdateCameraBounds(Collider2D bounds)
    {
        var confiner = virtualCamera.GetComponent<CinemachineConfiner2D>();
        if (confiner != null)
        {
            confiner.BoundingShape2D = bounds;
        }
        else
        {
            Debug.LogWarning("No CinemachineConfiner2D found on the virtual camera.");
        }
    }
}
