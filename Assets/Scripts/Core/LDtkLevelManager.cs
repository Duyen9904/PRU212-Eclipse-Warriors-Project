using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;
using LDtkUnity;
public class LDtkLevelManager : MonoBehaviour
{
    [Header("LDtk References")]
    public LDtkProjectFile ldtkProject;
    public string startingLevelName = "Level_1";

    [Header("Player References")]
    public GameObject playerPrefab;
    public string playerEntityName = "Player";

    [Header("Camera References")]
    public CameraManager cameraManager;

    private GameObject currentLevelGameObject;
    private LDtkComponentLevel currentLevel;
    private GameObject playerInstance;

    private void Start()
    {
        LoadLevel(startingLevelName);
    }

    // Load a specific level from the LDtk project
    public void LoadLevel(string levelName)
    {
        // Clean up previous level if needed
        if (currentLevelGameObject != null)
        {
            Destroy(currentLevelGameObject);
        }

        // Load the level prefab
        string prefabPath = $"Assets/LDtk/Prefabs/Levels/{levelName}.prefab";
        GameObject levelPrefab = Resources.Load<GameObject>(prefabPath);

        if (levelPrefab != null)
        {
            currentLevelGameObject = Instantiate(levelPrefab);
            currentLevel = currentLevelGameObject.GetComponent<LDtkComponentLevel>();

            // Setup camera bounds and spawn player
            SetupCameraBounds();
            SpawnPlayer();
        }
        else
        {
            Debug.LogError($"Level prefab {levelName} not found at path {prefabPath}!");
        }
    }

    // Setup camera bounds based on the level size
    private void SetupCameraBounds()
    {
        if (cameraManager != null && currentLevelGameObject != null && currentLevel != null)
        {
            CompositeCollider2D levelBounds = currentLevelGameObject.GetComponentInChildren<CompositeCollider2D>();

            if (levelBounds == null)
            {
                GameObject boundsObject = new GameObject("CameraBounds");
                boundsObject.transform.SetParent(currentLevelGameObject.transform);

                PolygonCollider2D polyCollider = boundsObject.AddComponent<PolygonCollider2D>();

                Vector2 levelSize = currentLevel.Size;
                float ppu = 100f; // Assuming 100 pixels per unit as a default value

                Vector2[] points = new Vector2[4];
                points[0] = new Vector2(0, 0);
                points[1] = new Vector2(levelSize.x / ppu, 0);
                points[2] = new Vector2(levelSize.x / ppu, levelSize.y / ppu);
                points[3] = new Vector2(0, levelSize.y / ppu);

                polyCollider.points = points;
                polyCollider.isTrigger = true;

                levelBounds = boundsObject.AddComponent<CompositeCollider2D>();
                polyCollider.usedByComposite = true;
            }

            cameraManager.UpdateCameraBounds(levelBounds);
        }
    }

    // Spawn the player at the designated entity position
    private void SpawnPlayer()
    {
        if (playerPrefab != null && currentLevel != null)
        {
            LDtkEntityDrawerComponent playerEntity = FindPlayerEntity();

            if (playerEntity != null)
            {
                Vector2 playerPosition = playerEntity.transform.position;
                playerInstance = Instantiate(playerPrefab, playerPosition, Quaternion.identity);

                if (cameraManager != null)
                {
                    cameraManager.SetCameraTarget(playerInstance.transform);
                }
            }
            else
            {
                Vector2 fallbackPosition = new Vector2(
                    currentLevel.Size.x / 2,
                    currentLevel.Size.y / 2
                );

                Debug.LogWarning($"No player entity found in level. Using fallback position: {fallbackPosition}");

                playerInstance = Instantiate(playerPrefab, fallbackPosition, Quaternion.identity);

                if (cameraManager != null)
                {
                    cameraManager.SetCameraTarget(playerInstance.transform);
                }
            }
        }
    }

    // Find player entity in the level
    private LDtkEntityDrawerComponent FindPlayerEntity()
    {
        LDtkEntityDrawerComponent[] entities = currentLevelGameObject.GetComponentsInChildren<LDtkEntityDrawerComponent>();

        foreach (var entity in entities)
        {
            if (entity.name == playerEntityName) // Assuming 'name' is the correct property to check
            {
                return entity;
            }
        }

        return null;
    }

    // Get available level names from the LDtk project
    public string[] GetAvailableLevels()
    {
        if (ldtkProject != null && ldtkProject.FromJson != null && ldtkProject.FromJson.Levels != null)
        {
            List<string> levelNames = new List<string>();

            foreach (var level in ldtkProject.FromJson.Levels)
            {
                levelNames.Add(level.Identifier);
            }

            return levelNames.ToArray();
        }

        return new string[0];
    }
}
