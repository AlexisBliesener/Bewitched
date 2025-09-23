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

    [Tooltip("Tracking by DropData ID")]
    private Dictionary<string, Transform> upgradeDict = new Dictionary<string, Transform>();

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

        string upgradeID = upgrade.GetID();
        if (!upgradeDict.ContainsKey(upgradeID))
        {
            GameObject stack = new GameObject(upgrade.GetDropName() + "_Stack");
            stack.transform.SetParent(upgradeIconParent, false);

            VerticalLayoutGroup verticalStack = stack.AddComponent<VerticalLayoutGroup>();
            verticalStack.childControlWidth = true;
            verticalStack.childControlHeight = true;
            verticalStack.childForceExpandWidth = false;
            verticalStack.childForceExpandHeight = false;

            upgradeDict[upgradeID] = stack.transform;
        }


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
