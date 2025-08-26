using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwingPivot : MonoBehaviour
{
    public GameObject swingPrefab;

    GameObject swingItem;

    public void Init(Character character, float dmg, float knockback)
    {
        swingItem = Instantiate(swingPrefab, transform);
        swingItem.GetComponent<BatHitbox>().Init(character, dmg, knockback);
    }

    public void OnDestroy()
    {
        if (swingItem)
        {
            Destroy(swingItem);
        }
    }
}
