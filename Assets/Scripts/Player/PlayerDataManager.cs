using UnityEngine;
using System.Collections.Generic;

public class PlayerDataManager : MonoBehaviour
{
    // Keys for PlayerPrefs
    private const string SELECTED_CHARACTER_KEY = "SelectedCharacter";
    private const string UNLOCKED_BIOMES_KEY = "UnlockedBiomes";
    private const string PLAYER_HEALTH_KEY = "PlayerHealth";
    private const string PLAYER_MANA_KEY = "PlayerMana";
    private const string COLLECTED_RELICS_KEY = "CollectedRelics";

    // Character stats cache
    private Dictionary<string, CharacterStats> characterStats = new Dictionary<string, CharacterStats>();

    // Relics collection
    private List<string> collectedRelics = new List<string>();

    private void Awake()
    {
        // Initialize character stats
        InitializeCharacterStats();

        // Load collected relics
        LoadCollectedRelics();
    }

    /// <summary>
    /// Initialize base stats for all characters
    /// </summary>
    private void InitializeCharacterStats()
    {
        // Wizard stats
        CharacterStats wizardStats = new CharacterStats
        {
            MaxHealth = 80,
            MaxMana = 150,
            BaseDamage = 50,
            MoveSpeed = 5f,
            JumpForce = 10f,
            HasRangedAttack = true,
            HasMeleeAttack = false
        };

        // Rogue stats
        CharacterStats rogueStats = new CharacterStats
        {
            MaxHealth = 100,
            MaxMana = 70,
            BaseDamage = 40,
            MoveSpeed = 8f,
            JumpForce = 12f,
            HasRangedAttack = false,
            HasMeleeAttack = true
        };

        // Knight stats
        CharacterStats knightStats = new CharacterStats
        {
            MaxHealth = 150,
            MaxMana = 60,
            BaseDamage = 35,
            MoveSpeed = 4f,
            JumpForce = 9f,
            HasRangedAttack = false,
            HasMeleeAttack = true
        };

        // Add to dictionary
        characterStats.Add("Wizard", wizardStats);
        characterStats.Add("Rogue", rogueStats);
        characterStats.Add("Knight", knightStats);
    }

    /// <summary>
    /// Save the selected character
    /// </summary>
    public void SaveSelectedCharacter(string characterType)
    {
        PlayerPrefs.SetString(SELECTED_CHARACTER_KEY, characterType);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Load the selected character
    /// </summary>
    public string LoadSelectedCharacter()
    {
        return PlayerPrefs.GetString(SELECTED_CHARACTER_KEY, "Knight");
    }

    /// <summary>
    /// Get stats for the specified character type
    /// </summary>
    public CharacterStats GetCharacterStats(string characterType)
    {
        if (characterStats.TryGetValue(characterType, out CharacterStats stats))
        {
            return stats;
        }

        // Default to Knight if character type not found
        return characterStats["Knight"];
    }

    /// <summary>
    /// Save the current health and mana values
    /// </summary>
    public void SavePlayerStatus(float currentHealth, float currentMana)
    {
        PlayerPrefs.SetFloat(PLAYER_HEALTH_KEY, currentHealth);
        PlayerPrefs.SetFloat(PLAYER_MANA_KEY, currentMana);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Load health and mana values
    /// </summary>
    public (float health, float mana) LoadPlayerStatus(string characterType)
    {
        // Get character stats
        CharacterStats stats = GetCharacterStats(characterType);

        // Load saved values or use max values if not found
        float health = PlayerPrefs.GetFloat(PLAYER_HEALTH_KEY, stats.MaxHealth);
        float mana = PlayerPrefs.GetFloat(PLAYER_MANA_KEY, stats.MaxMana);

        return (health, mana);
    }

    /// <summary>
    /// Mark a biome as unlocked
    /// </summary>
    public void UnlockBiome(int biomeIndex)
    {
        // Get currently unlocked biomes
        int unlockedBiomes = PlayerPrefs.GetInt(UNLOCKED_BIOMES_KEY, 0);

        // Ensure we only unlock sequentially (can't skip biomes)
        if (biomeIndex <= unlockedBiomes + 1)
        {
            PlayerPrefs.SetInt(UNLOCKED_BIOMES_KEY, biomeIndex);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Check if a biome is unlocked
    /// </summary>
    public bool IsBiomeUnlocked(int biomeIndex)
    {
        int unlockedBiomes = PlayerPrefs.GetInt(UNLOCKED_BIOMES_KEY, 0);
        return biomeIndex <= unlockedBiomes;
    }

    /// <summary>
    /// Add a collected relic
    /// </summary>
    public void AddRelic(string relicName)
    {
        if (!collectedRelics.Contains(relicName))
        {
            collectedRelics.Add(relicName);
            SaveCollectedRelics();
        }
    }

    /// <summary>
    /// Check if player has collected a specific relic
    /// </summary>
    public bool HasRelic(string relicName)
    {
        return collectedRelics.Contains(relicName);
    }

    /// <summary>
    /// Get all collected relics
    /// </summary>
    public List<string> GetCollectedRelics()
    {
        return new List<string>(collectedRelics);
    }

    /// <summary>
    /// Save collected relics to PlayerPrefs
    /// </summary>
    private void SaveCollectedRelics()
    {
        string relicsData = string.Join(",", collectedRelics);
        PlayerPrefs.SetString(COLLECTED_RELICS_KEY, relicsData);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Load collected relics from PlayerPrefs
    /// </summary>
    private void LoadCollectedRelics()
    {
        string relicsData = PlayerPrefs.GetString(COLLECTED_RELICS_KEY, "");

        if (!string.IsNullOrEmpty(relicsData))
        {
            string[] relics = relicsData.Split(',');
            collectedRelics = new List<string>(relics);
        }
        else
        {
            collectedRelics = new List<string>();
        }
    }

    /// <summary>
    /// Reset all player data
    /// </summary>
    public void ResetAllData()
    {
        PlayerPrefs.DeleteKey(SELECTED_CHARACTER_KEY);
        PlayerPrefs.DeleteKey(UNLOCKED_BIOMES_KEY);
        PlayerPrefs.DeleteKey(PLAYER_HEALTH_KEY);
        PlayerPrefs.DeleteKey(PLAYER_MANA_KEY);
        PlayerPrefs.DeleteKey(COLLECTED_RELICS_KEY);
        PlayerPrefs.Save();

        collectedRelics.Clear();
    }
}

/// <summary>
/// Character stats structure
/// </summary>
[System.Serializable]
public class CharacterStats
{
    public float MaxHealth;
    public float MaxMana;
    public float BaseDamage;
    public float MoveSpeed;
    public float JumpForce;
    public bool HasRangedAttack;
    public bool HasMeleeAttack;
}