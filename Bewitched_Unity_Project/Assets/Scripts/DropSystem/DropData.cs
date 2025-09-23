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
}
