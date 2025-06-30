using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ButtonTextSizer : MonoBehaviour
{
    public GraphicRaycaster graphicRaycaster;
    public TextMeshProUGUI buttonText;

    public float defaultFontSize = 24;
    public float expandedFontSize = 30;
    public float duration = 0.1f;

    private float selectedTime = 0;
    private float unselectedTime = 0;

    private bool selected = false;
    private bool changing = false;

    private float currentFontSize;

    PointerEventData pointerEventData;
    EventSystem eventSystem;

    private void Start()
    {
        eventSystem = EventSystem.current;
        currentFontSize = defaultFontSize;
    }

    void Update()
    {
        if (MouseOverButton())
        {
            if (!selected && !changing)
            {
                selected = true;
                StartCoroutine(ExpandText());
                changing = true;
            }
        }
        else
        {
            if (selected && !changing)
            {
                selected = false;
                StartCoroutine(ShrinkText());
                changing = true;
            }
        }
    }

    bool MouseOverButton()
    {
        pointerEventData = new PointerEventData(eventSystem)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        graphicRaycaster.Raycast(pointerEventData, results);

        if (results.Count > 0)
        {
            for (int i = 0; i < results.Count; i++)
            {
                if (results[i].gameObject == gameObject)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private IEnumerator ExpandText()
    {
        while (selectedTime < duration)
        {
            selectedTime += Time.unscaledDeltaTime;
            currentFontSize = Mathf.Lerp(defaultFontSize, expandedFontSize, selectedTime / duration);
            buttonText.fontSize = currentFontSize;
            yield return null;
        }

        selectedTime = 0;
        changing = false;
    }

    private IEnumerator ShrinkText()
    {
        while (unselectedTime < duration)
        {
            unselectedTime += Time.unscaledDeltaTime;
            currentFontSize = expandedFontSize - Mathf.Lerp(defaultFontSize, expandedFontSize, unselectedTime / duration) + defaultFontSize;
            buttonText.fontSize = currentFontSize;
            yield return null;
        }

        unselectedTime = 0;
        changing = false;
    }
}
