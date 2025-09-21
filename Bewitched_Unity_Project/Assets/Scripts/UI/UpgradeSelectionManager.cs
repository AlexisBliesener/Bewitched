using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using UnityEngine.SceneManagement;

/// <summary>
/// This has to be attached to the UpgradeSelectionUI gameObject,
/// which contains the elements of the pop-up screen.
/// </summary>
public class UpgradeSelectionManager : MonoBehaviour
{
    [Header("Screens")]
    [Tooltip("The Upgrade Selection Screen")]
    public GameObject upgradeSelectionScreen;

    private Button[] upgradeOptionButtons;
    private Button salvageButton;

    /// <summary>
    /// On awake, create list of buttons that are the children of the upgrade selection screen, 
    /// so that we do not have to reassign the inspector everytime. Also subscribes to the drop event.
    /// </summary>
    private void Awake()
    {
        if (DropSystem.Instance != null)
        {
            DropSystem.Instance.OnDropRandomDrop += UpdateOptions;
        }
        else
        {
            Debug.LogWarning("DropSystem.Instance not found.");
        }

        upgradeOptionButtons = upgradeSelectionScreen.GetComponentsInChildren<Button>(true);
        if (upgradeOptionButtons.Length != 4)
        {
            Debug.LogWarning("Upgrade Selection UI does not have 4 buttons.");
        }
        else
        {
            salvageButton = upgradeOptionButtons[3];
        }
    }

    /// <summary>
    /// Shows the screen on enable, allows player to use cursor to navigate the screen
    /// </summary>
    private void OnEnable()
    {
        upgradeSelectionScreen.SetActive(true);
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
            DropSystem.Instance.OnDropRandomDrop -= UpdateOptions;
        }
        else
        {
            Debug.LogWarning("DropSystem.Instance not found.");
        }
        Time.timeScale = 1.0f;
        Cursor.lockState = CursorLockMode.Locked;
    }

    /// <summary>
    /// Takes in the random drops and renames the option buttons to the names of the drops
    /// </summary>
    private void UpdateOptions(DropData option1, DropData option2, DropData option3)
    {
        DropData[] options = { option1, option2, option3 };
        for (int i = 0; i < 3; i++)
        {
            TMP_Text buttonText = upgradeOptionButtons[i].GetComponentInChildren<TMP_Text>(true);
            if (buttonText != null)
            {
                buttonText.text = options[i].GetDropName();
            }
            else
            {
                Debug.LogWarning($"Cannot attach drop name {options[i].GetDropName()} to button");
            }

            upgradeOptionButtons[i].onClick.RemoveAllListeners();
            int capturedIndex = i;
            upgradeOptionButtons[i].onClick.AddListener(() =>
            {
                DropSystem.Instance.SelectDropsOption(options[capturedIndex]);
                CloseScreen();
            });
        }
        salvageButton.onClick.RemoveAllListeners();
        salvageButton.onClick.AddListener(() =>
        {
            DropSystem.Instance.SalvageDrop();
            CloseScreen();
        });
    }

    /// <summary>
    /// Closes screen
    /// </summary>
    public void CloseScreen()
    {
        this.gameObject.SetActive(false);
    }
}
