using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

/// <summary>
/// Attached to the DeathUI gameObject, which will contain the stats to display from the game run.
/// Will also show the death animation of the player.
/// </summary>

public class DeathUIManager : MonoBehaviour
{
    [Header("Screens")]
    [Tooltip("The Death Screen")]
    public GameObject deathScreen;
    [Tooltip("The Stats Screen, parent of icons")]
    public GameObject statsScreen;

    [Header("Buttons")]
    [Tooltip("The first button to be selected when menu is opened.")]
    public GameObject firstButton;

    [Header("Upgrades Acquired")]
    [Tooltip("List of upgrades that the player has acquired.")]
    private List<DropData> playerUpgrades;
    [Tooltip("List of placeholder Images to be replaced by upgrade icons.")]
    private Image[] upgradeSlots;

    /// <summary>
    /// Shows the screen on enable, allows player to use cursor to navigate the screen
    /// </summary>
    private void OnEnable()
    {
        AudioManager.SubscribeCheckClick();
        if (deathScreen != null && deathScreen.activeInHierarchy == false)
        {
            deathScreen.SetActive(true);
            Debug.Log("script");
        }
        EventSystem.current.SetSelectedGameObject(firstButton);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        upgradeSlots = statsScreen.GetComponentsInChildren<Image>(true);
        /// Stats (upgrades)
        if (DropSystem.Instance != null)
        {
            playerUpgrades = DropSystem.Instance.playerUpgrades;
            UpdateStats();
        }
        else
        {
            // if no upgrades collected, show empty slots.
            Debug.LogWarning("DropSystem.Instance not found.");
        }
    }

    /// <summary>
    /// Stops checking for UI button clicks when Quit Menu is pressed
    /// </summary>
    void OnDisable()
    {
        AudioManager.UnsubscribeCheckClick();
    }

    /// will need to add stats and death animation into the death screen

    /// <summary>
    /// Updates the placeholder upgrade slots 
    /// with the player's acquired upgrades they got in the run.
    /// </summary>
    private void UpdateStats()
    {
        for (int i = 0; i < playerUpgrades.Count; i++)
        {
            Image iconSlot = upgradeSlots[i];
            if (iconSlot != null)
            {
                iconSlot.sprite = playerUpgrades[i].GetIcon();
            }

        }
    }
}
