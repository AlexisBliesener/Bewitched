using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

/// <summary>
/// This is a class for storing status effects for attacks
/// It will hold instructions for how to apply certain status effects (such as knockback)
/// and carry out applying the effects
/// </summary>
[System.Serializable]
public class AttackStatusEffects : MonoBehaviour
{
    const string FILE_ENDING = ".json";
    [SerializeField] string attackName;

    public enum KnockbackType // For handling knockback
    {
        BasicForward,
        Impact,
        Swing,
        Bash
    };

    [Header("Knockback Values")]

    // For when knockback is constant
    [SerializeField] float knockbackAmount = 0;

    // For when knockback is in a range
    [SerializeField] private float knockbackMinimum = 0;
    [SerializeField] private float knockbackMaximum = 0;
    [SerializeField] private float knockbackRange = 0;

    // The knockback type this is
    [SerializeField] private KnockbackType knockbackType = KnockbackType.BasicForward;

    [Header("Time Stop Values")]

    [SerializeField] float timeStopDuration = 0;

    #region Saving/Loading

    /// <summary>
    /// Saves this instance as a json string
    /// </summary>
    /// <returns> A JSON string </returns>
    public string SaveToJson()
    {
        string statusStr = JsonUtility.ToJson(this, true);

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif

        return statusStr;
    }

    /// <summary>
    /// Loads values from a JSON string into this instance
    /// </summary>
    /// <param name="str"> JSON string to set values with </param>
    public void LoadFromJson(string str)
    {
        JsonUtility.FromJsonOverwrite(str, this);

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }

    #endregion

    #region Getters

    /// <summary>
    /// Gets the knockback range
    /// </summary>
    /// <returns> The knockback range </returns>
    public float GetKnockbackRange()
    {
        return knockbackRange;
    }

    #endregion

    /// <summary>
    /// Applies the knockback
    /// </summary>
    /// <param name="user"> User of the attack </param>
    /// <param name="character"> Character attack is being used on </param>
    /// <param name="hitbox"> Hitbox the attack is using </param>
    public void ApplyKnockback(Character user, Character character, DefaultHitbox hitbox)
    {
        if (knockbackType == KnockbackType.Bash)
        {
            Vector3 knockbackDirection = user.GetCurrentSpeedVector() + (character.transform.position - hitbox.transform.position).normalized;

            character.GetComponent<KnockbackControl>().AddImpact(knockbackDirection, knockbackAmount);
        }
        else if (knockbackType == KnockbackType.Swing)
        {
            float knockbackAngle = hitbox.transform.parent.rotation.eulerAngles.y - 90;
            Vector3 knockbackDirection = new Vector3(Mathf.Sin(knockbackAngle * Mathf.Deg2Rad), 0, Mathf.Cos(knockbackAngle * Mathf.Deg2Rad));

            character.GetComponent<KnockbackControl>().AddImpact(knockbackDirection, knockbackAmount);
        }
        else if (knockbackType == KnockbackType.Impact)
        {
            Vector3 knockbackDirection = character.transform.position - hitbox.transform.position;
            float distance = knockbackDirection.magnitude;

            float knockbackAmt = knockbackMaximum - Mathf.Lerp(knockbackMinimum, knockbackMaximum, distance / knockbackRange);


            character.GetComponent<KnockbackControl>().AddImpact(knockbackDirection.normalized, knockbackAmt);
        }
        else
        {
            character.GetComponent<KnockbackControl>().AddImpact(hitbox.transform.forward.normalized, knockbackAmount);
        }
    }

    /// <summary>
    /// Applies the time stop
    /// </summary>
    /// <param name="user"> User of the attack </param>
    /// <param name="character"> Character attack is being used on </param>
    /// <param name="hitbox"> Hitbox the attack is using </param>
    public void ApplyTimeStop(Character user, Character character, DefaultHitbox hitbox)
    {
        Time.timeScale = 0;
        user.StartCoroutine(user.StartTime(timeStopDuration));
    }

    /// <summary>
    /// Applies the status effects
    /// </summary>
    /// <param name="user"> User of the attack </param>
    /// <param name="character"> Character attack is being used on </param>
    /// <param name="hitbox"> Hitbox the attack is using </param>
    public void ApplyStatusEffects(Character user, Character character, DefaultHitbox hitbox)
    {
        ApplyKnockback(user, character, hitbox);
        ApplyTimeStop(user, character, hitbox);
    }
}
