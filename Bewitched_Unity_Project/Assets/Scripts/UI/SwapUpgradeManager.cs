using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

/// <summary>
/// This has to be attached to the swapUpgradeUI gameObject,
/// which contains the elements of the pop-up screen.
/// Should only show up if the player already has 5 upgrades and wants to select a new one.
/// Pressing the swappable upgrade auto-swaps it (no confirm button)
/// </summary>
public class SwapUpgradeManager : MonoBehaviour
{
    [Header("Screens")]
    [Tooltip("The Swap Upgrade Screen")]
    public GameObject swapUpgradeUI;
    [Tooltip("The Parent that holds upgrade buttons")]
    public GameObject upgradesParent;
    [Tooltip("The Shop: Buy Upgrade Screen")]
    public GameObject buyUpgradeUI;

    [Header("Screen Objects")]
    [Tooltip("Pop up description game object for when the player hovers over an upgrade.")]
    public GameObject descriptionGO;
    [Tooltip("Selected upgrade name text, child of descriptionGO.")]
    private TMP_Text nameText;
    [Tooltip("Description text, child of descriptionGO.")]
    private TMP_Text descriptionText;
    [Tooltip("Pending upgrade game object, needs to contain the placeholder upgrade details.")]
    public GameObject pendingGO;

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
        swapUpgradeButtons = upgradesParent.GetComponentsInChildren<Button>(true);
        if (swapUpgradeButtons.Length == 5)
        {
            UpdateSwappableUpgrades();
        }
        else
        {
            Debug.LogWarning("Upgrade Swap UI does not have 5 buttons/upgrades.");
        }

        // Get name and description text for description screen
        if (descriptionGO != null)
        {
            TMP_Text[] descTexts = descriptionGO.GetComponentsInChildren<TMP_Text>(true);
            foreach (var t in descTexts)
            {
                if (t != null)
                {
                    if (t.name == "UpgradeName")
                    {
                        nameText = t;
                    }
                    else if (t.name == "DescriptionText")
                    {
                        descriptionText = t;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Shows the screen on enable, allows player to use cursor to navigate the screen
    /// </summary>
    private void OnEnable()
    {
        swapUpgradeUI.SetActive(true);
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(firstButton);
        Time.timeScale = 0f;

        UpdateSelectedUpgrade();
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
    /// Updates pending upgrade display
    /// </summary>
    public void UpdateSelectedUpgrade()
    {
        if (pendingGO == null)
        {
            Debug.LogWarning("pendingGO is not assigned.");
            return;
        }
        DropData pending = DropSystem.Instance != null ? DropSystem.Instance.pendingSwap : null;
        if (pending == null)
        {
            Debug.LogWarning("No pending swap found in DropSystem.");
            pendingGO.SetActive(false);
            return;
        }
        TMP_Text[] texts = pendingGO.GetComponentsInChildren<TMP_Text>(true);
        foreach (var text in texts)
        {
            if (text.name == "Title")
            {
                text.text = pending.GetDropName();

            }
            else if (text.name == "Description")
            {
                text.text = pending.GetDescription();
            }
        }

        GameObject iconGO = pendingGO.transform.GetChild(0).gameObject;
        Image iconSprite = iconGO.GetComponent<Image>();

        if (iconGO != null)
        {
            Image iconImage = iconGO.GetComponent<Image>();
            if (iconImage != null)
            {
                iconImage.sprite = pending.GetIcon();
            }
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

                DropSystem.Instance.SelectDropsOption(newUpgrade);
                HUDManager.Instance.RefreshHUD();
                DropSystem.Instance.pendingSwap = null;
                UpdateSwappableUpgrades();
                CloseScreen();
                if (ShopManager.Instance != null)
                {
                    ShopManager.Instance.UpdateSellOptions();
                }
                else
                {
                    Debug.LogWarning("ShopManager instance is null.");
                }
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
            selectEntry.callback.AddListener((eventData) => { ShowDescription(upgrade.GetDropName(), upgrade.GetDescription()); });
            trigger.triggers.Add(selectEntry);

            // OnDeselect event (leaving the button)
            EventTrigger.Entry deselectEntry = new EventTrigger.Entry();
            deselectEntry.eventID = EventTriggerType.Deselect;
            deselectEntry.callback.AddListener((eventData) => { HideDescription(); });
            trigger.triggers.Add(deselectEntry);
        }
    }

    /// <summary>
    /// Cancels swap transaction
    /// </summary>
    public void CancelSwap()
    {
        if (DropSystem.Instance == null)
        {
            Debug.LogWarning("DropSystem.Instance not found.");
            return;
        }
        DropData pending = DropSystem.Instance.pendingSwap;
        DropSystem.Instance.pendingSwap = null;
        CloseScreen();
        if (buyUpgradeUI != null && buyUpgradeUI.activeInHierarchy)
        {
            buyUpgradeUI.SetActive(true);
            ShopManager.Instance.RestoreUpgrade(pending);
            EventSystem.current.SetSelectedGameObject(buyUpgradeButton);
        }
        else if (DropSystem.Instance.upgradeSelectionUI != null)
        {
            DropSystem.Instance.upgradeSelectionUI.SetActive(true);
            EventSystem.current.SetSelectedGameObject(
                DropSystem.Instance.upgradeSelectionUI.GetComponent<UpgradeSelectionManager>().firstButton
            );
        }
        else
        {
            Debug.LogWarning("No UI found to return to after canceling swap.");
        }
    }

    /// <summary>
    /// Show Name and Description of upgrade that is currently selected
    /// </summary>
    private void ShowDescription(string upgradeNameText, string descText)
    {
        if (descriptionGO == null)
        {
            Debug.LogWarning("descriptionGO not assigned!");
            return;
        }

        descriptionGO.SetActive(true);

        if (nameText != null)
        {
            nameText.text = upgradeNameText;
        }
        else
        {
            Debug.LogWarning("No name TMP_Text found inside descriptionGO!");
        }

        if (descriptionText != null)
        {
            descriptionText.text = descText;
        }
        else
        {
            Debug.LogWarning("No description TMP_Text found inside descriptionGO!");
        }
    }
    /// <summary>
    /// Hide Description of upgrade when deselected
    /// </summary>
    private void HideDescription()
    {
        if (descriptionGO == null) return;

        if (nameText != null) nameText.text = "";
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
