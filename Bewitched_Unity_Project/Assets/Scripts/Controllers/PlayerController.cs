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
using UnityEngine.AI;
using Unity.AI.Navigation;
using DG.Tweening;

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
    [Tooltip("The targeting range for a character")]
    public float targetingRange = 10;
    [Tooltip("Character current locked onto")]
    public Enemy lockedCharacter = null;

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
    [Header("Interact UI")]
    [Tooltip("nearby interactable object")]
    public IInteract nearbyInteractable;
    [SerializeField, Tooltip("UI prefab for the interact button (it will be shown when the player is near the interactable object)")]
    private GameObject interactUI;

    [Header("Staircase Door")]
    public StaircaseDoor exitDoor;

    [Tooltip("The character controller of the current character")]
    private CharacterController characterController;

    public Vector2 movementInput;

    public Vector3 direction;

    private Vector3 velocity = new Vector3(0,0,0);

    private float speed;

    private bool allowMovement = true;

   // private bool dodging = false;

    [Tooltip("The y velocity the player is moving at")]
    private float yVelocity;
    [Tooltip("The jump speed of the player")]
    private float jumpSpeed;
    [Tooltip("The window to counter this enemy is open")]
    private Enemy enemyCounterable = null;

    public void SetCounterAvaliable(Enemy enemy)
    {
        enemyCounterable = enemy;
    }

    public Enemy GetCounterAvailable()
    {
        return enemyCounterable;
    }

    public void SeteCharacterController(CharacterController controller)
    {
        characterController = controller;
    }
    
    [Tooltip("Player Modifier Volume")]
    [SerializeField] NavMeshModifierVolume playerZone;

    [Tooltip("Navmesh Surface")]
    [SerializeField] NavMeshSurface surface;

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
        oldHag.ActivateSurroundingPoints();

        ResumeGame();
        HideInteractUI();
    }

    private void Awake()
    {
        currentCharacter = oldHag;

        characterController = currentCharacter.GetComponent<CharacterController>();
    }

    private void FixedUpdate()
    {
        TargetEnemy();
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
            if (movementInput.sqrMagnitude > 0.01)
            {
                if (CameraController.GetIsAiming())
                {
                    Vector3 desiredVelocity = direction * speed;
                    desiredVelocity = Camera.main.transform.TransformDirection(desiredVelocity);
                    desiredVelocity.y = 0f; // Prevent tilting
                    if (desiredVelocity.magnitude >= velocity.magnitude) // If accelerating or changing direction at same speed
                    {
                        velocity += desiredVelocity.normalized * currentCharacter.acceleration * Time.deltaTime;
                    }
                    else
                    {
                        velocity = Vector3.Lerp(velocity, desiredVelocity, Time.deltaTime * currentCharacter.deceleration);
                    }

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
                    float xChange = GetAccelerationValue(velocity.x, desiredVelocity.x) * Time.deltaTime;
                    velocity.x += xChange;

                    if (Mathf.Abs(velocity.x) >= speed) velocity.x = speed * Mathf.Sign(velocity.x); // If above max x velocity (movement speed straight in x direction)

                    float zChange = GetAccelerationValue(velocity.z, desiredVelocity.z) * Time.deltaTime;
                    velocity.z += zChange;

                    if (Mathf.Abs(velocity.z) >= speed) velocity.z = speed * Mathf.Sign(velocity.z);


                    if (velocity.magnitude > speed)
                    {
                        velocity = velocity.normalized * speed;
                    }

                    if (velocity.magnitude < 0.01f)
                    {
                        velocity = Vector3.zero;
                    }

                    characterController.Move(velocity * Time.deltaTime);

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
            currentCharacter.SetVelocity(velocity);
        }
    }

    /// <summary>
    /// Determines what acceleration/deceleration value should be used for x and z values
    /// </summary>
    /// <param name="currentVelocity"> Current velocity in direction </param>
    /// <param name="desired"> Desired velocity (at top speed in direction) </param>
    /// <returns> Acceleration or deceleraton value </returns>
    public float GetAccelerationValue(float currentVelocity, float desired)
    {
        float currentSign = Mathf.Sign(currentVelocity);
        float desiredSign = Mathf.Sign(desired);

        if (Mathf.Abs(currentVelocity) <= 0.01f) return currentCharacter.acceleration * desiredSign;

        if (currentSign == desiredSign) // If moving in same direction
        {
            if (Mathf.Abs(currentVelocity) > Mathf.Abs(desired)) // If going faster than desired in direction
            {
                return currentCharacter.deceleration * -currentSign; // Reverse direction so adding substracts from magnitude
            }
            else // Otherwise accelerate
            {
                return currentCharacter.acceleration * Mathf.Sign(desired);
            }
        }
        else // If needing to move in a different direction, move in desired direction
        {
            return currentCharacter.deceleration * desired;
        }
    }

    /// <summary>
    /// Gets the direction of the player to move in
    /// </summary>
    /// <returns> The direction of input if moving or the direction the player is facing </returns>
    public Vector3 GetMovementDirection()
    {
        Vector3 inputDirection;

        if (movementInput.sqrMagnitude > 0.01)
        {
            inputDirection = Camera.main.transform.TransformDirection(direction);
        }
        else
        {
            inputDirection = currentCharacter.transform.forward;
        }

        return inputDirection;
    }

    public void Move(InputAction.CallbackContext context)
    {
        movementInput = context.ReadValue<Vector2>();
        direction = new Vector3(movementInput.x, 0, movementInput.y).normalized;
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
        if(characterController.isGrounded && context.started && currentCharacter.GetJumpSpeed() > 0)
        {
            Character attacker = currentCharacter.GetAttacker();
            //if (attacker != null && !dodging) // Do a dodge if being attacked
            //{
            //   // StartCoroutine(Dodge(attacker.Dodgable(), attacker));
            //}
            //else
            //{
                jumpSpeed = currentCharacter.GetJumpSpeed();
                yVelocity = jumpSpeed;
          //  }
            currentCharacter.Jump();
            StartCoroutine(JumpCoroutine());
        }
    }

    /// <summary>
    /// Starts the jump
    /// Waits for jump delay for animation purposes then starts movement 
    /// </summary>
    /// <returns></returns>
    private IEnumerator JumpCoroutine()
    {
        yield return new WaitForSeconds(currentCharacter.GetJumpDelay());
        jumpSpeed = currentCharacter.GetJumpSpeed();
        yVelocity = jumpSpeed;
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
    /// <summary>
    /// This is called when the player interacts with the interactable object
    /// It will trigger the pickup event
    /// </summary>
    public void Interact(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            if (nearbyInteractable != null)
            {
                nearbyInteractable.Interact();
                // Hide the interact UI since the interact action has been performed
                HideInteractUI();
                return;
            }
            if (exitDoor != null)
            {
                exitDoor.OpenDoor();
            }
        }
    }
    /// <summary>
    /// Shows the interact UI, this is called when the player is near the interactable object
    /// </summary>
    public void ShowInteractUI()
    {
        if (interactUI == null) return;
        interactUI.SetActive(true);
    }
    /// <summary>
    /// Hides the interact UI, this is called when the player is out of range of the interactable object
    /// </summary>
    public void HideInteractUI()
    {
        if (interactUI == null) return;
        interactUI.SetActive(false);
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

    
    /// <summary>
    /// Targets the closest enemy to the input direction if it is within a range
    /// </summary>
    public void TargetEnemy()
    {
        Vector3 camForward = new Vector3(Camera.main.transform.forward.x, 0, Camera.main.transform.forward.z);
        camForward = camForward.normalized;

        Vector3 camRight = new Vector3(Camera.main.transform.right.x, 0, Camera.main.transform.right.z);
        camRight = camRight.normalized;

        Vector3 inputDirection =  camForward * movementInput.y + camRight * movementInput.x;
        inputDirection = inputDirection.normalized;

        Debug.DrawRay(currentCharacter.transform.position, inputDirection, Color.red);

        RaycastHit info;

        if (lockedCharacter == currentCharacter) lockedCharacter = null;

        if(Physics.SphereCast(currentCharacter.transform.position, 3f, inputDirection, out info, 10, enemyLayerMask))
        {
            if(info.collider.transform.GetComponent<Enemy>() && info.collider.gameObject != currentCharacter.gameObject)
            {
                lockedCharacter = info.collider.transform.GetComponent<Enemy>();
            }
        }

        if (lockedCharacter)
        {
            Debug.DrawRay(currentCharacter.transform.position, lockedCharacter.transform.position - currentCharacter.transform.position, Color.green);
        }
    }

    /// <summary>
    /// Gets the locked target
    /// </summary>
    /// <returns> Locked target </returns>
    public Enemy GetLockedTarget()
    {
        return lockedCharacter;
    }

    /// <summary>
    /// Handles dodging for a character
    /// </summary>
    /// <param name="wellTimed"></param>
    /// <returns></returns>
    //public IEnumerator Dodge(bool wellTimed, Character attacker)
    //{
    //    dodging = true;
    //    SetAllowMovement(false);

    //    Debug.Log(attacker);
    //    attacker.SetDodged();
    //    if (wellTimed)
    //    {
    //        //Time.timeScale = 0.75f;
    //    }

    //    Vector3 toAttacker = attacker.transform.position - currentCharacter.transform.position;
    //    int attackDirection;

    //    if (direction.magnitude < 0.01f) // If inputting in direction
    //    {
    //        attackDirection = 0; // Backwards
    //    }
    //    else
    //    {
    //        float angle = Vector3.SignedAngle(direction, toAttacker, Vector3.up);

    //        if (angle <= 0 && angle > -135)
    //        {
    //            attackDirection = -1; // Left
    //        }
    //        else if (angle > 0 && angle < 135)
    //        {
    //            attackDirection = 1; // Right
    //        }
    //        else
    //        {
    //            attackDirection = 0;
    //        }
    //    }

    //    Vector3 dodgeDirection;

    //    if (attackDirection == 0) // Dodge backwards
    //    {
    //        dodgeDirection = -toAttacker.normalized;
    //    }
    //    else if (attackDirection == -1)
    //    {
    //        dodgeDirection = Quaternion.AngleAxis(90f, Vector3.up) * toAttacker.normalized;
    //    }
    //    else
    //    {
    //        dodgeDirection = Quaternion.AngleAxis(-90f, Vector3.up) * toAttacker.normalized;
    //    }

    //    Vector3 targetPosition = currentCharacter.transform.position + dodgeDirection * 2;
    //    Vector3 lookBackDir = (targetPosition - currentCharacter.transform.position).normalized;
    //    lookBackDir.y = 0;

    //    Time.timeScale = 1;
    //    SetAllowMovement(true);
    //    dodging = false;
    //    characterController.enabled = true;

    //    currentCharacter.transform.DOMove(targetPosition, 0.5f);
    //    yield return new WaitForSeconds(0.5f);

    //    //Manually set position after
    //    currentCharacter.transform.position = targetPosition;

    //    Time.timeScale = 1;
    //    SetAllowMovement(true);
    //    dodging = false;
    //    Debug.Log("Dodge End");
    //}
}
