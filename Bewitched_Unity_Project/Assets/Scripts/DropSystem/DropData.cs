using UnityEngine;
/// <summary>
/// DropData is a class that holds the drop information.
/// It has a display name, description, icon, rarity, and drop script.
/// The rarity is selected from a dropdown populated by DropSystem's availableRarities list.
/// </summary>
[System.Serializable]
public class DropData
{
    [Tooltip("The name of the drop")]
    [SerializeField] private string dropName;
    [Tooltip("The description of the drop")]
    [SerializeField] private string description;
    [Tooltip("The ID of the drop")]
    [SerializeField] private string dropID;
    [Tooltip("The icon of the drop")]
    [SerializeField] private Sprite icon;
    [Tooltip("The rarity of the drop")]
    [SerializeField] private int rarityIndex;
    [Tooltip("The script that will be used to activate the drop (must implement IDrop)")]
    [SerializeField] private GameObject dropScript;
    [Tooltip("The IDrop script of the drop to avoid GetComponent<IDrop>() calls which is a little bit expensive")]
    private IDrop dropScriptComponent;
    [SerializeField,Tooltip("The amount to buy this drop")]
    private int buyAmount;
    [SerializeField,Tooltip("The amount to sell this drop")]
    private int sellAmount;
    /// <summary>
    /// Get the amount to buy this drop
    /// </summary>
    public int GetBuyAmount() => buyAmount;
    /// <summary>
    /// Get the amount to sell this drop
    /// </summary>
    public int GetSellAmount() => sellAmount;
    /// <summary>
    /// Set the amount to buy this drop
    /// </summary>
    public void SetBuyAmount(int val) => buyAmount = val;
    /// <summary>
    /// Set the amount to sell this drop
    /// </summary>
    public void SetSellAmount(int val) => sellAmount = val;
    // <summary> Get the name of the drop </summary>
    public string GetDropName() => dropName;
    // <summary> Get the description of the drop </summary>
    public string GetDescription() => description;
    // <summary> Get the ID of the drop </summary>
    public string GetID() => dropID;
    // <summary> Get the icon of the drop </summary>
    public Sprite GetIcon() => icon;
    // <summary> Set the name of the drop </summary>
    public void SetDropName(string val) => dropName = val;
    // <summary> Set the description of the drop </summary>
    public void SetDescription(string val) => description = val;
    // <summary> Set the ID of the drop </summary>
    public void SetID(string val) => dropID = val;
    // <summary> Set the icon of the drop </summary>
    public void SetIcon(Sprite val) => icon = val;
    // <summary> Get the rarity index for the drop </summary>
    public int GetRarityIndex() => rarityIndex;
    // <summary> Set the rarity index for the drop </summary>
    public void SetRarityIndex(int val) => rarityIndex = val;
    // <summary> Get the drop script </summary>
    public GameObject GetDropScript() => dropScript;
    // <summary> Set the drop script </summary>
    public void SetDropScript(GameObject val) => dropScript = val;
    public bool CanStackWith(DropData other) => dropName == other.dropName;
    /// <summary>
    /// Get the drop script component
    /// </summary>
    public IDrop GetDropScriptComponent()
    {
        if (dropScriptComponent == null)
        {
            dropScriptComponent = GetDropScript()?.GetComponent<IDrop>();
        }
        if (dropScriptComponent == null)
        {
            Debug.LogError($"No drop script found for drop {GetDropName()}!!");
        }
        return dropScriptComponent;
    }
    /// <summary>
    /// Activate the drop
    /// </summary>
    public void Activate()
    {
        GetDropScriptComponent().Activate();
    }
    /// <summary>
    /// Deactivate the drop
    /// </summary>
    public void Deactivate()
    {
        GetDropScriptComponent().Deactivate();
    }
    /// <summary>
    /// Reset the stack count of the drop
    /// </summary>
    public void ResetStack()
    {
        GetDropScriptComponent().stackNum = 0;
    }
    /// Helper function to get the stack count of a drop
    /// </summary>
    /// <returns>The stack count of the drop</returns>
    public int GetStackCount()
    {
        return GetDropScriptComponent().stackNum;
    }
    /// <summary>
    /// Helper function to increase the stack count of a drop
    /// </summary>
    /// <returns>The new stack count of the drop</returns>
    public int IncreaseStack()
    {
        GetDropScriptComponent().stackNum++;
        return GetDropScriptComponent().stackNum;
    }
    /// <summary>
    /// Helper function to decrease the stack count of a drop
    /// </summary>
    /// <returns>The new stack count of the drop</returns>
    public int DecreaseStack()
    {
        GetDropScriptComponent().stackNum--;
        return GetDropScriptComponent().stackNum;
    }
}
