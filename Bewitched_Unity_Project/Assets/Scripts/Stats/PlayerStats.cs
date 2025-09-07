using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// A class that holds player stats with methods to load and save information to JSON
/// </summary>
[System.Serializable]
public class PlayerStats
{
    [Tooltip("Bool Checking if Scenes Are Swapping")]
    private bool sceneSwap;

    [Tooltip("Amount of XP gained")]
    private int xpGained;

    [Tooltip("Player Level")]
    private int playerLevel;

    [Tooltip("The Player's Last Saved Maximum Health")]
    private float maxHealth;

    [Tooltip("The Player's Last Saved Position")]
    private Vector3 playerPosition;

    [Tooltip("The Number of Each Upgrade the Player has Obtained")]
    private Dictionary<string, int> upgrades;

    /// <summary>
    /// Default Constructor - called when a new game is started and sets default values
    /// </summary>
    public PlayerStats()
    {
        sceneSwap = false;
        maxHealth = 100;
        xpGained = 0;
        playerLevel = 1;
        playerPosition = new Vector3(0, 0, 0); // Change this to set to whereever player spawns in level 1
        upgrades = new Dictionary<string, int>();
    }

    #region Getters

    /// <summary>
    /// XP getter
    /// </summary>
    /// <returns> Get XP gained </returns>
    public int GetXPGained()
    {
        return xpGained;
    }

    /// <summary>
    /// Player Level Getter
    /// </summary>
    /// <returns> Gets Current Level </returns>
    public int GetLevel()
    {
        return playerLevel;
    }

    /// <summary>
    /// Max Health Getter
    /// </summary>
    /// <returns> Max Health </returns>
    public float GetMaxHealth()
    {
        return maxHealth;
    }

    /// <summary>
    /// Player Position Getter
    /// </summary>
    /// <returns> Player Position </returns>
    public Vector3 GetPosition()
    {
        return playerPosition;
    }

    /// <summary>
    /// Get the Number of A Certain Upgrade
    /// </summary>
    /// <param name="upgrade"> Upgrade to Search For </param>
    /// <returns> Number of that upgrade player has </returns>
    public int GetUpgradeAmt(string upgrade)
    {
        if (upgrades.ContainsKey(upgrade))
        {
            return upgrades[upgrade];
        }
        return 0;
    }

    #endregion

    #region Setters

    /// <summary>
    /// Sets the scene swap checker
    /// </summary>
    /// <param name="val"> Value to set swapping scenes </param>
    public void SetSceneSwap(bool val)
    {
        sceneSwap = val;
    }

    /// <summary>
    /// Player Position Setter
    /// </summary>
    /// <param name="position"> The player's Position </param>
    public void SetPlayerPosition(Vector3 position)
    {
        playerPosition = position;
    }

    /// <summary>
    /// Upgrade Amount Setter
    /// </summary>
    /// <param name="upgrade"> Upgrade to Set </param>
    /// <param name="amt"> Amount to Set to </param>
    public void SetUpgradeAmt(string upgrade, int amt) // For manually setting a set amount of upgrades (like if upgrades get converted)
    {
        upgrades[upgrade] = amt;
    }

    #endregion

    #region Incrementers

    /// <summary>
    /// Upgrade Incrementer
    /// </summary>
    /// <param name="upgrade"> Upgrade to Increment</param>
    public void IncrementUpgradeAmt(string upgrade)
    {
        if (upgrades.ContainsKey(upgrade))
        {
            upgrades[upgrade]++;
        }
        else
        {
            upgrades[upgrade] = 1;
        }
    }

    /// <summary>
    /// Increments the players xp by an amount
    /// </summary>
    /// <param name="xp"> XP to increment by </param>
    public void IncrementXP(int xp)
    {
        xpGained += xp;

        if (xpGained / 1000 > playerLevel)
        {
            playerLevel++;
            StatLoader.instance.HandleLevelUp();
        }
    }

    /// <summary>
    /// Increments the player's level
    /// </summary>
    public void IncrementLevel()
    {
        playerLevel += 1;
    }

    #endregion

    /// <summary>
    /// Checks if the player has leveled up
    /// </summary>
    /// <returns> True if the player has leveled up </returns>
    public bool CheckLevelUp()
    {
        if (xpGained / 1000 > playerLevel)
        {
            IncrementLevel();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Checks save validity (if swapping between scenes and makes invalid)
    /// </summary>
    /// <returns> True if valid, false otherwise </returns>
    public bool CheckSwappingScenes()
    {
        if (sceneSwap)
        {
            sceneSwap = false;
            return true;
        }
        return false;
    }
}
