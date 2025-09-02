/// <summary>
/// The current state of the room 
/// This is used in RoomController to change the state of the room
/// </summary>
public enum RoomState
{
    Inactive,   // Room not entered yet
    Active,     // Room entered , enemies spawneda , doors locked
    Cleared     // All enemies defeated and doors unlocked
}