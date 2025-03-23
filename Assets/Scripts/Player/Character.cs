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

    // Optional: preview animations in the selection screen
    public bool isSelected = false;
}