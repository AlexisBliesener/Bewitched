using UnityEngine;
using UnityEngine.Playables;

public class BreakWallMoment : MonoBehaviour
{
    
    [SerializeField, Tooltip("diraction for the cut scene")]
    private PlayableDirector director;
    [SerializeField, Tooltip("The vfx to play when all goblins are killed, and it's waiting for the player to breaj the wall")]
    private GameObject flashingVFX;
    private bool isBreakWallActivated = true;
    private void Start()
    {
        if (director == null)
        {
            Debug.LogWarning("Director is null on BreakWallMoment");
        }
        if (flashingVFX == null)
        {
            Debug.LogWarning("Flashing VFX is null on BreakWallMoment");
        }
        else
        {
            flashingVFX.SetActive(true);
        }

    }
    /// <summary>
    /// Subscribe to the cut scene director stopped event
    /// </summary>
    void OnEnable()
    {
        if (director != null)
        {
            director.stopped += OnCutsceneFinished;
            director.played += OnCutSceneStarted;
        }
    }
    /// <summary>
    /// Unsubscribe from the cut scene director stopped event
    /// </summary>
    void OnDisable()
    {
        if (director != null)
        {
            director.stopped -= OnCutsceneFinished;
            director.played -= OnCutSceneStarted;
        }
    }
    /// <summary>
    /// This is called when the cut scene is finished 
    /// </summary>
    private void OnCutsceneFinished(PlayableDirector directors)
    {
        // Kill ogre? 
        PossessionAbility.instance.SetCanLeavePossession(true);
        PlayerController.instance.currentCharacter.gameObject.SetActive(true);
        PlayerController.instance.currentCharacter.health.KillEnemy();
        PlayerController.instance.SetAllowMovement(true);
        DestroyImmediate(director.gameObject);
        Destroy(this);
    }
    /// <summary>
    /// This is called when the cut scene is started
    /// </summary>
    private void OnCutSceneStarted(PlayableDirector director)
    {
        // Hide the flashing VFX
        flashingVFX.SetActive(false);
        isBreakWallActivated = false;
        NarrativeStatePopup.instance?.HideNarrativePanel(NarrativeStatePopup.NarrativeState.BreakWallActivated);
        PlayerController.instance.SetAllowMovement(false);
        PlayerController.instance.currentCharacter.gameObject.SetActive(false);
        // we will kill all the enemies on the arena 
        foreach (Enemy enemy in RoomSystem.Instance.GetActiveRoomController().roomEnemies)
        {
            if (enemy == null || enemy.gameObject == PlayerController.instance.currentCharacter.gameObject) continue;
            enemy.health.KillEnemy();
        }
        //End the level music
        if(AudioManager.manager != null)
        {
            AudioManager.ChangeMusicParameter("End", "True");
        }
        else
        {
            Debug.LogWarning("Audio Manager instance is not set!");
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<DefaultHitbox>() != null && this.enabled)
        {
            director.gameObject.SetActive(true);
        }
    }

    private void Update()
    {
        if (isBreakWallActivated)
        {
            // To keep showing all the texts in the list 
            NarrativeStatePopup.instance?.ShowNarrativePanel(NarrativeStatePopup.NarrativeState.BreakWallActivated);
        }
    }
}
