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
        Invoke("ApplySelectedCharacter", 0.1f);
    }

    void ApplySelectedCharacter()
    {
        // Get selected character ID from PlayerPrefs
        int selectedCharacterId = PlayerPrefs.GetInt("SelectedCharacter", 0);
        ApplyCharacterData(selectedCharacterId);
    }

    public void ApplyCharacterData(int characterId)
    {
        // Get character settings from PlayerPrefs

        // Apply health settings
        if (playerHealth != null)
        {
            // Note: You'll need to modify PlayerHealth to expose maxHealth as settable
            // or add a SetMaxHealth method
            int healthValue = PlayerPrefs.GetInt($"Character_{characterId}_Health", 30);
            // Either call a new method on PlayerHealth or modify it later
        }

        // Apply movement settings
        if (playerController != null)
        {
            float moveSpeed = PlayerPrefs.GetFloat($"Character_{characterId}_Speed", 5f);
            // Assuming your PlayerController has a moveSpeed property
            // playerController.moveSpeed = moveSpeed;
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

        // Apply animation controller
        if (animator != null)
        {
            string animControllerPath = PlayerPrefs.GetString($"Character_{characterId}_AnimatorPath", "");
            if (!string.IsNullOrEmpty(animControllerPath))
            {
                RuntimeAnimatorController controller = Resources.Load<RuntimeAnimatorController>(animControllerPath);
                if (controller != null)
                    animator.runtimeAnimatorController = controller;
            }
        }
    }
}