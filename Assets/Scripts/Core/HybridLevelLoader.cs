using UnityEngine;
using System.Collections.Generic;

// Only import LDTK package when used
#if LDTK_IMPORTER
using LDtkUnity;
#endif

public class HybridLevelLoader : MonoBehaviour
{
    [Header("Level Type")]
    [SerializeField] private bool usesLDTK = false;

    [Header("LDTK Settings (Only for Glacier Biome)")]
#if LDTK_IMPORTER
    [SerializeField] private LDtkComponentProject ldtkProject;
    [SerializeField] private TextAsset glacierLevelFile;
#endif

    [Header("Traditional Level Settings")]
    [SerializeField] private Transform standardSpawnPoint;
    [SerializeField] private GameObject levelEnvironment;

    [Header("Player Prefabs")]
    [SerializeField] private GameObject wizardPrefab;
    [SerializeField] private GameObject roguePrefab;
    [SerializeField] private GameObject knightPrefab;

    private GameObject currentLevelInstance;
    private GameObject playerInstance;

    private void Start()
    {
        // Load the appropriate level
        LoadLevel();
    }





    public void LoadLevel()
    {
        // Clean up any existing level/player
        CleanupExistingLevel();

        if (usesLDTK)
        {
            // Load LDTK level (Glacier Biome)
            LoadLDTKLevel();
        }
        else
        {
            // Load traditional Unity scene level
            LoadTraditionalLevel();
        }
    }

    private void CleanupExistingLevel()
    {
        // Clean up any existing level
        if (currentLevelInstance != null)
        {
            Destroy(currentLevelInstance);
        }

        // Clean up existing player
        if (playerInstance != null)
        {
            Destroy(playerInstance);
        }
    }

    private void LoadLDTKLevel()
    {
#if LDTK_IMPORTER
        if (ldtkProject != null && glacierLevelFile != null)
        {
            // Import the level from LDTK
            currentLevelInstance = ldtkProject.InstantiateLevel(glacierLevelFile.name);
            
            // Find player spawn point in LDTK level
            Transform spawnPoint = FindLDTKSpawnPoint(currentLevelInstance);
            
            // Spawn the selected character
            SpawnPlayer(spawnPoint);
        }
        else
        {
            Debug.LogError("Failed to load LDTK level. LDTK project or level file is missing.");
        }
#else
        Debug.LogError("Trying to load LDTK level but LDTK_IMPORTER is not defined. Make sure the LDtk Unity package is imported.");
#endif
    }

#if LDTK_IMPORTER
    private Transform FindLDTKSpawnPoint(GameObject levelInstance)
    {
        // Look for a designated spawn point entity in the LDTK level
        LDtkEntityInstance[] spawnPoints = levelInstance.GetComponentsInChildren<LDtkEntityInstance>();
        
        foreach (var entity in spawnPoints)
        {
            if (entity.EntityIdentifier == "PlayerSpawn" || entity.EntityIdentifier == "SpawnPoint")
            {
                return entity.transform;
            }
        }
        
        // Fallback: return level root position if no spawn point is found
        Debug.LogWarning("No player spawn point found in the LDTK level. Using level root position.");
        return levelInstance.transform;
    }
#endif

    private void LoadTraditionalLevel()
    {
        // For traditional levels, just activate the pre-configured environment
        if (levelEnvironment != null)
        {
            levelEnvironment.SetActive(true);
            currentLevelInstance = levelEnvironment;

            // Use the standard spawn point
            if (standardSpawnPoint != null)
            {
                SpawnPlayer(standardSpawnPoint);
            }
            else
            {
                Debug.LogError("No spawn point assigned for traditional level!");
            }
        }
        else
        {
            Debug.LogError("No level environment assigned for traditional level!");
        }
    }

    private void SpawnPlayer(Transform spawnPoint)
    {
        // Get selected character from scene manager
        GameSceneManager.CharacterType selectedCharacter = GameSceneManager.Instance.SelectedCharacter;

        // Determine which prefab to use
        GameObject prefabToSpawn = null;

        switch (selectedCharacter)
        {
            case GameSceneManager.CharacterType.Wizard:
                prefabToSpawn = wizardPrefab;
                break;
            case GameSceneManager.CharacterType.Rogue:
                prefabToSpawn = roguePrefab;
                break;
            case GameSceneManager.CharacterType.Knight:
                prefabToSpawn = knightPrefab;
                break;
        }

        // Instantiate player at spawn point
        if (prefabToSpawn != null && spawnPoint != null)
        {
            Vector3 spawnPosition = spawnPoint.position;
            playerInstance = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
        }
        else
        {
            Debug.LogError($"Failed to spawn player character. Prefab or spawn point is missing.");
        }
    }
}