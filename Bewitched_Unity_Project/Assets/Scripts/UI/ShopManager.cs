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
    [Tooltip("The Shop Upgrade Screen")]
    public GameObject ShopUI;
    [Tooltip("The Buy Upgrades Screen")]
    public GameObject BuyUI;
    [Tooltip("The Sell Upgrades Screen")]
    public GameObject SellUI;

    [Header("List of Upgrades Acquired")]
    [Tooltip("List of upgrades that the player has acquired.")]
    private List<DropData> playerUpgrades;


    [Header("Buttons")]
    [Tooltip("List of placeholder buttons for the upgrades that can be bought")]
    private Button[] buyUpgradeButtons;
    [Tooltip("List of placeholder buttons for the upgrades that can be sold")]
    private Button[] sellUpgradeButtons;
    [Tooltip("The first button to be selected when menu is opened.")]
    public GameObject firstButton;

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

    public void BuyScreen()
    {
        SellUI.gameObject.SetActive(false);
        BuyUI.gameObject.SetActive(true);
    }

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
                // Line below adds item automatically, but need to check souls
                // DropSystem.Instance.SelectDropsOption(options[capturedIndex]);
                CloseScreen();
            });
        }
    }


    private void UpdateSellOptions()
    {

        for (int i = 0; i < sellUpgradeButtons.Length; i++)
        {
            if (i < playerUpgrades.Count)
            {
                sellUpgradeButtons[i].gameObject.SetActive(true);

                // Attach button text for name and price
                TMP_Text[] buttonText = sellUpgradeButtons[i].GetComponentsInChildren<TMP_Text>(true);
                foreach (var t in buttonText)
                {
                    if (t != null)
                    {
                        if (t.name == "Name")
                        {
                            t.text = playerUpgrades[i].GetDropName();
                        }
                        else if (t.name == "SellPrice")
                        {
                            t.text = playerUpgrades[i].GetSellAmount().ToString();
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Cannot attach drop name {playerUpgrades[i].GetDropName()} and drop price {playerUpgrades[i].GetBuyAmount()} to button");
                    }
                }


                // Attach button icon
                Image buttonIcon = sellUpgradeButtons[i].GetComponent<Image>();
                if (buttonIcon != null)
                {
                    buttonIcon.sprite = playerUpgrades[i].GetIcon();
                }
                else
                {
                    Debug.LogWarning($"Cannot attach drop icon {playerUpgrades[i].GetDropName()} to button");
                }

                sellUpgradeButtons[i].onClick.RemoveAllListeners();
                int capturedIndex = i;
                sellUpgradeButtons[i].onClick.AddListener(() =>
                {
                    // Need to add in disabling the upgrade that got swapped
                    // This is the old upgrade that needs to be disabled
                    // DropSystem.Instance.SelectDropsOption(playerUpgrades[capturedIndex]); 

                    // Add in enabling the new upgrade (not on the swappable screen)

                    CloseScreen();
                });
            }
            else
            {
                sellUpgradeButtons[i].gameObject.SetActive(false);
            }
        }
        sellUpgradeButtons[5].gameObject.SetActive(true);
    }


    /// <summary>
    /// Closes screen
    /// </summary>
    public void CloseScreen()
    {
        this.gameObject.SetActive(false);
    }
}
