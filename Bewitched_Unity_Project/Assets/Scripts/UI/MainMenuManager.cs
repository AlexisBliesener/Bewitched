using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

/// <summary>
/// This has to be attached to the MenuUI gameObject,
/// which contains sub-menus for Settings, the Compendium, and Upgrades.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("Screens")]
    [Tooltip("The Main Screen")]
    public GameObject mainScreen;
    [Tooltip("The Settings Screen")]
    public GameObject settingsScreen;
    [Tooltip("The Compendium Screen")]
    public GameObject compendiumScreen;
    [Tooltip("The Upgrade Log Screen")]
    public GameObject upgradeScreen;

    [Header("First Selected Buttons")]
    [Tooltip("The first button to be selected when Pause menu is opened.")]
    public GameObject mainButton;
    [Tooltip("The first button to be selected when Settings menu is opened.")]
    public GameObject settingsButton;
    [Tooltip("The first button to be selected when Compendium menu is opened.")]
    public GameObject compendiumButton;
    [Tooltip("The first button to be selected when Upgrade Log menu is opened.")]
    public GameObject upgradeButton;
    


    /// <summary>
    /// On enable, set first button to work (controller support),
    /// brings up main screen, and allows player to use cursor.
    /// </summary>
    private void OnEnable()
    {
        OpenScreen(mainScreen);
        EventSystem.current.SetSelectedGameObject(mainButton);
        Cursor.lockState = CursorLockMode.None;
        //Audio
        AudioManager.OpenUIAudio();
    }

    /// <summary>
    /// On disable, if there's no other UI menu open, go back to gameplay.
    /// </summary>
    private void OnDisable()
    {
        Time.timeScale = 1.0f;
        Cursor.lockState = CursorLockMode.Locked;
        //Audio
        if (AudioManager.manager) AudioManager.CloseUIAudio();
    }

    /// <summary>
    /// Close all UI screens in the main menu 
    /// </summary>
    public void CloseAllScreens()
    {
        mainScreen.SetActive(false);
        settingsScreen.SetActive(false);
        compendiumScreen.SetActive(false);
        upgradeScreen.SetActive(false);
    }

    /// <summary>
    /// Open sub-menus in the main menu and set first button for controller support.
    /// </summary>
    public void OpenScreen(GameObject screen)
    {
        CloseAllScreens();
        screen.SetActive(true);
        if (screen.name == "MenuMain")
        {
            EventSystem.current.SetSelectedGameObject(mainButton);
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
