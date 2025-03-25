using UnityEngine;

public class Relic : MonoBehaviour, IInteractable
{
    [Header("Relic Properties")]
    public string relicName;
    public string relicDescription;
    public int relicIndex; // 0=Glacier, 1=Volcano, 2=Forest, 3=Dungeon

    [Header("Visual")]
    public GameObject collectEffect;
    public float rotationSpeed = 50f;

    private void Update()
    {
        // Simple rotation animation
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
    }

    // Implement IInteractable interface
    public void Interact(PlayerController player)
    {
        CollectRelic(player);
    }

    // This method is called when collecting the relic via trigger or interaction
    private void CollectRelic(PlayerController player)
    {
        // Check if player is null before using it
        if (player == null)
        {
            Debug.LogError("Player controller is null in CollectRelic method");

            // Try to find player if it's null
            player = FindFirstObjectByType<PlayerController>();

            // If still null, just mark the relic as collected without player reference
            if (player == null)
            {
                // Handle collection without player reference
                if (GameSceneManager.Instance != null)
                {
                    GameSceneManager.Instance.CollectedRelics[relicIndex] = true;
                    Debug.Log($"Relic {relicIndex} collected (without player reference)!");
                }

                // Play effect and destroy relic
                if (collectEffect != null)
                {
                    Instantiate(collectEffect, transform.position, Quaternion.identity);
                }

                Destroy(gameObject);
                return;
            }
        }

        // The rest of your collection code here
        if (GameSceneManager.Instance != null)
        {
            GameSceneManager.Instance.CollectedRelics[relicIndex] = true;
        }

        // Play collection effect
        if (collectEffect != null)
        {
            Instantiate(collectEffect, transform.position, Quaternion.identity);
        }

        // Play sound if audio manager exists
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound("relic_collect");
        }

        // Destroy the relic object
        Destroy(gameObject);
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Auto-collect when player touches the relic (alternative to interaction)
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                CollectRelic(player);
            }
        }
    }

    private void ShowRelicCollectedUI()
    {
        // Implement relic collected notification UI
        // This could show the relic's name and powers
        Debug.Log("Collected relic: " + relicName);
    }
}