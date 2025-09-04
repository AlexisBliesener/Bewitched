using UnityEngine;

public class UpgradeItemBase : MonoBehaviour
{
    [Header("Upgrade Info")]
    [SerializeField]
    private string upgradeName;
    [SerializeField]
    private string description;
    [SerializeField]
    private Sprite icon;
    [SerializeField]
    private RarityData rarity;

    public string GetUpgradeName() => upgradeName;
    public string GetDescription() => description;

    public RarityData GetRarity() => rarity;

    public virtual void Activate()
    {
        Debug.Log("Activating Upgrade for " + upgradeName);
    }
}