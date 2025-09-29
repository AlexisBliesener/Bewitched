using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class ShopManager : MonoBehaviour
{
    [Header("Screens")]
    [Tooltip("The Buy Upgrades Screen")]
    public GameObject BuyUI;
    [Tooltip("The Sell Upgrades Screen")]
    public GameObject SellUI;

    [Header("List of Upgrades Acquired")]
    [Tooltip("List of upgrades that the player has acquired.")]
    private List<DropData> playerUpgrades;

    [Header("Buttons")]
    [Tooltip("The first button to be selected when menu is opened.")]
    public GameObject firstButton;
    [Tooltip("List of placeholder buttons for the upgrades that can be bought")]
    private Button[] buyUpgradeButtons;
    [Tooltip("List of placeholder buttons for the upgrades that can be sold")]
    private Button[] sellUpgradeButtons;
    
    [Header("Pop-ups")]
    [Tooltip("Pop up text for when the player has insufficient funds to buy an upgrade.")]
    public GameObject NoSoulText;

    /// <summary>
    /// Shows the screen on enable, allows player to use cursor to navigate the screen
    /// </summary>
    private void OnEnable()
    {
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        BuyUI.SetActive(true);
        SellUI.SetActive(false);
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(firstButton);

        // Subscribing to the random upgrades
        if (DropSystem.Instance != null)
        {
            DropSystem.Instance.OnShopAlterInteract += UpdateBuyOptions;
        }
        else
        {
            Debug.LogWarning("DropSystem.Instance not found.");
        }

        // Getting the upgrades the player already has
        if (DropSystem.Instance != null)
        {
            playerUpgrades = DropSystem.Instance.playerUpgrades;
        }
        else
        {
            Debug.LogWarning("DropSystem.Instance not found.");
            playerUpgrades = new List<DropData>();
        }
        buyUpgradeButtons = BuyUI.GetComponentsInChildren<Button>(true);
        sellUpgradeButtons = SellUI.GetComponentsInChildren<Button>(true);
        UpdateSellOptions();

    }

    /// <summary>
    /// Sets buy screen active. To be attached to a "Buy Menu" button.
    /// </summary>
    public void BuyScreen()
    {
        SellUI.gameObject.SetActive(false);
        BuyUI.gameObject.SetActive(true);
    }

    /// <summary>
    /// Sets sell screen active. To be attached to a "Sell Menu" button.
    /// </summary>
    public void SellScreen()
    {
        BuyUI.gameObject.SetActive(false);
        SellUI.gameObject.SetActive(true);
    }

    /// <summary>
    /// Disables Screen
    /// </summary>
    private void OnDisable()
    {
        if (DropSystem.Instance != null)
        {
            DropSystem.Instance.OnShopAlterInteract -= UpdateBuyOptions;
        }

        EventSystem.current.SetSelectedGameObject(null);
        Time.timeScale = 1.0f;
        Cursor.lockState = CursorLockMode.Locked;
    }

    /// <summary>
    /// Updated the placeholder shop buttons with the name and icons of the random drops that can be bought.
    /// </summary>
    private void UpdateBuyOptions(List<DropData> options)
    {
        if (options == null || options.Count < 5)
        {
            Debug.LogWarning("Not enough shop options.");
        }

        for (int i = 0; i < 5; i++) // only do 5 buttons in the list (does not include the buy/sell button)
        {
            // Attach button text for name and price
            TMP_Text[] buttonText = buyUpgradeButtons[i].GetComponentsInChildren<TMP_Text>(true);
            foreach (var t in buttonText)
            {
                if (t != null)
                {
                    if (t.name == "Name")
                    {
                        t.text = options[i].GetDropName();
                    }
                    else if (t.name == "BuyPrice")
                    {
                        t.text = options[i].GetBuyAmount().ToString();
                    }

                }
                else
                {
                    Debug.LogWarning($"Cannot attach drop name {options[i].GetDropName()} and drop price {options[i].GetBuyAmount()} to button");
                }
            }

            // Attach button icon
                Image buttonIcon = buyUpgradeButtons[i].GetComponent<Image>();
            if (buttonIcon != null)
            {
                buttonIcon.sprite = options[i].GetIcon();
            }
            else
            {
                Debug.LogWarning($"Cannot attach drop icon {options[i].GetDropName()} to button");
            }

            buyUpgradeButtons[i].onClick.RemoveAllListeners();
            int capturedIndex = i;
            buyUpgradeButtons[i].onClick.AddListener(() =>
            {
                DropData chosenUpgrade = options[capturedIndex];

                if (DropSystem.Instance.BuyUpgrade(chosenUpgrade))
                {
                    // Bought upgrade, gave souls
                    buyUpgradeButtons[capturedIndex].gameObject.SetActive(false);
                    UpdateSellOptions();
                }
                else
                {
                    // Not enough souls to buy requested upgrade
                    ShowPopup(NoSoulText, 3f);
                }
            });
        }
    }

    /// <summary>
    /// Updates the upgrade buttons that appear on the sell screen of the shop altar.
    /// Buttons are populated with the player's current upgrades and how much soul they will sell for.
    /// Clears and refreshes every time because screen has to be refreshed when player sells an upgrade.
    /// </summary>
    private void UpdateSellOptions()
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
        foreach (var upgradeButton in sellUpgradeButtons)
        {
            upgradeButton.gameObject.SetActive(false);
            upgradeButton.onClick.RemoveAllListeners();
        }

        int i = 0;

        foreach (var stack in groupedUpgrades)
        {
            if (i >= sellUpgradeButtons.Length - 1) break; // last button is a menu switch button

            DropData upgrade = stack.Value.upgrade;
            int stackCount = stack.Value.count;

            Button button = sellUpgradeButtons[i];
            button.gameObject.SetActive(true);

            // Attach button text for name and price
            TMP_Text[] buttonText = button.GetComponentsInChildren<TMP_Text>(true);
            foreach (var t in buttonText)
            {
                if (t != null)
                {
                    if (t.name == "Name")
                    {
                        if (stackCount > 1)
                        {
                            t.text = $"{upgrade.GetDropName()} x{stackCount}";
                        }
                        else
                        {
                            t.text = $"{upgrade.GetDropName()}";
                        }
                    }
                    else if (t.name == "SellPrice")
                    {
                        t.text = (upgrade.GetSellAmount() * stackCount).ToString();
                    }
                }
                else
                {
                    Debug.LogWarning($"Cannot attach drop name {playerUpgrades[i].GetDropName()} and drop price {playerUpgrades[i].GetBuyAmount()} to button, text gameObject null.");
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
                if (DropSystem.Instance.SellUpgrade(capturedUpgrade))
                {
                    UpdateSellOptions();
                }

            });

            i++;
        }

        for (int j = i; j < sellUpgradeButtons.Length - 1; j++)
        {
            sellUpgradeButtons[j].gameObject.SetActive(false);

        }
        sellUpgradeButtons[sellUpgradeButtons.Length - 1].gameObject.SetActive(true);

    }


    /// <summary>
    /// Show pop up when player tries to buy an upgrade but does not have enough money.
    /// </summary>
    public void ShowPopup(GameObject popup, float seconds = 3f)
    {
        StartCoroutine(ShowPopupCorountine(popup, seconds));
    }

    /// <summary>
    /// Corountine for popup
    /// </summary>
    private IEnumerator ShowPopupCorountine(GameObject popup, float seconds)
    {
        popup.SetActive(true);
        yield return new WaitForSecondsRealtime(seconds);
        popup.SetActive(false);
    }

    /// <summary>
    /// Closes screen
    /// </summary>
    public void CloseScreen()
    {
        this.gameObject.SetActive(false);
    }
}
