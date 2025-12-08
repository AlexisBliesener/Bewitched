using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;
using UnityEngine.InputSystem.UI;

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
    [SerializeField, Tooltip("The ui input module used for input")]
    private InputSystemUIInputModule UIInput;

    [Header("Buff Holder")]
    [Tooltip("Buff Component")]
    public Buffs playerBuffs;

    [Header("Ability Cooldown UI")]
    [Tooltip("Primary Cooldown UI")]
    public CooldownDisplay primaryCooldownDisplay;
    [Tooltip("Secondary Cooldown UI")]
    public CooldownDisplay secondaryCooldownDisplay;

    [Header("Targeting variables")]
    [SerializeField, Tooltip("The radius of the dectection sphere")]
    private float sphereRadius = 6f;
    [SerializeField, Tooltip("The distance away from the player of the dectection sphere")]
    private float sphereDistance = 8f;
    [SerializeField, Tooltip("The weight that being in the direction the player wants to attack in affects the targeting calculation")]
    private float inFrontWeight = 50f;

    [Header("Pause UI")]
    public GameObject pauseMenu;
    [Header("Interact UI")]
    [Tooltip("nearby interactable object")]
    public IInteract nearbyInteractable;
    [SerializeField, Tooltip("UI prefab for the interact button (it will be shown when the player is near the interactable object)")]
    private GameObject interactUI;
    [SerializeField, Tooltip("UI prefab for the narrative panel (it will be shown when the player enters the narrative trigger)")]
    public GameObject narrativePanel;

    [Header("Staircase Door")]
    public StaircaseDoor exitDoor;

    [Tooltip("The character controller of the current character")]
    private CharacterController characterController;

    [Tooltip("Movement input from the player on X and Y")]
    public Vector2 movementInput;
    [Tooltip("Movement input from the player on X and Z")]
    public Vector3 movementInputV3;

    public Vector3 direction;

    private Vector3 velocity = new Vector3(0, 0, 0);

    private float speed;

    private bool allowMovement = true;
    [Tooltip("If true eleth is currently sprints, false if not")]
    private bool sprinting = false;
    [Tooltip("If ui has been clicked before interact was clicked")]
    private bool uiClicked = false;

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

    /// <summary>
    /// Returns if the player is currently sprinting
    /// </summary>
    public bool GetSprinting()
    {
        return sprinting;
    }

    private void Start()
    {
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
        instance = this;
        currentCharacter = oldHag;

        characterController = currentCharacter.GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        if (UIInput != null)
        {
            UIInput.actionsAsset["UI/Submit"].performed += UIClicked;
        }
        else
        {
            Debug.LogWarning("UIInput not assigned!");
        }
    }

    private void OnDisable()
    {
        if (UIInput != null)
        {
            UIInput.actionsAsset["UI/Submit"].performed -= UIClicked;
        }
        else
        {
            Debug.LogWarning("UIInput not assigned!");
        }
    }

    private void UIClicked(InputAction.CallbackContext context)
    {
        uiClicked = true;
    }

    /// <summary>
    /// This is called when the player interacts with the interactable object
    /// It will trigger the pickup event
    /// </summary>
    public void Interact(InputAction.CallbackContext context)
    {
        if (context.canceled)
        {
            sprinting = false;
        }
        else if (context.performed)
        {
            sprinting = false;
            if (!sprinting)
            {
                if (nearbyInteractable != null && nearbyInteractable.CanInteract)
                {
                    nearbyInteractable.Interact();
                }
                else if (exitDoor != null)
                {
                    exitDoor.OpenDoor();
                }
            }

            if (currentCharacter == oldHag && movementInput != Vector2.zero)
            {
                sprinting = true;
            }
        }
    }

    private void FixedUpdate()
    {
        TargetEnemy();
        HandleCooldownUI();

        if (allowMovement && !pauseMenu.activeInHierarchy)
        {
            if (movementInput.sqrMagnitude > 0.01)
            {
                speed = currentCharacter.GetSpeed();
                if (currentCharacter == oldHag && sprinting)
                {
                    speed = oldHag.GetSprintSpeed();
                }

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

                characterController.Move(velocity * Time.fixedDeltaTime);
                if (velocity.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(new Vector3(velocity.x, 0, velocity.z));
                    currentCharacter.transform.rotation = Quaternion.Slerp(
                        currentCharacter.transform.rotation,
                        targetRotation,
                        currentCharacter.GetRotationSpeed() * Time.fixedDeltaTime
                    );
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
        movementInputV3 = new Vector3(movementInput.x, 0, movementInput.y);
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
        else if (context.canceled)
        {
            currentCharacter.ReleaseSecondary();
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
                if (TimeController.instance != null)
                {
                    TimeController.instance.PauseGame();
                }
                else
                {
                    Debug.LogWarning("TimeController instance is not set!");
                }

                pauseMenu.SetActive(true);
            }
            else
            {
                ResumeGame();
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
        if (TimeController.instance != null)
        {
            TimeController.instance.ResumeGame();
        }
        else
        {
            Debug.LogWarning("TimeController instance is not set!");
        }
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
    /// Returns the input of the left stick
    /// </summary>
    /// <returns>Left stick input</returns>
    public Vector2 GetMovementInput()
    {
        return movementInput;
    }

    /// <summary>
    /// Targets the closest enemy to the input direction if it is within a range
    /// </summary>
    public void TargetEnemy()
    {
        Vector3 dir = new Vector3(movementInput.x, 0, movementInput.y);
        dir = Camera.main.transform.TransformDirection(dir);

        if (movementInput.magnitude < 0.001f)
            dir = new Vector3(Camera.main.transform.forward.x, 0, Camera.main.transform.forward.z);

        dir.y = 0;
        dir = dir.normalized;

        Enemy target = null;
        float targetDistance = Mathf.Infinity;

        RaycastHit[] hits = Physics.SphereCastAll(currentCharacter.transform.position + dir * sphereDistance, sphereRadius, dir, 0f, enemyLayerMask);

        if (hits.Length > 0)
        {
            foreach (RaycastHit hit in hits)
            {
                Enemy enemy = hit.collider.GetComponent<Enemy>();
                if (enemy == null) continue;
                Vector3 enemyPosNoY = new Vector3(enemy.transform.position.x, currentCharacter.transform.position.y, enemy.transform.position.z);
                Vector3 toEnemy = (enemyPosNoY - currentCharacter.transform.position).normalized;
                float baseDist = Vector3.Distance(enemyPosNoY, currentCharacter.transform.position) - enemy.sizeRadius - currentCharacter.sizeRadius;

                float dot = Vector3.Dot(toEnemy, dir);
                float dist = baseDist + (1 - dot) * inFrontWeight; // or adjust sign depending on intent

                if (enemy && hit.collider.gameObject != currentCharacter.gameObject && (target == null || dist < targetDistance))
                {
                    target = enemy;
                    targetDistance = dist;
                }
            }
        }

        if (target != currentCharacter)
        {
            lockedCharacter = target;

            if (lockedCharacter)
            {
                Debug.DrawRay(currentCharacter.transform.position, lockedCharacter.transform.position - currentCharacter.transform.position, Color.green);
            }
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
