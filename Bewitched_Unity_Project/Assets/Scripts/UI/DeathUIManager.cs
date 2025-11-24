using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

/// <summary>
/// Attached to the DeathUI gameObject, which will contain the stats to display from the game run.
/// Will also show the death animation of the player.
/// </summary>

public class DeathUIManager : MonoBehaviour
{
    [Header("Screens")]
    [Tooltip("The Death Screen")]
    public GameObject deathScreen;
    [Tooltip("The Stats Screen, parent of icons")]
    public GameObject statsScreen;

    [Header("Buttons")]
    [Tooltip("The first button to be selected when menu is opened.")]
    public GameObject firstButton;

    [Header("Upgrades Acquired")]
    [Tooltip("List of upgrades that the player has acquired.")]
    private List<DropData> playerUpgrades;
    [Tooltip("List of placeholder Images to be replaced by upgrade icons.")]
    private Image[] upgradeSlots;

    /// <summary>
    /// Shows the screen on enable, allows player to use cursor to navigate the screen
    /// </summary>
    private void OnEnable()
    {
        AudioManager.SubscribeCheckClick();
        if (deathScreen != null && deathScreen.activeInHierarchy == false)
        {
            deathScreen.SetActive(true);
            Debug.Log("script");
        }
        EventSystem.current.SetSelectedGameObject(firstButton);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;

        /// Get upgrade slot placeholders
        upgradeSlots = new Image[statsScreen.transform.childCount];
        for (int i = 0; i < statsScreen.transform.childCount; i++)
        {
            upgradeSlots[i] = statsScreen.transform.GetChild(i).GetComponent<Image>();
        }

        /// Stats (upgrades)
        if (DropSystem.Instance != null)
        {
            playerUpgrades = DropSystem.Instance.playerUpgrades;
            UpdateStats();
        }
        else
        {
            // if no upgrades collected, show empty slots.
            Debug.LogWarning("DropSystem.Instance not found.");
        }
    }

    /// <summary>
    /// Stops checking for UI button clicks when Quit Menu is pressed
    /// </summary>
    void OnDisable()
    {
        AudioManager.UnsubscribeCheckClick();
    }

    /// <summary>
    /// Updates the placeholder upgrade slots 
    /// with the player's acquired upgrades they got in the run.
    /// </summary>
    private void UpdateStats()
    {
        Dictionary<string, (DropData upgrade, int count)> groupedUpgrades = new();
        foreach (var upgrade in playerUpgrades)
        {
            if (upgrade == null) continue;

            string name = upgrade.GetDropName();
            if (!groupedUpgrades.ContainsKey(name))
                groupedUpgrades[name] = (upgrade, 1);
            else
                groupedUpgrades[name] = (groupedUpgrades[name].upgrade, groupedUpgrades[name].count + 1);
        }

        // Stack exact upgrades
        int slotIndex = 0;
        foreach (var kvp in groupedUpgrades)
        {
            if (slotIndex >= upgradeSlots.Length)
                break;

            Image slot = upgradeSlots[slotIndex];
            DropData upgrade = kvp.Value.upgrade;
            int count = kvp.Value.count;

            
            // First icon
            Transform main = slot.transform.Find("MainIcon");
            if (main == null)
            {
                GameObject mainObj = new GameObject("MainIcon", typeof(RectTransform), typeof(Image));
                mainObj.transform.SetParent(slot.transform, false);
                main = mainObj.transform;
            }

            Image mainIcon = main.GetComponent<Image>();
            mainIcon.sprite = upgrade.GetIcon();
            mainIcon.enabled = true;

            // First icon renders on top
            main.SetAsLastSibling();
            

            // Stack exact icons under
            for (int i = 1; i < count; i++)
            {
                GameObject clone = new GameObject(upgrade.GetDropName() + "_stack_" + i, typeof(RectTransform));
                clone.transform.SetParent(slot.transform, false);

                Image cloneImage = clone.AddComponent<Image>();
                cloneImage.sprite = upgrade.GetIcon();
                cloneImage.preserveAspect = true;

                RectTransform rt = clone.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);

                float offset = 15f;
                rt.anchoredPosition = new Vector2(0, -i * offset);

                // behind the first icon
                clone.transform.SetAsFirstSibling();
            }

            slotIndex++;
        }
    }
}
