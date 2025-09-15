using Cinemachine;
using FMOD.Studio;
using FMODUnity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.TextCore.Text;

public class PlayerController : MonoBehaviour
{
    public delegate void PlayerControlHandler(Character character);
    public static PlayerController instance { get; private set; }

    [Header("Layer Masks")]
    [SerializeField, Tooltip("A layer mask that contains only the enemy layer")]
    private LayerMask enemyLayerMask;

    [Header("Character Settings")]

    [Tooltip("The character being controlled currently")]
    public Character currentCharacter;
    [Tooltip("The main character body (possessor)")]
    public Hag oldHag;

    [Header("UI Settings")]

    [Tooltip("The hag health bar")]
    public GameObject hagHealthBar;

    [Header("Buff Holder")]
    [Tooltip("Buff Component")]
    public Buffs playerBuffs;

    [Header("Ability Cooldown UI")]
    [Tooltip("Primary Cooldown UI")]
    public CooldownDisplay primaryCooldownDisplay;
    [Tooltip("Secondary Cooldown UI")]
    public CooldownDisplay secondaryCooldownDisplay;

    [Header("Pause UI")]
    public GameObject pauseMenu;

    [Header("Staircase Door")]
    public StaircaseDoor exitDoor;

    [Tooltip("The character controller of the current character")]
    private CharacterController characterController;

    public Vector2 input;

    public Vector3 direction;

    private Vector3 velocity = new Vector3(0,0,0);

    private float speed;

    private bool allowMovement = true;

    [Tooltip("The y velocity the player is moving at")]
    private float yVelocity;
    [Tooltip("The jump speed of the player")]
    private float jumpSpeed;

    public void SeteCharacterController(CharacterController controller)
    {
        characterController = controller;
    }

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

        ResumeGame();
    }

    private void Awake()
    {
        currentCharacter = oldHag;

        characterController = currentCharacter.GetComponent<CharacterController>();
    }

    private void FixedUpdate()
    {
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


        if (allowMovement)
        {
            if (input.sqrMagnitude > 0.01)
            {
                if (CameraController.GetIsAiming())
                {
                    Vector3 desiredVelocity = direction * speed;
                    desiredVelocity = Camera.main.transform.TransformDirection(desiredVelocity);
                    desiredVelocity.y = 0f; // Prevent tilting
                    desiredVelocity = desiredVelocity.normalized * speed;

                    velocity = Vector3.Lerp(velocity, desiredVelocity, Time.deltaTime * 10f);

                    Vector3 finalMovement = velocity * Time.deltaTime + new Vector3(0, yVelocity, 0) * Time.fixedDeltaTime;
                    characterController.Move(finalMovement);

                }
                else
                {
                    Vector3 desiredVelocity = direction * speed;
                    desiredVelocity = Camera.main.transform.TransformDirection(desiredVelocity);
                    desiredVelocity.y = 0f; // Prevent tilting
                    desiredVelocity = desiredVelocity.normalized * speed;

                    velocity = Vector3.Lerp(velocity, desiredVelocity, Time.deltaTime * 10f);

                    Vector3 finalMovement = velocity * Time.deltaTime + new Vector3(0, yVelocity, 0) * Time.fixedDeltaTime;
                    characterController.Move(finalMovement);

                    if (velocity.sqrMagnitude > 0.01f)
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(velocity);
                        currentCharacter.transform.rotation = Quaternion.Slerp(
                            currentCharacter.transform.rotation,
                            targetRotation,
                            10f * Time.deltaTime
                        );
                    }
                }
            }
            else
            {
                velocity = new Vector3(0, 0, 0);
                characterController.Move(new Vector3(0, yVelocity, 0) * Time.fixedDeltaTime);
            }
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
            if (currentCharacter.CheckPrimaryUsable())
            {
                StartCoroutine(currentCharacter.BeginPrimary());
            }
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
            if (currentCharacter.CheckSecondaryUsable())
            {
                StartCoroutine(currentCharacter.BeginSecondary());
            }
        }
        else
        {
            return;
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
    }
    
}
