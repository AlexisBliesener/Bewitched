using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CooldownDisplay : MonoBehaviour
{
    public TextMeshProUGUI cooldownText;

    public GameObject onCooldownCover;

    public Image abilityImage;

    private bool ableToUse = true;

    public void SetCooldownCover(float timeDif)
    {
        if (timeDif > 0 || !ableToUse)
        {
            if (!onCooldownCover.activeInHierarchy)
            {
                onCooldownCover.SetActive(true);
            }
            if (!ableToUse)
            {
                cooldownText.text = "";
            }
            else
            {
                cooldownText.text = Mathf.Floor(timeDif).ToString();
            }
        }
        else
        {
            if (onCooldownCover.activeInHierarchy)
            {
                onCooldownCover.SetActive(false);
            }
        }
    }

    public void SwitchAbilityImage(Image image)
    {
        abilityImage = image;
    }

    public void SetAbleToUse(bool val)
    {
        ableToUse = val;
    }
}
