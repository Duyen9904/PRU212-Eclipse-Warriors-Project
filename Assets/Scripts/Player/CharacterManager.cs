using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class CharacterManager : MonoBehaviour
{
    public CharacterDatabase characterDatabase;
    public Text nameText;
    public SpriteRenderer artworkSprite;

    [Header("Animation Preview")]
    public bool showAnimationPreview = true;
    public float animationPreviewSpeed = 10f;
    public Toggle idleToggle;
    public Toggle walkToggle;
    public Toggle shootToggle;

    private int selectedOption = 0;
    private AnimationState currentPreviewState = AnimationState.Idle;
    private float animationTimer = 0f;
    private int currentFrameIndex = 0;
    public string nextSceneName = "GlacierBiome";
    private enum AnimationState
    {
        Idle,
        Walk,
        Shoot
    }

    void Start()
    {
        if (!PlayerPrefs.HasKey("SelectedOption"))
        {
            selectedOption = 0;
        }
        else
        {
            Load();
        }

        UpdateCharacter(selectedOption);

        // Set up toggle listeners if assigned
        if (idleToggle != null)
            idleToggle.onValueChanged.AddListener((isOn) => { if (isOn) SetPreviewState(AnimationState.Idle); });

        if (walkToggle != null)
            walkToggle.onValueChanged.AddListener((isOn) => { if (isOn) SetPreviewState(AnimationState.Walk); });

        if (shootToggle != null)
            shootToggle.onValueChanged.AddListener((isOn) => { if (isOn) SetPreviewState(AnimationState.Shoot); });
    }

    void Update()
    {
        if (showAnimationPreview)
        {
            UpdateAnimationPreview();
        }
    }

    public void NextOption()
    {
        selectedOption++;
        if (selectedOption >= characterDatabase.characterCount)
        {
            selectedOption = 0;
        }
        UpdateCharacter(selectedOption);
        Save();
    }

    public void PreviousOption()
    {
        selectedOption--;
        if (selectedOption < 0)
        {
            selectedOption = characterDatabase.characterCount - 1;
        }
        UpdateCharacter(selectedOption);
        Save();
    }

    private void UpdateCharacter(int selectedOption)
    {
        Character character = characterDatabase.GetCharacter(selectedOption);

        // Update text
        nameText.text = character.characterName;

        // Reset animation preview
        ResetAnimationPreview();

        // If not showing animation, just set the static sprite
        if (!showAnimationPreview)
        {
            artworkSprite.sprite = character.characterSprite;
        }
    }

    private void SetPreviewState(AnimationState state)
    {
        currentPreviewState = state;
        ResetAnimationPreview();
    }

    private void ResetAnimationPreview()
    {
        animationTimer = 0f;
        currentFrameIndex = 0;

        // Set initial frame of animation
        UpdatePreviewSprite();
    }

    private void UpdateAnimationPreview()
    {
        Character character = characterDatabase.GetCharacter(selectedOption);
        Sprite[] currentAnimation = GetCurrentAnimationSprites(character);

        // If no sprites to animate, just show the default sprite
        if (currentAnimation == null || currentAnimation.Length == 0)
        {
            artworkSprite.sprite = character.characterSprite;
            return;
        }

        // Update animation timer
        animationTimer += Time.deltaTime * animationPreviewSpeed;
        if (animationTimer >= 1f)
        {
            animationTimer = 0f;
            currentFrameIndex = (currentFrameIndex + 1) % currentAnimation.Length;
            UpdatePreviewSprite();
        }
    }

    private void UpdatePreviewSprite()
    {
        Character character = characterDatabase.GetCharacter(selectedOption);
        Sprite[] currentAnimation = GetCurrentAnimationSprites(character);

        if (currentAnimation != null && currentAnimation.Length > 0)
        {
            artworkSprite.sprite = currentAnimation[currentFrameIndex];
        }
        else
        {
            artworkSprite.sprite = character.characterSprite;
        }
    }

    private Sprite[] GetCurrentAnimationSprites(Character character)
    {
        switch (currentPreviewState)
        {
            case AnimationState.Idle:
                return character.idleSprites;
            case AnimationState.Walk:
                return character.walkSprites;
            case AnimationState.Shoot:
                return character.shootSprites;
            default:
                return character.idleSprites;
        }
    }

    private void Load()
    {
        selectedOption = PlayerPrefs.GetInt("SelectedOption");
    }

    private void Save()
    {
        // Save the selected character index
        PlayerPrefs.SetInt("SelectedOption", selectedOption);
        PlayerPrefs.SetInt("SelectedCharacter", selectedOption);

        // Get the selected character from the database
        Character character = characterDatabase.GetCharacter(selectedOption);

        // Save character-specific stats
        PlayerPrefs.SetInt($"Character_{selectedOption}_Health", character.health);
        PlayerPrefs.SetFloat($"Character_{selectedOption}_Speed", character.moveSpeed);
        PlayerPrefs.SetFloat($"Character_{selectedOption}_DamageMultiplier", character.damageMultiplier);

        // Save the changes
        PlayerPrefs.Save();
    }
    public void ChangeScene(string sceneName)
    {
        // Optional: Add debug logging to verify the method is called
        Debug.Log("Loading scene: " + sceneName);

        // Save character selection before changing scene
        Save();

        // Load the scene by name
        SceneManager.LoadScene(nextSceneName);
    }
    // Preview specific animations through UI buttons
    public void PreviewIdle()
    {
        SetPreviewState(AnimationState.Idle);
        if (idleToggle != null) idleToggle.isOn = true;
    }

    public void PreviewWalk()
    {
        SetPreviewState(AnimationState.Walk);
        if (walkToggle != null) walkToggle.isOn = true;
    }

    public void PreviewShoot()
    {
        SetPreviewState(AnimationState.Shoot);
        if (shootToggle != null) shootToggle.isOn = true;
    }
}   