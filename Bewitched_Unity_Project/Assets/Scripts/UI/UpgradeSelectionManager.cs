using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

/// <summary>
/// This has to be attached to the UpgradeSelectionUI gameObject,
/// which contains the elements of the pop-up screen.
/// </summary>
public class UpgradeSelectionManager : MonoBehaviour
{
    [Header("Screens")]
    [Tooltip("The Upgrade Selection Screen")]
    public GameObject upgradeSelectionScreen;
    [Tooltip("The Swap Upgrade Screen")]
    public GameObject swapUpgradeUI;

    [Header("Buttons")]
    [Tooltip("List of buttons for the upgrade drops")]
    private Button[] upgradeOptionButtons;
    [Tooltip("Salvage Upgrade Button")]
    private Button salvageButton;
    [Tooltip("The first button to be selected when menu is opened.")]
    public GameObject firstButton;

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

        if (upgradeSelectionScreen != null && upgradeSelectionScreen.activeInHierarchy == false)
        {
            upgradeSelectionScreen.SetActive(true);
            Debug.Log("on");
        }
        
        StartCoroutine(SetFirstButtonDelay());
        //Plays UpgradeOpen and ducks audio
        AudioManager.TryPlayOneShot("UpgradeOpen");
        AudioManager.OpenUIAudio(0.8f);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
    }


    /// <summary>
    /// One frame delay so that the SetSelectedGameObject does not auto-submit (for controller)
    /// </summary>
    private IEnumerator SetFirstButtonDelay()
    {
        EventSystem.current.SetSelectedGameObject(null);
        yield return null; //wait one frame to avoid double-press
        if (firstButton != null)
        {
            EventSystem.current.SetSelectedGameObject(firstButton);
        }
    }

    /// <summary>
    /// Disables Screen, unless Swap Upgrade is active.
    /// </summary>
    private void OnDisable()
    {
        if (swapUpgradeUI == null || !swapUpgradeUI.activeSelf)
        {
            EventSystem.current.SetSelectedGameObject(null);

            Time.timeScale = 1.0f;
            Cursor.lockState = CursorLockMode.Locked;
        }
        
    }

    /// <summary>
    /// Destroys the subscription so that the UI screen buttons will re-load with the new drops in the next upgrade.
    /// </summary>
    private void OnDestroy()
    {
        if (DropSystem.Instance != null)
        {
            DropSystem.Instance.OnDropRandomDrop -= UpdateOptions;
        }
        else
        {
            Debug.LogWarning("DropSystem.Instance not found.");
        }
    }

    /// <summary>
    /// Takes in the random drops and renames the option buttons to the names of the drops
    /// </summary>
    private void UpdateOptions(DropData option1, DropData option2, DropData option3)
    {
        DropData[] options = { option1, option2, option3 };
        for (int i = 0; i < 3; i++)
        {
            // Attach button title and description
            TMP_Text[] buttonText = upgradeOptionButtons[i].GetComponentsInChildren<TMP_Text>(true);
            foreach (var t in buttonText)
            {
                if (t != null)
                {
                    if (t.name == "Title")
                    {
                        t.text = options[i].GetDropName();
                    }
                    else if (t.name == "Description")
                    {
                        t.text = options[i].GetDescription();
                    }

                }
                else
                {
                    Debug.LogWarning($"Cannot attach drop name {options[i].GetDropName()} and drop description {options[i].GetDescription()} to button");
                }
            }
            
            // Attach button icon
            GameObject iconGO = upgradeOptionButtons[i].transform.GetChild(0).gameObject;
            Image iconSprite = iconGO.GetComponent<Image>();
            if (iconGO.name == "Icon")
            {
                iconSprite.sprite = options[i].GetIcon();
            }
            else
            {
                Debug.LogWarning($"Cannot attach drop icon {options[i].GetDropName()} to button");
            }

            upgradeOptionButtons[i].onClick.RemoveAllListeners();
            int capturedIndex = i;
            upgradeOptionButtons[i].onClick.AddListener(() =>
            {
                DropSystem.Instance.SelectDropsOption(options[capturedIndex]);
                CloseScreen();
                //Plays upgrade select sound effect
                AudioManager.TryPlayOneShot("UpgradeSelect");
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
        //Fades in the rest of audio
        AudioManager.CloseUIAudio(0.8f);
        this.gameObject.SetActive(false);
    }
}
