using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;


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
    [Tooltip("The Upgrade Selection Screen")]
    public GameObject upgradeSelectionUI;
    [Tooltip("The Swap Upgrade Screen")]
    public GameObject swapUpgradeUI;

    [Header("First Selected Buttons")]
    [Tooltip("The first button to be selected when Pause menu is opened.")]
    public GameObject pauseButton;
    [Tooltip("The first button to be selected when Compendium menu is opened.")]
    public GameObject compendiumButton;
    [Tooltip("The first button to be selected when Upgrade Log menu is opened.")]
    public GameObject upgradeButton;
    [Tooltip("The first button to be selected when Settings menu is opened.")]
    public GameObject settingsButton;

    private void OnEnable()
    {
        OpenScreen(mainPauseScreen);
        EventSystem.current.SetSelectedGameObject(pauseButton);
        Cursor.lockState = CursorLockMode.None;
    }

    private void OnDisable()
    {
        if (!upgradeSelectionUI.activeSelf && !swapUpgradeUI.activeSelf)
        {
            Time.timeScale = 1.0f;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    public void CloseAllScreens()
    {
        mainPauseScreen.SetActive(false);
        settingsScreen.SetActive(false);
        compendiumScreen.SetActive(false);
        upgradeScreen.SetActive(false);
        if (upgradeSelectionUI.activeSelf)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

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

    public void QuitToDesktop()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    public void LoadLevel(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
