using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

/// <summary>
/// This has to be attached to the PauseUI gameObject,
/// which contains sub-menus for Settings, the Compendium, and Upgrades.
/// </summary>
public class PauseManager : MonoBehaviour
{
    [Header("Screens")]
    [Tooltip("The Default Pause Screen")]
    public GameObject mainPauseScreen;
    [Tooltip("The Settings Screen")]
    public GameObject settingsScreen;
    [Tooltip("The Compendium Screen")]
    public GameObject compendiumScreen;
    [Tooltip("The Upgrade Log Screen")]
    public GameObject upgradeScreen;

    [Header("First Selected Buttons")]
    [Tooltip("The first button to be selected when Pause menu is opened.")]
    public GameObject pauseButton;
    [Tooltip("The first button to be selected when Compendium menu is opened.")]
    public GameObject compendiumButton;
    [Tooltip("The first button to be selected when Upgrade Log menu is opened.")]
    public GameObject upgradeButton;
    [Tooltip("The first button to be selected when Settings menu is opened.")]
    public GameObject settingsButton;

    [Header("Other Screens and their buttons")]
    [Tooltip("The Upgrade Selection Screen")]
    public GameObject upgradeSelectionUI;
    [Tooltip("The first button to be selected when upgrade menu is opened.")]
    public GameObject upgradeMenuButton;
    [Tooltip("The Swap Upgrade Screen")]
    public GameObject swapUpgradeUI;
    [Tooltip("The first button to be selected when swap menu is opened.")]
    public GameObject swapButton;
    [Tooltip("The Shop: Buy Upgrade Screen")]
    public GameObject buyUpgradeUI;
    [Tooltip("The first button to be selected when the Shop: Buy Upgrade menu is opened.")]
    public GameObject buyUpgradeButton;
    [Tooltip("The Shop: Sell Upgrade Screen")]
    public GameObject sellUpgradeUI;
    [Tooltip("The first button to be selected when the Shop: Sell Upgrade menu is opened.")]
    public GameObject sellUpgradeButton;

    /// <summary>
    /// On enable, set first button to work (controller support),
    /// brings up main screen, and allows player to use cursor.
    /// </summary>
    private void OnEnable()
    {
        OpenScreen(mainPauseScreen);
        EventSystem.current.SetSelectedGameObject(pauseButton);
        Cursor.lockState = CursorLockMode.None;
        //Audio
        AudioManager.OpenUIAudio();
    }

    /// <summary>
    /// On disable, if there's no other UI menu open, go back to gameplay.
    /// If UI menus are open, set first selected button so that controller works.
    /// </summary>
    private void OnDisable()
    {
        if (!upgradeSelectionUI.activeInHierarchy && !swapUpgradeUI.activeInHierarchy
            && !buyUpgradeUI.activeInHierarchy && !sellUpgradeUI.activeInHierarchy)
        {
            Time.timeScale = 1.0f;
            Cursor.lockState = CursorLockMode.Locked;
            //Audio
            AudioManager.CloseUIAudio();
        }
        else if (upgradeSelectionUI.activeInHierarchy)
        {
            EventSystem.current.SetSelectedGameObject(upgradeMenuButton);
        }
        else if (swapUpgradeUI.activeInHierarchy)
        {
            EventSystem.current.SetSelectedGameObject(swapButton);
        }
        else if (buyUpgradeUI.activeInHierarchy)
        {
            EventSystem.current.SetSelectedGameObject(buyUpgradeButton);
        }
        else if (sellUpgradeUI.activeInHierarchy)
        {
            EventSystem.current.SetSelectedGameObject(sellUpgradeButton);
        }
    }

    /// <summary>
    /// Close all UI screens in the pause menu 
    /// </summary>
    public void CloseAllScreens()
    {
        mainPauseScreen.SetActive(false);
        settingsScreen.SetActive(false);
        compendiumScreen.SetActive(false);
        upgradeScreen.SetActive(false);
    }

    /// <summary>
    /// Open sub-menus in the pause menu and set first button for controller support.
    /// </summary>
    public void OpenScreen(GameObject screen)
    {
        CloseAllScreens();
        screen.SetActive(true);
        if (screen.name == "PauseMain")
        {
            EventSystem.current.SetSelectedGameObject(pauseButton);
        }
        else if (screen.name == "Compendium")
        {
            EventSystem.current.SetSelectedGameObject(compendiumButton);
        }
        else if (screen.name == "UpgradeLog")
        {
            EventSystem.current.SetSelectedGameObject(upgradeButton);
        }
        else if (screen.name == "Settings")
        {
            EventSystem.current.SetSelectedGameObject(settingsButton);
        }
    }

    /// <summary>
    /// Closes application.
    /// </summary>
    public void QuitToDesktop()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    /// <summary>
    /// Loads level, which is a unity scene.
    /// </summary>
    public void LoadLevel(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
