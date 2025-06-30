using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StaircaseDoor : MonoBehaviour
{
    [Header("Positioning Variables")]
    [Tooltip("Main Camera")]
    public Camera mainCamera;
    [Tooltip("Height offset")]
    public float heightOffset = 5;
    [Tooltip("Canvas to Write On")]
    public Canvas canvas;

    public GameObject interactText;
    public Hag oldHag;

    public float activationRange = 4;
    public string nextLevelName;

    bool canInteract = false;
    bool opened = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float dist = (oldHag.transform.position - transform.position).magnitude;
        if (PlayerController.instance.currentCharacter == oldHag && dist < activationRange)
        {
            interactText.SetActive(true);
            canInteract = true;

            Vector3 charPosition = new Vector3(transform.position.x, transform.position.y + heightOffset, transform.position.z);
            Vector3 screenPos = mainCamera.WorldToScreenPoint(charPosition);
            Vector2 canvasPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, screenPos, canvas.worldCamera, out canvasPos);
            interactText.GetComponent<RectTransform>().anchoredPosition = canvasPos;
        }
        else
        {
            interactText.SetActive(false);
            canInteract = false;
        }
    }

    public void OpenDoor()
    {
        if (canInteract && !opened)
        {
            opened = true;
            StartCoroutine(OpenDoorSequence());
        }
    }

    private IEnumerator OpenDoorSequence()
    {
        yield return new WaitForSeconds(3);
        SceneManager.LoadScene(nextLevelName);
    }

    public bool GetOpened()
    {
        return opened;
    }
}
