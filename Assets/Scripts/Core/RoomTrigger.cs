// RoomTrigger.cs - Place on colliders to trigger room transitions
using UnityEngine;

public class RoomTrigger : MonoBehaviour
{
    [Header("Transition Settings")]
    public string targetRoomName;
    public bool requiresButtonPress = false;
    public KeyCode transitionKey = KeyCode.E;

    [Header("UI Prompt")]
    public GameObject interactPrompt;

    private bool playerInTrigger = false;

    private void Start()
    {
        // Hide interaction prompt initially
        if (interactPrompt != null)
        {
            interactPrompt.SetActive(false);
        }
    }

    private void Update()
    {
        // Check for interaction if player is in trigger zone and button press is required
        if (playerInTrigger && requiresButtonPress)
        {
            if (Input.GetKeyDown(transitionKey))
            {
                TriggerRoomTransition();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInTrigger = true;

            // Show interaction prompt if needed
            if (requiresButtonPress && interactPrompt != null)
            {
                interactPrompt.SetActive(true);
            }

            // Auto-transition if no button press required
            if (!requiresButtonPress)
            {
                TriggerRoomTransition();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInTrigger = false;

            // Hide interaction prompt
            if (interactPrompt != null)
            {
                interactPrompt.SetActive(false);
            }
        }
    }

    private void TriggerRoomTransition()
    {
        // Transition to the target room
        if (RoomManager.Instance != null)
        {
            RoomManager.Instance.TransitionToRoom(targetRoomName);
        }
        else
        {
            Debug.LogError("RoomManager instance not found!");
        }
    }
}