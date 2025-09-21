using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance;
    [Header("Upgrades UI")]
    [Tooltip("Parent that holds the frame where the upgrades will go on the HUD.")]

    public Transform upgradeIconParent;

    /// <summary>
    /// It sets the instance of the HUDManager class,
    /// and allows only one instance of the class.
    /// </summary>
    private void Awake()
    {
        // Only one instance of HUDManager should be there
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Adding Upgrade Icon to the HUD
    /// </summary>
    public void AddUpgrade(DropData upgrade)
    {
        if (upgrade == null) return;

        GameObject upgradeObject = new GameObject(upgrade.GetDropName());
        upgradeObject.transform.SetParent(upgradeIconParent, false);

        Image upgradeIcon = upgradeObject.AddComponent<Image>();
        
        if (upgrade.GetIcon() != null)
        {
            upgradeIcon.sprite = upgrade.GetIcon();
        }
        else
        {
            Debug.LogWarning($"No sprite found for {upgrade.GetDropName()}");
        }
    }
}
