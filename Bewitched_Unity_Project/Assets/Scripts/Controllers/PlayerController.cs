using Cinemachine;
using FMOD.Studio;
using FMODUnity;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.TextCore.Text;
using static UnityEngine.UI.Image;

public class PlayerController : MonoBehaviour
{
    public delegate void PlayerControlHandler(Character character);
    public static PlayerController instance { get; protected set; }

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

    [Tooltip("Player Modifier Volume")]
    [SerializeField] NavMeshModifierVolume playerZone;

    [Tooltip("Navmesh Surface")]
    [SerializeField] NavMeshSurface surface;

    [Tooltip("The character controller of the current character")]
    private CharacterController characterController;

    public Vector2 movementInput;

    public Vector3 direction;

    private Vector3 velocity = new Vector3(0,0,0);

    private float speed;

    private bool allowMovement = true;

   // private bool dodging = false;

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
                        velocity += desiredVelocity.normalized * currentCharacter.acceleration * Time.fixedDeltaTime;
                    }
                    else
                    {
                        velocity = Vector3.Lerp(velocity, desiredVelocity, Time.fixedDeltaTime * currentCharacter.deceleration);
                    }

                    velocity += Vector3.up * Physics.gravity.y * Time.fixedDeltaTime;

                    characterController.Move(velocity * Time.fixedDeltaTime);

                }
                else
                {
                    Vector3 desiredVelocity = direction * speed;
                    desiredVelocity = Camera.main.transform.TransformDirection(desiredVelocity);
                    desiredVelocity.y = 0f; // Prevent tilting
                    desiredVelocity = desiredVelocity.normalized * speed;
                    float xChange = GetAccelerationValue(velocity.x, desiredVelocity.x) * Time.fixedDeltaTime;
                    velocity.x += xChange;

                    if (Mathf.Abs(velocity.x) >= speed) velocity.x = speed * Mathf.Sign(velocity.x); // If above max x velocity (movement speed straight in x direction)

                    float zChange = GetAccelerationValue(velocity.z, desiredVelocity.z) * Time.fixedDeltaTime;
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

                    velocity += Vector3.up * Physics.gravity.y * Time.fixedDeltaTime;

                    characterController.Move(velocity * Time.fixedDeltaTime);

                    if (velocity.sqrMagnitude > 0.01f)
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(new Vector3(velocity.x, 0, velocity.z));
                        currentCharacter.transform.rotation = Quaternion.Slerp(
                            currentCharacter.transform.rotation,
                            targetRotation,
                            10f * Time.fixedDeltaTime
                        );
                    }
                }
            }
            else
            {   
                velocity = new Vector3(0, 0, 0);
                characterController.Move(velocity);
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

    public virtual void SetAllowMovement(bool val)
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
        Vector3 dir = currentCharacter.transform.forward;

        if (movementInput.magnitude < 0.001f)
            dir = new Vector3(Camera.main.transform.forward.x, 0, Camera.main.transform.forward.z);

        dir.y = 0;
        dir = dir.normalized;

        if (lockedCharacter == currentCharacter) lockedCharacter = null;

        RaycastHit[] hits = Physics.SphereCastAll(currentCharacter.transform.position + dir * 3f, 2f, dir, 0f, enemyLayerMask);

        Enemy target = null;
        float targetDistance = Mathf.Infinity;

        if (hits.Length > 0 && (hits.Length != 1 || hits[0].collider.gameObject.name != "Eleth"))
        {
            foreach (RaycastHit hit in hits)
            {
                Enemy enemy = hit.collider.GetComponent<Enemy>();
                if (enemy && hit.collider.gameObject != currentCharacter.gameObject && (target == null || Vector3.Distance(enemy.transform.position, currentCharacter.transform.position) < targetDistance))
                {
                    target = enemy;
                    targetDistance = Vector3.Distance(enemy.transform.position, currentCharacter.transform.position);
                    break;
                }
            }
        }
        else
        {
            hits = Physics.SphereCastAll(currentCharacter.transform.position + dir * 8f, 4f, dir, 0f, enemyLayerMask);
            if (hits.Length > 0)
            {
                foreach (RaycastHit hit in hits)
                {
                    Enemy enemy = hit.collider.GetComponent<Enemy>();
                    if (enemy && hit.collider.gameObject != currentCharacter.gameObject && (target == null || Vector3.Distance(enemy.transform.position, currentCharacter.transform.position) < targetDistance))
                    {
                        target = enemy;
                        targetDistance = Vector3.Distance(enemy.transform.position, currentCharacter.transform.position);
                        break;
                    }
                }
            }
        }

        lockedCharacter = target;

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
