using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

public class UpgradeSelectionManager : MonoBehaviour
{
    [Header("Screens")]
    [Tooltip("The Upgrade Selection Screen")]
    public GameObject upgradeSelectionScreen;

    private void OnEnable()
    {
        upgradeSelectionScreen.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
    }

    private void OnDisable()
    {
        Time.timeScale = 1.0f;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void CloseScreen()
    {
        upgradeSelectionScreen.SetActive(false);
    }

    public void OnClick()
    {
        
    }
}
