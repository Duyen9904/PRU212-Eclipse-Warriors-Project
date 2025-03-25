using UnityEngine;

public class CharacterCustomizer : MonoBehaviour
{
    [Header("Character Visual")]
    public SpriteRenderer characterSprite;

    [Header("Animation")]
    public Animator animator;

    [Header("Components")]
    private PlayerController playerController;
    private PlayerHealth playerHealth;

    private void Awake()
    {
        // Get component references
        playerController = GetComponent<PlayerController>();
        playerHealth = GetComponent<PlayerHealth>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (characterSprite == null)
            characterSprite = GetComponent<SpriteRenderer>();

        // Apply character data after a short delay
        ApplySelectedCharacter();
    }

    void ApplySelectedCharacter()
    {
        // Get selected character ID from PlayerPrefs
        int selectedCharacterId = PlayerPrefs.GetInt("SelectedCharacter", 0);
        Debug.Log($"Applying character data for ID: {selectedCharacterId}");
        ApplyCharacterData(selectedCharacterId);
    }

    public void ApplyCharacterData(int characterId)
    {
        // Apply health settings
        if (playerHealth != null)
        {
            int healthValue = PlayerPrefs.GetInt($"Character_{characterId}_Health", 100);
            playerHealth.SetMaxHealth(healthValue);
            Debug.Log($"Set health to {healthValue}");
        }

        // Apply movement settings
        if (playerController != null)
        {
            float moveSpeed = PlayerPrefs.GetFloat($"Character_{characterId}_Speed", 5f);
            playerController.SetMoveSpeed(moveSpeed);
            Debug.Log($"Set move speed to {moveSpeed}");
        }

        // Apply visual changes
        if (characterSprite != null)
        {
            string spritePath = PlayerPrefs.GetString($"Character_{characterId}_SpritePath", "");
            if (!string.IsNullOrEmpty(spritePath))
            {
                Sprite selectedSprite = Resources.Load<Sprite>(spritePath);
                if (selectedSprite != null)
                    characterSprite.sprite = selectedSprite;
            }
        }

        // Apply animation controller - FIXED
        if (animator != null)
        {
            string animControllerName = PlayerPrefs.GetString($"Character_{characterId}_AnimController", "");
            if (!string.IsNullOrEmpty(animControllerName))
            {
                // Find the animator controller in Resources
                RuntimeAnimatorController controller = Resources.Load<RuntimeAnimatorController>($"Animations/{animControllerName}");
                if (controller != null)
                {
                    animator.runtimeAnimatorController = controller;
                    Debug.Log($"Applied animator controller: {animControllerName}");
                }
                else
                {
                    // Try to find it in any folder
                    RuntimeAnimatorController[] controllers = Resources.FindObjectsOfTypeAll<RuntimeAnimatorController>();
                    foreach (var foundController in controllers)
                    {
                        if (foundController.name == animControllerName)
                        {
                            animator.runtimeAnimatorController = foundController;
                            Debug.Log($"Found and applied animator controller: {animControllerName}");
                            break;
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning($"No animator controller saved for character ID {characterId}");
            }
        }
    }
}