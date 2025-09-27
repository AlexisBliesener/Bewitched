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
        // Subscribing to the random upgrades
        if (DropSystem.Instance != null)
        {
            DropSystem.Instance.OnShopAlterInteract += UpdateShopOptions;
        }
        else
        {
            Debug.LogWarning("DropSystem.Instance not found.");
        }

    }

    /// <summary>
    /// Shows the screen on enable, allows player to use cursor to navigate the screen
    /// </summary>
    private void OnEnable()
    {
        ShopUI.SetActive(true);
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

    private void UpdateShopOptions(List<DropData> options)
    {
        if (options == null || options.Count < 5)
        {
            Debug.LogWarning("Not enough shop options.");
        }
        for (int i = 0; i < 5; i++)
        {
            DropData shopUpgrade = options[i];
            Debug.Log(options[i].GetDropName());

            // Attach button text
            TMP_Text buttonText = shopUpgradeButtons[i].GetComponentInChildren<TMP_Text>(true);
            if (buttonText != null)
            {
                buttonText.text = options[i].GetDropName();
            }
            else
            {
                Debug.LogWarning($"Cannot attach drop name {options[i].GetDropName()} to button");
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
    
    
    /// <summary>
    /// Closes screen
    /// </summary>
    public void CloseScreen()
    {
        this.gameObject.SetActive(false);
    }
}
