using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static PlayerController;

public class PossessionAbility : MonoBehaviour
{
    public static PlayerControlHandler CharacterControlChangeEvent;

    [Tooltip("The game virutal camera")]
    public CinemachineVirtualCamera virtualCam;
    [Tooltip("The cooldown in seconds that the player must wait in witch state before being able to possess again")]
    [SerializeField] float possessionCooldown = 10;
    [SerializeField, Tooltip("The max distance away from the camera that the player can possess")]
    private float maxPossessionDistance;
    [Tooltip("Possession Orb Cooldown UI")]
    public CooldownDisplay possessionCooldownDisplay;
    [Tooltip("Time to fill up enemy explosion")]
    public float enemyExplosionTime = 10;
    [Tooltip("Rate at which life is drained")]
    public float lifeDrainCoefficient = 2;
    [Tooltip("The currently controlled character's health bar")]
    public GameObject secondaryHealthBar;

    [Tooltip("The time possession was left")]
    private float timePossessionLastLeft = Mathf.NegativeInfinity;

    private bool possessHeld = false;
    private bool leaveHeld = false;

    private float timePossessing;

    [SerializeField]
    private Hag oldHag;
    private Character currentCharacter;

    private void Awake()
    {
        currentCharacter = oldHag;
        CharacterControlChangeEvent += SwitchCharacter;
    }

    void OnDisable()
    {
        CharacterControlChangeEvent -= SwitchCharacter;
    }


    public void Possess(InputAction.CallbackContext context)
    {
        if (currentCharacter == oldHag)
        {
            if (context.started)
            {
                if (Time.time - timePossessionLastLeft >= possessionCooldown)
                {
                    timePossessionLastLeft = Time.time;
                    currentCharacter.AnimatePossess();
                    FirePossession();
                }
            }
            else
            {
                return;
            }
        }
        else
        {
            if (context.started)
            {
                timePossessionLastLeft = Time.time;
                StartCoroutine(ExplodeEnemy());
            }
            else
            {
                return;
            }
        }
    }

    private void FirePossession()
    {
        Ray possessionRay = new Ray(virtualCam.transform.position, virtualCam.transform.forward);
        RaycastHit hitInfo;
        if (Physics.Raycast(possessionRay, out hitInfo, maxPossessionDistance))
        {
            if (hitInfo.collider.gameObject.CompareTag("Enemy"))
            {
                Debug.Log(hitInfo.collider.gameObject.name + " Enemy hit");
                Character characterHit = hitInfo.collider.gameObject.GetComponent<Character>();
                CharacterControlChangeEvent?.Invoke(characterHit);
                characterHit.SetControlled(true);
            }
            else
            {
                Debug.Log("fired but not hit");
            }
        }
    }

    private void Update()
    {
        // HandleHeldAbilites
        if (Time.time - timePossessionLastLeft >= possessionCooldown && possessHeld)
        {
            timePossessionLastLeft = Time.time;
            currentCharacter.AnimatePossess();
            FirePossession();
        }

        if (leaveHeld)
        {
            StartCoroutine(ExplodeEnemy());
        }

        // handle Cooldown UI
        if (currentCharacter == oldHag)
        {
            possessionCooldownDisplay.SetAbleToUse(true);
        }
        else
        {
            possessionCooldownDisplay.SetAbleToUse(false);
        }

        possessionCooldownDisplay.SetCooldownCover(possessionCooldown - (Time.time - timePossessionLastLeft));
    }

    private IEnumerator ExplodeEnemy()
    {
        yield return null; // wait one frame

        if (currentCharacter != oldHag)
        {
            if (Time.time - timePossessing > enemyExplosionTime)
            {
                currentCharacter.Explode();
                currentCharacter.Die();
                // Apply shunt damage
            }

            // respawn old Hag

            oldHag.transform.position = currentCharacter.transform.position;
            oldHag.transform.rotation = currentCharacter.transform.rotation;
            currentCharacter.SetControlled(false);
            CharacterControlChangeEvent?.Invoke(oldHag);
        }
    }

    public void SwitchCharacter(Character newCharacter)
    {

        Debug.Log("new char " + newCharacter);
        PlayerController.instance.SeteCharacterController(newCharacter.GetComponent<CharacterController>());
        HealthController hagHealth = oldHag.GetComponent<HealthController>();
        HealthController newHealth = newCharacter.GetComponent<HealthController>();
        if (newCharacter == oldHag)
        {
            // This means we are switching back to the hag
            if (hagHealth != null)
            {
                hagHealth.SetDecay(0f); // Hag does not decay
                hagHealth.EnableUpdateModel(true);
            }
            if (secondaryHealthBar != null)
            {
                oldHag.gameObject.SetActive(true);
                secondaryHealthBar.SetActive(false);
            }
            currentCharacter.SetTeamID(2);

            PlayerController.instance.SetAllowMovement(true);
            //Might need to change this once I get to enemy specific death sounds

            // I have commented this out for now as it was causing issues with fmod and that caused a problem with switching characters


            // if(AudioManager.TryGetReference("LeaveBody",out EventReference evRef)){
            //         EventInstance ev = RuntimeManager.CreateInstance(evRef);
            //         ev.start();
            //         ev.release();
            //     }
        }
        else
        {
            // Possess an enemy
            if (hagHealth != null)
            {
                hagHealth.SetDecay(0f);
            }
            if (newHealth != null)
            {
                newHealth.SetDecay(lifeDrainCoefficient);
                newHealth.EnableUpdateModel(true);
                if (secondaryHealthBar != null)
                {
                    secondaryHealthBar.GetComponent<HealthBar>().Subscribe(newHealth);
                    oldHag.gameObject.SetActive(false);
                    secondaryHealthBar.SetActive(true);
                }
            }
            newCharacter.SetTeamID(1);
            timePossessing = Time.time;
        }
        currentCharacter = newCharacter;
        PlayerController.instance.currentCharacter = newCharacter;
    }
}
