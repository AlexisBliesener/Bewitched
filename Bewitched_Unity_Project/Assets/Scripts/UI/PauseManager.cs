using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

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

    private void OnEnable()
    {
        OpenScreen(mainPauseScreen);
    }

    public void CloseAllScreens()
    {
        mainPauseScreen.SetActive(false);
        settingsScreen.SetActive(false);
        compendiumScreen.SetActive(false);
        upgradeScreen.SetActive(false);
    }

    public void OpenScreen(GameObject screen)
    {
        CloseAllScreens();
        screen.SetActive(true);
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
