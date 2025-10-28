using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class BreakWallMoment : MonoBehaviour
{
    
    [SerializeField, Tooltip("diraction for the cut scene")]
    private PlayableDirector director;

    private void Start()
    {
        if (director == null)
        {
            Debug.LogWarning("Director is null on BreakWallMoment");
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
    private void OnCutsceneFinished(PlayableDirector director)
    {
        PlayerController.instance.SetAllowMovement(true);
        PlayerController.instance.currentCharacter.gameObject.SetActive(true);
        // Kill ogre? 
        PlayerController.instance.currentCharacter.health.SubHealth(PlayerController.instance.currentCharacter.health.GetMaxHealth());
        Destroy(director.gameObject);
    }
    /// <summary>
    /// This is called when the cut scene is started
    /// </summary>
    private void OnCutSceneStarted(PlayableDirector director)
    {
        PlayerController.instance.SetAllowMovement(false);
        PlayerController.instance.currentCharacter.gameObject.SetActive(false);
        // we will kill all the enemies on the arena 
        foreach (GameObject enemyGameObject in RoomSystem.Instance.GetActiveRoomController().roomEnemies)
        {
            Enemy enemy = enemyGameObject.GetComponent<Enemy>();
            enemy.health.SetCurrentHealth(0);
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<DefaultHitbox>() != null)
        {
            director.gameObject.SetActive(true);
        }
    }
}
