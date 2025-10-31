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
    [Tooltip("The Shop: Buy Upgrade Screen")]
    public GameObject buyUpgradeUI;
    [Tooltip("Pop up description for when the player hovers over an upgrade.")]
    public GameObject DescriptionGO;


    [Header("List of Upgrades Acquired")]
    [Tooltip("List of upgrades that the player has acquired.")]
    private List<DropData> playerUpgrades;

    [Header("Buttons")]
    [Tooltip("List of placeholder buttons for the upgrades that can be swapped")]
    private Button[] swapUpgradeButtons;
    [Tooltip("The first button to be selected when menu is opened.")]
    public GameObject firstButton;
    [Tooltip("The first button to be selected when the Shop: Buy Upgrade menu is opened.")]
    public GameObject buyUpgradeButton;

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
        if (!buyUpgradeUI.activeInHierarchy)
        {
            Time.timeScale = 1.0f;
            Cursor.lockState = CursorLockMode.Locked;
        }
        else if (buyUpgradeUI.activeInHierarchy)
        {
            EventSystem.current.SetSelectedGameObject(buyUpgradeButton);
        }
    }

    /// <summary>
    /// Takes in the acquired upgrades and renames the buttons to the names of the upgrades
    /// On clicking the button, the new upgrade applies to the player and the old one that got selected gets disabled.
    /// </summary>
    private void UpdateSwappableUpgrades()
    {
        if (DropSystem.Instance == null)
        {
            Debug.LogWarning("DropSystem instance not found.");
            return;
        }

        // refresh player upgrade list (necessary after selling)
        playerUpgrades = DropSystem.Instance.playerUpgrades;
        Dictionary<string, (DropData upgrade, int count)> groupedUpgrades = new();

        foreach (var upgrade in playerUpgrades)
        {
            if (upgrade == null) continue;

            string name = upgrade.GetDropName();
            if (!groupedUpgrades.ContainsKey(name))
            {
                groupedUpgrades[name] = (upgrade, 1);
            }
            else
            {
                groupedUpgrades[name] = (groupedUpgrades[name].upgrade, groupedUpgrades[name].count + 1);
            }
        }

        // clear buttons
        foreach (var upgradeButton in swapUpgradeButtons)
        {
            upgradeButton.gameObject.SetActive(false);
            upgradeButton.onClick.RemoveAllListeners();
        }

        var groupedList = new List<KeyValuePair<string, (DropData upgrade, int count)>>(groupedUpgrades);

        for (int i = 0; i < groupedList.Count; i++)
        {
            if (i >= swapUpgradeButtons.Length) break;
            var stack = groupedList[i];

            DropData upgrade = stack.Value.upgrade;
            int stackCount = stack.Value.count;

            Button button = swapUpgradeButtons[i];
            button.gameObject.SetActive(true);

            // Attach button text for name and price
            TMP_Text buttonText = button.GetComponentInChildren<TMP_Text>(true);
            for (int index = 0; index < 5; index++)
            {
                if (stackCount > 1)
                {
                    buttonText.text = $"{upgrade.GetDropName()} x{stackCount}";
                }
                else
                {
                    buttonText.text = upgrade.GetDropName();
                }
            }


            // Attach button icon
            Image buttonIcon = button.GetComponent<Image>();
            if (buttonIcon != null)
            {
                buttonIcon.sprite = upgrade.GetIcon();
            }
            else
            {
                Debug.LogWarning($"Cannot attach drop icon {playerUpgrades[i].GetDropName()} to button");
            }

            button.onClick.RemoveAllListeners();
            DropData capturedUpgrade = upgrade;
            button.onClick.AddListener(() =>
            {
                DropData newUpgrade = DropSystem.Instance.pendingSwap;

                if (buyUpgradeUI.activeInHierarchy)
                {
                    DropSystem.Instance.SellUpgrade(upgrade, true);
                }
                else
                {
                    DropSystem.Instance.SellUpgrade(upgrade, false);
                }

                DropSystem.Instance.playerUpgrades.Add(newUpgrade);
                HUDManager.Instance.AddUpgrade(newUpgrade);
                DropSystem.Instance.pendingSwap = null;
                UpdateSwappableUpgrades();
                CloseScreen();
                ShopManager.Instance.UpdateSellOptions();

            });

            EventTrigger trigger = button.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = button.gameObject.AddComponent<EventTrigger>();
            }
            trigger.triggers.Clear();

            // OnSelect event (controller highlight or hover)
            EventTrigger.Entry selectEntry = new EventTrigger.Entry();
            selectEntry.eventID = EventTriggerType.Select;
            selectEntry.callback.AddListener((eventData) => { ShowDescription(upgrade.GetDescription()); });
            trigger.triggers.Add(selectEntry);

            // OnDeselect event (leaving the button)
            EventTrigger.Entry deselectEntry = new EventTrigger.Entry();
            deselectEntry.eventID = EventTriggerType.Deselect;
            deselectEntry.callback.AddListener((eventData) => { HideDescription(); });
            trigger.triggers.Add(deselectEntry);
        }
    }

    private void ShowDescription(string text)
    {
        if (DescriptionGO == null)
        {
            Debug.LogWarning("DescriptionGO not assigned!");
            return;
        }

        DescriptionGO.SetActive(true);

        TMP_Text descriptionText = DescriptionGO.GetComponentInChildren<TMP_Text>(true);
        if (descriptionText != null)
        {
            descriptionText.text = text;
        }
        else
        {
            Debug.LogWarning("No TMP_Text found inside DescriptionGO!");
        }
    }

    private void HideDescription()
    {
        if (DescriptionGO == null) return;

        TMP_Text descriptionText = DescriptionGO.GetComponentInChildren<TMP_Text>(true);
        if (descriptionText != null) descriptionText.text = "";
    }

    /// <summary>
    /// Closes screen
    /// </summary>
    public void CloseScreen()
    {
        this.gameObject.SetActive(false);
    }
}
