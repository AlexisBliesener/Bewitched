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


    [Header("Buttons")]
    [Tooltip("List of placeholder buttons for the upgrades that can be bought")]
    private Button[] shopUpgradeButtons;
    [Tooltip("The first button to be selected when menu is opened.")]
    public GameObject firstButton;

    /// <summary>
    /// On awake, create list of buttons that are the children of the shop upgrades screen, 
    /// so that we do not have to reassign the inspector everytime.
    /// </summary>
    private void Awake()
    {
        
        shopUpgradeButtons = ShopUI.GetComponentsInChildren<Button>(true);
    }

    /// <summary>
    /// Shows the screen on enable, allows player to use cursor to navigate the screen
    /// </summary>
    private void OnEnable()
    {
        ShopUI.SetActive(true);
        BuyUI.SetActive(true);
        SellUI.SetActive(false);

        // Subscribing to the random upgrades
        if (DropSystem.Instance != null)
        {
            DropSystem.Instance.OnShopAlterInteract += UpdateShopOptions;
        }
        else
        {
            Debug.LogWarning("DropSystem.Instance not found.");
        }

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
        if (DropSystem.Instance != null)
        {
            DropSystem.Instance.OnShopAlterInteract -= UpdateShopOptions;
        }

        EventSystem.current.SetSelectedGameObject(null);
        Time.timeScale = 1.0f;
        Cursor.lockState = CursorLockMode.Locked;
    }

    /// <summary>
    /// Updated the placeholder shop buttons with the name and icons of the random drops that can be bought.
    /// </summary>
    private void UpdateShopOptions(List<DropData> options)
    {

        if (options == null || options.Count < 5)
        {
            Debug.LogWarning("Not enough shop options.");
        }

        for (int i = 0; i < 5; i++)
        {
            DropData shopUpgrade = options[i];

            // Attach button text for name and price
            TMP_Text[] buttonText = shopUpgradeButtons[i].GetComponentsInChildren<TMP_Text>(true);
            foreach (var t in buttonText)
            {
                if (t != null)
                {
                    if (t.name == "Name")
                    {
                        t.text = options[i].GetDropName();
                    }
                    else if (t.name == "Price")
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
                Image buttonIcon = shopUpgradeButtons[i].GetComponent<Image>();
            if (buttonIcon != null)
            {
                buttonIcon.sprite = options[i].GetIcon();
            }
            else
            {
                Debug.LogWarning($"Cannot attach drop icon {options[i].GetDropName()} to button");
            }

            shopUpgradeButtons[i].onClick.RemoveAllListeners();
            int capturedIndex = i;
            shopUpgradeButtons[i].onClick.AddListener(() =>
            {
                // Line below adds item automatically, but need to check souls
                // DropSystem.Instance.SelectDropsOption(options[capturedIndex]);
                CloseScreen();
            });
        }
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
    /// Closes screen
    /// </summary>
    public void CloseScreen()
    {
        this.gameObject.SetActive(false);
    }
}
