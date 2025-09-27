using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

/// <summary>
/// This has to be attached to the SwapUpgradeUI gameObject,
/// which contains the elements of the pop-up screen.
/// Should only show up if the player already has 5 upgrades and wants to select a new one.
/// Pressing the swappable upgrade auto-swaps it (no confirm button)
/// </summary>
public class SwapUpgradeManager : MonoBehaviour
{
    [Header("Screens")]
    [Tooltip("The Swap Upgrade Screen")]
    public GameObject SwapUpgradeUI;

    [Header("List of Upgrades Acquired")]
    [Tooltip("List of upgrades that the player has acquired.")]
    private List<DropData> playerUpgrades;

    [Header("Buttons")]
    [Tooltip("List of placeholder buttons for the upgrades that can be swapped")]
    private Button[] swapUpgradeButtons;
    [Tooltip("The first button to be selected when menu is opened.")]
    public GameObject firstButton;

    /// <summary>
    /// On awake, create list of buttons that are the children of the swap upgrades screen, 
    /// so that we do not have to reassign the inspector everytime.
    /// </summary>
    private void Awake()
    {
        // Getting the player acquired upgrades
        if (DropSystem.Instance != null)
        {
            playerUpgrades = DropSystem.Instance.playerUpgrades;
        }
        else
        {
            Debug.LogWarning("DropSystem.Instance not found.");
            playerUpgrades = new List<DropData>();
        }

        // Get swap upgrade placeholder buttons
        swapUpgradeButtons = SwapUpgradeUI.GetComponentsInChildren<Button>(true);
        if (swapUpgradeButtons.Length == 5)
        {
            UpdateSwappableUpgrades();
        }
        else
        {
            Debug.LogWarning("Upgrade Swap UI does not have 5 buttons/upgrades.");
        }
    }

    /// <summary>
    /// Shows the screen on enable, allows player to use cursor to navigate the screen
    /// </summary>
    private void OnEnable()
    {
        SwapUpgradeUI.SetActive(true);
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(firstButton);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
    }

    /// <summary>
    /// Disables Screen
    /// </summary>
    private void OnDisable()
    {
        EventSystem.current.SetSelectedGameObject(null);
        Time.timeScale = 1.0f;
        Cursor.lockState = CursorLockMode.Locked;
    }

    /// <summary>
    /// Takes in the acquired upgrades and renames the buttons to the names of the upgrades
    /// On clicking the button, the new upgrade applies to the player and the old one that got selected gets disabled.
    /// </summary>
    private void UpdateSwappableUpgrades()
    {
        for (int i = 0; i < swapUpgradeButtons.Length; i++)
        {
            // Attach button text
            TMP_Text buttonText = swapUpgradeButtons[i].GetComponentInChildren<TMP_Text>(true);
            if (buttonText != null)
            {
                buttonText.text = playerUpgrades[i].GetDropName();
            }
            else
            {
                Debug.LogWarning($"Cannot attach drop name {playerUpgrades[i].GetDropName()} to button");
            }

            // Attach button icon
            Image buttonIcon = swapUpgradeButtons[i].GetComponent<Image>();
            if (buttonIcon != null)
            {
                buttonIcon.sprite = playerUpgrades[i].GetIcon();
            }
            else
            {
                Debug.LogWarning($"Cannot attach drop icon {playerUpgrades[i].GetDropName()} to button");
            }

            swapUpgradeButtons[i].onClick.RemoveAllListeners();
            int capturedIndex = i;
            swapUpgradeButtons[i].onClick.AddListener(() =>
            {
                // Need to add in disabling the upgrade that got swapped
                // This is the old upgrade that needs to be disabled
                // DropSystem.Instance.SelectDropsOption(playerUpgrades[capturedIndex]); 

                // Add in enabling the new upgrade (not on the swappable screen)

                CloseScreen();
            });
        }
    }

    /// <summary>
    /// Closes screen
    /// </summary>
    public void CloseScreen()
    {
        this.gameObject.SetActive(false);
    }
}
