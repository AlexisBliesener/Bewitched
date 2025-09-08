using UnityEngine;
/// <summary>
/// DropItemBase is a base class for all the drops in the game.
/// It has some basic properties and a virtual function to activate the drop.
/// </summary>
public class DropItemBase : MonoBehaviour
{
    [Header("Drop Info")]
    [Tooltip("The name of the drop")]
    [SerializeField] private string dropName;
    [Tooltip("The description of the drop")]
    [SerializeField] private string description;
    [Tooltip("The icon of the drop")]
    [SerializeField] private Sprite icon;
    [Tooltip("The rarity of the drop")]
    [SerializeField] private ItemRarity rarity;
    // <summary> Get the name of the drop </summary>
    public string GetDropName() => dropName;
    // <summary> Get the description of the drop </summary>
    public string GetDescription() => description;
    // <summary> Get the icon of the drop </summary>
    public Sprite GetIcon() => icon;
    // <summary> Set the name of the drop </summary>
    public void SetDropName(string val) => dropName = val;
    // <summary> Set the description of the drop </summary>
    public void SetDescription(string val) => description = val;
    // <summary> Set the icon of the drop </summary>
    public void SetIcon(Sprite val) => icon = val;
    // <summary> Set the rarity of the drop </summary>
    public void SetRarity(ItemRarity val) => rarity = val;
    // <summary> Get the rarity of the drop </summary>
    public ItemRarity GetRarity() => rarity;
    /// <summary>
    /// This is a virtual function that will be called when the player picks up the drop.
    /// It will be used to activate the drop.
    /// For example, if the drop is a health potion, it will increase the player's health.
    /// </summary>
    public virtual void Activate()
    {
        
    }
}