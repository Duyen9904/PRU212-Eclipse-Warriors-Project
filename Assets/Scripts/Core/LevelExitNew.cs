using UnityEngine;

public class LevelExitNew : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Người chơi vào cổng! Chuyển màn...");
            EnemyManager.instance.LoadNextLevel();
        }
    }
}
