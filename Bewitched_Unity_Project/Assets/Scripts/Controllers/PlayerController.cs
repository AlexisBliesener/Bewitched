using Cinemachine;
using FMOD.Studio;
using FMODUnity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TextCore.Text;

public class PlayerController : MonoBehaviour
{
    public delegate void PlayerControlHandler(Character character);
    public static PlayerControlHandler CharacterControlChangeEvent;
    public static PlayerController instance { get; private set; }

    [Header("Layer Masks")]
    [SerializeField, Tooltip("A layer mask that contains only the enemy layer")]
    private LayerMask enemyLayerMask;

    [Header("Character Settings")]

    [Tooltip("The character being controlled currently")]
    public Character currentCharacter;
    [Tooltip("The main character body (possessor)")]
    public Hag oldHag;
    [Tooltip("Rate at which life is drained")]
    public float lifeDrainCoefficient = 2;
    [Tooltip("Time to fill up enemy explosion")]
    public float enemyExplosionTime = 10;

    //This is the prefab for the possession orb that the hag shoots.
    [Tooltip("The cooldown in seconds that the player must wait in witch state before being able to possess again")]
    [SerializeField] float possessionCooldown = 10;
    [SerializeField, Tooltip("The max distance away from the camera that the player can possess")]
    private float maxPossessionDistance;

    [Header("UI Settings")]
    [Tooltip("The game virutal camera")]
    public CinemachineVirtualCamera virtualCam;

    [Tooltip("The hag health bar")]
    public GameObject hagHealthBar;
    [Tooltip("The currently controlled character's health bar")]
    public GameObject secondaryHealthBar;

    [Header("Buff Holder")]
    [Tooltip("Buff Component")]
    public Buffs playerBuffs;

    [Header("Ability Cooldown UI")]
    [Tooltip("Possession Orb Cooldown UI")]
    public CooldownDisplay possessionCooldownDisplay;
    [Tooltip("Primary Cooldown UI")]
    public CooldownDisplay primaryCooldownDisplay;
    [Tooltip("Secondary Cooldown UI")]
    public CooldownDisplay secondaryCooldownDisplay;

    [Header("Pause UI")]
    public GameObject pauseMenu;

    [Header("Staircase Door")]
    public StaircaseDoor exitDoor;

    private CharacterController characterController;

    public Vector2 input;

    public Vector3 direction;

    private Vector3 velocity = new Vector3(0,0,0);

    private float speed;

    private bool allowMovement = true;

    private float timePossessing;

    [Tooltip("The time possession was left")]
    private float timePossessionLastLeft = Mathf.NegativeInfinity;

    private bool primaryHeld = false;
    private bool secondaryHeld = false;
    private bool possessHeld = false;
    private bool leaveHeld = false;

    [Tooltip("The y velocity the player is moving at")]
    private float yVelocity;
    [Tooltip("The jump speed of the player")]
    private float jumpSpeed;

    private void Start()
    {
        instance = this;
        HealthController hagHealth = oldHag.GetComponent<HealthController>();
        if (hagHealth != null)
        {
            hagHealth.SetHealthToMax();
            if (hagHealthBar != null)
            {
                hagHealthBar.GetComponent<HealthBar>().Subscribe(hagHealth);
                hagHealthBar.SetActive(true);
            }
        }
    }

    private void Awake()
    {
        currentCharacter = oldHag;

        characterController = currentCharacter.GetComponent<CharacterController>();
        CharacterControlChangeEvent+=SwitchCharacter;
    }

    void OnDisable()
    {
        CharacterControlChangeEvent-=SwitchCharacter;
    }

    private void FixedUpdate()
    {
        HandleHeldAbilities();
        HandleCooldownUI();
        speed = currentCharacter.movementSpeed;

        if(characterController.isGrounded && yVelocity <0)
        {
            yVelocity = -0.5f;
        }
        else if (yVelocity > Physics.gravity.y)
        {
            yVelocity += (Physics.gravity * Time.fixedDeltaTime * 2).y;
        }

        characterController.Move(new Vector3(0, yVelocity, 0) * Time.fixedDeltaTime);

        if (allowMovement)
        {
            if (input.sqrMagnitude > 0.01)
            {
                
                Vector3 desiredVelocity = direction * speed;
                desiredVelocity = currentCharacter.transform.TransformDirection(desiredVelocity);

                velocity = Vector3.Lerp(velocity, desiredVelocity, Time.deltaTime * 10f);

                characterController.Move(velocity * Time.deltaTime);
                currentCharacter.AnimateMove();
            }
            else
            {
                velocity = new Vector3(0, 0, 0);
                currentCharacter.AnimateIdle();
            }
        }

        if (currentCharacter != oldHag)
        {
            oldHag.AnimateIdle();
        }
    }

    public void Move(InputAction.CallbackContext context)
    {
        input = context.ReadValue<Vector2>();
        direction = new Vector3(input.x, 0, input.y).normalized;
    }

    public void PrimaryFire(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            primaryHeld = true;
            if (currentCharacter.CheckPrimaryUsable())
            {
                StartCoroutine(currentCharacter.BeginPrimary());
            }
        }
        else if (context.canceled) // On release
        {
            currentCharacter.ReleasePrimary();
            primaryHeld = false;
        }
        else
        {
            return;
        }
    }

    public void SecondaryFire(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            secondaryHeld = true;
            if (currentCharacter.CheckSecondaryUsable())
            {
                currentCharacter.SetSecondaryAnimStatus(true);
                StartCoroutine(currentCharacter.BeginSecondary());
            }
        }
        else if (context.canceled) // On release
        {
            currentCharacter.ReleaseSecondary(); // does nothing for some, starts attack for others
            secondaryHeld = false;
        }
        else
        {
            return;
        }
    }

    public void Possess(InputAction.CallbackContext context)
    {
        if(currentCharacter == oldHag)
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

    public void Jump(InputAction.CallbackContext context)
    {
        if(characterController.isGrounded && context.started)
        {
            jumpSpeed = currentCharacter.GetJumpSpeed();
            yVelocity = jumpSpeed;
        }
    }

    public void HandleHeldAbilities()
    {
        if (currentCharacter.CheckPrimaryUsable() && primaryHeld)
        {
            StartCoroutine(currentCharacter.BeginPrimary());
        }

        if (currentCharacter.CheckSecondaryUsable() && secondaryHeld)
        {
            currentCharacter.SetSecondaryAnimStatus(true);
            StartCoroutine(currentCharacter.BeginSecondary());
        }

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
    }

    public void PauseGame(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            if (pauseMenu.activeInHierarchy == false) // If not paused
            {
                Time.timeScale = 0;
                pauseMenu.SetActive(true);
            }
            else
            {
                ResumeGame();
            }
        }
    }

    public void Interact(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            exitDoor.OpenDoor();
        }
    }

    public void ResumeGame()
    {
        Time.timeScale = 1;
        pauseMenu.SetActive(false);
    }

    public void SwitchCharacter(Character newCharacter){

        Debug.Log("new char " + newCharacter);
        characterController = newCharacter.GetComponent<CharacterController>();
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
            SetAllowMovement(true);
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
                if (secondaryHealthBar != null )
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
    }

    public Hag GetHag()
    {
        return oldHag;
    }

    public Character GetCurrentCharacter()
    {
        return currentCharacter;
    }

    public void SetAllowMovement(bool val)
    {
        allowMovement = val;
    }

    public void HandleCooldownUI()
    {
        if (currentCharacter.primaryFireIcon != primaryCooldownDisplay.abilityImage.sprite)
        {
            primaryCooldownDisplay.abilityImage.sprite = currentCharacter.primaryFireIcon;
        }

        if (currentCharacter.secondaryFireIcon != secondaryCooldownDisplay.abilityImage.sprite)
        {
            secondaryCooldownDisplay.abilityImage.sprite = currentCharacter.secondaryFireIcon;
        }

        primaryCooldownDisplay.SetCooldownCover(currentCharacter.GetCooldownPrimary());
        secondaryCooldownDisplay.SetCooldownCover(currentCharacter.GetCooldownSecondary());

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

    private void FirePossession()
    {
        Ray possessionRay = new Ray(virtualCam.transform.position, virtualCam.transform.forward);
        RaycastHit hitInfo;
        if(Physics.Raycast(possessionRay, out hitInfo, maxPossessionDistance))
        {
            if(hitInfo.collider.gameObject.CompareTag("Enemy"))
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
}
