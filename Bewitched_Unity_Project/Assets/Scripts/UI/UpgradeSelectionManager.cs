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


    /// <summary>
    /// Shows the screen on enable, allows player to use cursor to navigate the screen
    /// </summary>
    private void OnEnable()
    {
        upgradeSelectionScreen.SetActive(true);
        Cursor.lockState = CursorLockMode.None;

        //List <GameObject> ButtonList = new List<GameObject>();
        foreach (Transform child in upgradeSelectionScreen.transform.GetComponentInChildren<Transform>())
        {
            //if (child.gameObject.name == "SalvageButton")
            TMP_Text buttonText = child.GetComponentInChildren<TMP_Text>(true);
            if (buttonText != null)
            {
                Debug.Log(buttonText.text);
                //Debug.Log(DropSystem.option1?.GetDropName());
            }
        }

    }

    /// <summary>
    /// Disables Screen
    /// </summary>
    private void OnDisable()
    {
        Time.timeScale = 1.0f;
        Cursor.lockState = CursorLockMode.Locked;
    }

    /// <summary>
    /// Closes screen, call if need be
    /// </summary>
    public void CloseScreen()
    {
        this.gameObject.SetActive(false);
    }
}
