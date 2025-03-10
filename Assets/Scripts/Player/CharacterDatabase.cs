using UnityEngine;

[CreateAssetMenu(fileName = "New Character Database", menuName = "Character/Database")]
public class CharacterDatabase : ScriptableObject
{
    public Character[] characters;

    public int characterCount
    {
        get
        {
            return characters.Length;
        }
    }

    public Character GetCharacter(int index)
    {
        return characters[index];
    }

}
