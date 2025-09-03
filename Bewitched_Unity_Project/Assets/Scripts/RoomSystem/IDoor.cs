/// <summary>
/// Interface for door objects that can be locked and unlocked by the room system
/// </summary>
public interface IDoor
{
    /// <summary>
    /// Locks the door
    /// </summary>
    void Lock();

    /// <summary>
    /// Unlocks the door
    /// </summary>
    void Unlock();
}

