using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This has to be attached to the UpgradesHUD gameObject,
/// which contains the UpgradesPanel gameObject,
/// which will be the parent of the upgrades that the player collects.
/// Adds acquired upgrades to the HUD.
/// </summary>
public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance;

    [Header("Upgrades UI")]
    [Tooltip("Parent that holds the frame where the upgrades will go on the HUD.")]
    public Transform upgradeIconParent;

    [Tooltip("Number of unique upgrades / number of stacks")]
    public int uniqueUpgradesCount => upgradeDict.Count;
    [Tooltip("See if upgrade is already acquired by the player")]
    public bool HasExactUpgrade(string id) => upgradeDict.ContainsKey(id);

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

        // Make new stack for each new upgrade
        string upgradeID = upgrade.GetID();
        if (!upgradeDict.ContainsKey(upgradeID))
        {
            GameObject stack = new GameObject(upgrade.GetDropName() + "_Stack", typeof(RectTransform));
            stack.transform.SetParent(upgradeIconParent, false);

            upgradeDict[upgradeID] = stack.transform;
        }

        GameObject upgradeObject = new GameObject(upgrade.GetDropName(), typeof(RectTransform));
        upgradeObject.transform.SetParent(upgradeDict[upgradeID], false);

        // Add icons of upgrades
        Image upgradeIcon = upgradeObject.AddComponent<Image>();

        if (upgrade.GetIcon() != null)
        {
            upgradeIcon.sprite = upgrade.GetIcon();
            upgradeIcon.preserveAspect = true;
        }
        else
        {
            Debug.LogWarning($"No sprite found for {upgrade.GetDropName()}");
        }

        // resizing the icons
        LayoutElement layout = upgradeObject.AddComponent<LayoutElement>();
        layout.preferredWidth = 100;
        layout.preferredHeight = 100;

        // Stacking 
        RectTransform rt = upgradeObject.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);

        int count = upgradeDict[upgradeID].childCount - 1;
        float overlap = 20f;

        rt.anchoredPosition = new Vector2(0, -count * overlap);
        upgradeObject.transform.SetAsFirstSibling();
    }
}
