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
    [Tooltip("The Enemies Compendium Screen")]
    public GameObject enemiesScreen;
    [Tooltip("The Upgrade System Screen")]
    public GameObject upgradSystemScreen;

    [Header("First Selected Buttons")]
    [Tooltip("The first button to be selected when Pause menu is opened.")]
    public GameObject mainButton;
    [Tooltip("The first button to be selected when Settings menu is opened.")]
    public GameObject settingsButton;
    [Tooltip("The first button to be selected when Compendium menu is opened.")]
    public GameObject compendiumButton;
    [Tooltip("The first button to be selected when Upgrade Log menu is opened.")]
    public GameObject upgradeButton;
    [Tooltip("the first button to be selected when the Enemies menu is opened")]
    public GameObject enemiesButton;
    [Tooltip("The first button to be selected when the Upgrade Systems menu is opened")]
    public GameObject upgradSystemButton;
    


    /// <summary>
    /// On enable, set first button to work (controller support),
    /// brings up main screen, and allows player to use cursor.
    /// </summary>
    private void OnEnable()
    {
        OpenScreen(mainScreen);
        EventSystem.current.SetSelectedGameObject(mainButton);
        Cursor.lockState = CursorLockMode.None;
        AudioManager.SubscribeCheckClick();
    }

    /// <summary>
    /// On disable, if there's no other UI menu open, go back to gameplay.
    /// </summary>
    private void OnDisable()
    {
        Time.timeScale = 1.0f;
        Cursor.lockState = CursorLockMode.Locked;
        AudioManager.UnsubscribeCheckClick();
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
        enemiesScreen.SetActive(false);
        upgradSystemScreen.SetActive(false);
    }

    /// <summary>
    /// Open sub-menus in the main menu and set first button for controller support.
    /// </summary>
    public void OpenScreen(GameObject screen)
    {
        CloseAllScreens();
        screen.SetActive(true);
        if (screen == mainScreen)
        {
            EventSystem.current.SetSelectedGameObject(mainButton);
        }
        else if (screen == compendiumScreen)
        {
            EventSystem.current.SetSelectedGameObject(compendiumButton);
        }
        else if (screen == upgradeScreen)
        {
            EventSystem.current.SetSelectedGameObject(upgradeButton);
        }
        else if (screen == settingsScreen)
        {
            EventSystem.current.SetSelectedGameObject(settingsButton);
        }
        else if (screen == enemiesScreen)
        {
            EventSystem.current.SetSelectedGameObject(enemiesButton);
        }
        else if (screen == upgradSystemScreen)
        {
            EventSystem.current.SetSelectedGameObject(upgradSystemButton);
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
        if (SoulSystem.Instance != null)
        {
            SoulSystem.Instance.ResetSouls();
        }
    }
}
