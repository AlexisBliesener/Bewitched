using UnityEngine;


/// <summary>
/// Data for each room
/// </summary>
[System.Serializable]
public class RoomData
{
    [Tooltip("Room name")]
    public string roomName = "Room";
    [Tooltip("Room controller reference")]
    public RoomController roomController;
}