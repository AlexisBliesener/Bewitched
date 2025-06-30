using System;
using System.Collections;
using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public delegate void PlayerControlHandler(Character character);
    public static PlayerControlHandler CharacterControlChangeEvent;
    public static PlayerController instance { get; private set; }

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
    [Tooltip("The possession orb prefab to be shot")]
    [SerializeField] GameObject possessionOrbPrefab;
    [Tooltip("The speed the possession orb moves")]
    [SerializeField] float possessionOrbSpeed = 5;
    [Tooltip("Orb Cooldown")]
    [SerializeField] float orbCooldown = 10;
    [Tooltip("The Start Animation Delay on the Possession Orb")]
    [SerializeField] float orbAnimationDelay = 0;

    [Header("UI Settings")]
    [Tooltip("The game camera")]
    public Camera myCamera;

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

    private Vector2 input;

    private Vector3 direction;

    private Vector3 velocity = new Vector3(0,0,0);

    private float speed;

    private bool allowMovement = true;

    private float timePossessing;

    private float timeLastFired = -Mathf.Infinity;

    private int numOrbsFired = 0;

    private bool primaryHeld = false;
    private bool secondaryHeld = false;
    private bool possessHeld = false;
    private bool leaveHeld = false;

    private void Start()
    {
        instance = this;
    }

    private void Awake()
    {
        currentCharacter = oldHag;

        characterController = currentCharacter.GetComponent<CharacterController>();
        CharacterControlChangeEvent+=SwitchCharacter;

        oldHag.SetHealthToMax();

        hagHealthBar.GetComponent<HealthBar>().SetCharacter(oldHag);
        hagHealthBar.SetActive(true);
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

        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        Ray ray = myCamera.ScreenPointToRay(Input.mousePosition);

        if (allowMovement)
        {

            if (groundPlane.Raycast(ray, out float enter))
            {
                Vector3 mouseWorldPosition = ray.GetPoint(enter);
                Vector3 lookDirection = (mouseWorldPosition - currentCharacter.transform.position).normalized;

                float targetAngle = Mathf.Atan2(lookDirection.x, lookDirection.z) * Mathf.Rad2Deg;
                currentCharacter.transform.rotation = Quaternion.Euler(0, targetAngle, 0);
            }

        
            if (input.sqrMagnitude > 0.01)
            {
                Vector3 desiredVelocity = direction * speed;
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
            currentCharacter.DrainLife(lifeDrainCoefficient * Time.deltaTime);
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
        if (context.started)
        {
            possessHeld = true;
            if (Time.time - timeLastFired >= orbCooldown)
            {
                timeLastFired = Time.time;
                currentCharacter.AnimatePossess();
                StartCoroutine(FireOrbWithDelay());
            }
        }
        else if (context.canceled)
        {
            possessHeld = false;
        }
        else
        {   
            return;
        }
    }

    public void LeaveBody(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            leaveHeld = true;
            StartCoroutine(ExplodeEnemy());
        }
        else if (context.canceled)
        {
            leaveHeld = false;
        }
        else
        {
            return;
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

        if (Time.time - timeLastFired >= orbCooldown && possessHeld)
        {
            timeLastFired = Time.time;
            currentCharacter.AnimatePossess();
            StartCoroutine(FireOrbWithDelay());
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
        
        characterController = newCharacter.GetComponent<CharacterController>();

        if (newCharacter == oldHag)
        {
            secondaryHealthBar.SetActive(false);
            currentCharacter.SetTeamID(2);
            SetAllowMovement(true);
            //Might need to change this once I get to enemy specific death sounds
            if(AudioManager.TryGetReference("LeaveBody",out EventReference evRef)){
                    EventInstance ev = RuntimeManager.CreateInstance(evRef);
                    ev.start();
                    ev.release();
                }
        }
        else
        {
            secondaryHealthBar.GetComponent<HealthBar>().SetCharacter(newCharacter);
            secondaryHealthBar.SetActive(true);
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

        if (currentCharacter == oldHag || numOrbsFired < playerBuffs.numExtraPossessionOrbs)
        {
            possessionCooldownDisplay.SetAbleToUse(true);
        }
        else
        {
            possessionCooldownDisplay.SetAbleToUse(false);
        }

        possessionCooldownDisplay.SetCooldownCover(orbCooldown - (Time.time - timeLastFired));
    }

    private IEnumerator FireOrbWithDelay()
    {
        yield return new WaitForSeconds(orbAnimationDelay);

        if (currentCharacter == oldHag) // Change condition when upgrade created
        {
            GameObject orb = Instantiate(possessionOrbPrefab);
            orb.transform.position = currentCharacter.transform.position;
            orb.GetComponent<PossessionOrb>().Init(currentCharacter.transform.forward * possessionOrbSpeed, currentCharacter.GetComponent<Character>());
        }
        else if (numOrbsFired < playerBuffs.numExtraPossessionOrbs)
        {
            GameObject orb = Instantiate(possessionOrbPrefab);
            orb.transform.position = currentCharacter.transform.position;
            orb.GetComponent<PossessionOrb>().Init(currentCharacter.transform.forward * possessionOrbSpeed, currentCharacter.GetComponent<Character>());
            numOrbsFired++;
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
            else
            {
                currentCharacter.SetControlled(false);
                CharacterControlChangeEvent?.Invoke(oldHag);
            }
        }
    }
}
