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

    [ContextMenu("Save to JSON")]
    public string SaveToJson()
    {
        string statusStr = JsonUtility.ToJson(this, true);

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif

        return statusStr;
    }

    [ContextMenu("Load From JSON")]
    public void LoadFromJson(string str)
    {
        JsonUtility.FromJsonOverwrite(str, this);

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }

    #endregion

    #region Getters

    public float GetKnockbackRange()
    {
        return knockbackRange;
    }

    #endregion

    public void AddKnockback(KnockbackType type, float amt = 0, float minAmt = 0, float maxAmt = 0, float range = 0)
    {
        knockbackType = type;
        knockbackAmount = amt;
        knockbackMinimum = minAmt;
        knockbackMaximum = maxAmt;
        knockbackRange = range;
    }

    public void AddTimeStop(float duration = 0)
    {
        timeStopDuration = duration;
    }

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

    public void ApplyTimeStop(Character user, Character character, DefaultHitbox hitbox)
    {
        Time.timeScale = 0;
        user.StartCoroutine(user.StartTime(timeStopDuration));
    }

    public void ApplyStatusEffects(Character user, Character character, DefaultHitbox hitbox)
    {
        ApplyKnockback(user, character, hitbox);
        ApplyTimeStop(user, character, hitbox);
    }
}
