using UnityEngine;

public class AttackEffectController : MonoBehaviour
{
    public GameObject slashUpEffect;
    public GameObject slashDownEffect;
    public GameObject slashLeftEffect;
    public GameObject slashRightEffect;

    // Referenced by DirectionalEnemyController during attacks
    public void ShowAttackEffect(Vector2 direction)
    {
        // Determine primary direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        GameObject effectToShow = null;
        Vector3 spawnOffset = Vector3.zero;

        // Select proper effect based on angle
        if (angle > -45 && angle <= 45) // Right
        {
            effectToShow = slashRightEffect;
            spawnOffset = new Vector3(0.5f, 0, 0);
        }
        else if (angle > 45 && angle <= 135) // Up
        {
            effectToShow = slashUpEffect;
            spawnOffset = new Vector3(0, 0.5f, 0);
        }
        else if ((angle > 135 && angle <= 180) || (angle <= -135 && angle >= -180)) // Left
        {
            effectToShow = slashLeftEffect;
            spawnOffset = new Vector3(-0.5f, 0, 0);
        }
        else // Down
        {
            effectToShow = slashDownEffect;
            spawnOffset = new Vector3(0, -0.5f, 0);
        }

        // Show the effect if available
        if (effectToShow != null)
        {
            GameObject effect = Instantiate(effectToShow, transform.position + spawnOffset, Quaternion.identity);
            Destroy(effect, 0.5f); // Automatically destroy after 0.5 seconds
        }
    }
}