using UnityEngine;
using System.Collections.Generic;

public class RoomManager : MonoBehaviour
{
    [Header("Room References")]
    public static RoomManager Instance;

    [System.Serializable]
    public class Room
    {
        public string roomName;
        public GameObject roomObject;
        public Transform playerSpawnPoint;
        public Collider2D roomBounds;
        [HideInInspector] public bool isActive = false;
    }

    [Header("Room Settings")]
    public List<Room> rooms = new List<Room>();
    public Room currentRoom;

    [Header("Camera Settings")]
    public CameraManager cameraManager;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Deactivate all rooms except the current one
        foreach (Room room in rooms)
        {
            if (room != currentRoom)
            {
                DeactivateRoom(room);
            }
            else
            {
                ActivateRoom(room);
            }
        }
    }

    // Transition to a different room
    public void TransitionToRoom(string roomName)
    {
        // Find the target room
        Room targetRoom = rooms.Find(r => r.roomName == roomName);

        if (targetRoom == null)
        {
            Debug.LogError("Room not found: " + roomName);
            return;
        }

        // Save the state of the current room if needed
        if (currentRoom != null)
        {
            DeactivateRoom(currentRoom);
        }

        // Activate the new room
        ActivateRoom(targetRoom);
        currentRoom = targetRoom;

        // Find the player
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null && targetRoom.playerSpawnPoint != null)
        {
            // Move player to the room's spawn point
            player.transform.position = targetRoom.playerSpawnPoint.position;
        }

        // Update camera bounds
        if (cameraManager != null && targetRoom.roomBounds != null)
        {
            cameraManager.UpdateCameraBounds(targetRoom.roomBounds);
        }
    }

    // Activate a room
    private void ActivateRoom(Room room)
    {
        if (room.roomObject != null)
        {
            room.roomObject.SetActive(true);
            room.isActive = true;
        }
    }

    // Deactivate a room
    private void DeactivateRoom(Room room)
    {
        if (room.roomObject != null)
        {
            room.roomObject.SetActive(false);
            room.isActive = false;
        }
    }

    // Get a reference to a specific room
    public Room GetRoom(string roomName)
    {
        return rooms.Find(r => r.roomName == roomName);
    }
}
