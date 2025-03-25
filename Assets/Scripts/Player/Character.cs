using UnityEngine;

[System.Serializable]
public class Character
{
    public string characterName;
    public Sprite characterSprite; // Main icon/portrait
    public RuntimeAnimatorController animatorController;

    // Animation sprites for different states
    public Sprite[] idleSprites;
    public Sprite[] walkSprites;
    public Sprite[] shootSprites;
    public Sprite[] dieSprites;

    // Add gameplay stats
    [Header("Gameplay Stats")]
    public int health = 100;
    public float moveSpeed = 5f;
    public float damageMultiplier = 1f;

    // Optional: preview animations in the selection screen
    public bool isSelected = false;
}