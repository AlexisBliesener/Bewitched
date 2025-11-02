using System.Collections;
using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using NaughtyAttributes;
using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public abstract class Enemy : Character
{
    [Header("Enemy AI Settings")]
    [Tooltip("Determines the enemy's mental state atm")]
    public bool lobotimzed = false;
    [Tooltip("Minimum Stopping Distance"), Range(0, 10)]
    public float minStopDistance = 0.5f;
    [Tooltip("Last seen time buffer"), Range(0, 10)]
    public float seenBuffer = 0.5f;
    [Header("Surrounding Settings")]
    [Tooltip("Distance from point an enemy can be before switching to chase"), Range(0, 10)]
    [SerializeField] protected float surroundingToChaseRadius = 2;

    [Tooltip("Distance from point an enemy must reach before switching to surround"), Range(0, 10)]
    [SerializeField] protected float chaseToSurroundingRadius = 1;
    [Header("Sight Settings")]
    [Tooltip("Sight Range"), Range(0, 360)]
    public float sightRange;
    [Tooltip("Maximum Sight Angle"), Range(0, 360)]
    public float maxSightAngle;

    [Tooltip("Hearing Range"), Range(0, 10)]
    public float hearingRange;

    [Tooltip("Walk Point Range"), Range(0, 50)]
    public float patrolRange;

    [Header("Time/Delay Settings")]

    [Tooltip("Time before searching"), Range(0, 10)]
    public float timeBeforeSearch = 5;
    [Tooltip("AI Attack Delay"), Range(0, 10)]
    public float attackDelayAI = 0.5f;

    [Header("Attack Settings")]
    [Tooltip("Chance for AI primary attack"), Range(0, 1)]
    public float primaryAttackChance = .5f;

    [Tooltip("Chance for AI secondary attack"), Range(0, 1)]
    public float secondaryAttackChance = .5f;
    [Tooltip("The threshold percentage that the enemy is low health for specific behaviors"), Range(0, 100)]
    public float lowHealthThresholdPercentage = 30;
    protected PlayerController playerController;

    protected Hag hag;
    protected Character currentPlayer;

    [SerializeField, Tooltip("If the player is further than distance away from the target the player will move towards it before attacking")]
    protected float moveToTargetDistance;



    [Tooltip("Point that the Enemy runs to while chasing/surrounding"), HideIf("debugging")]
    protected GameObject surroundPoint;

    protected GameObject destinationMarker;

    protected bool walkPointSet = false;
    protected bool playerInSightRange, currentInSightRange, targetInSightRange = false;
    protected bool targetInPrimaryRange = false;


    protected Vector3 walkPoint;

    protected Character target;

    protected Vector3 lastTargetLocation;

    protected bool seenTarget = false;

    protected GameObject minibar;

    protected bool isStunned = false;

    protected bool inAttackDelay = false;

    protected float timePlayerLastSeen;

    protected NavPath currentPath;

    /// <summary>
    /// Path getter function
    /// </summary>
    /// <returns> The path for this enemy </returns>
    public NavPath GetNavPath() { return currentPath; }

    protected bool reachedWalkpoint = true;

    protected bool lookAtPlayer = false;

    [Tooltip("Bool determining if this enemy is using the A* search")]
    protected bool usingAStar = false;

    [Tooltip("Corner node index we are currently on in our path")]
    protected int currentCornerIndex = 0;

    protected string debugAIInfo;

    [Tooltip("Dictionary of costly nodes with the cost they have been given")]
    Dictionary<List<int>, int> surroundingCostlyNodes = new Dictionary<List<int>, int>();

    protected bool overrideBlock = false;

    public enum PathState
    {
        Unset,
        Searching,
        Set
    }


    public enum AIMovementState
    {
        Patrolling, // Before spotting player
        Chasing, // Reaching the player
        Surrounding, // Staying in range of the player
        Retreating, // Post attack, return to safe distance
        Blocked, // For attacking, stun, etc. the character does not move or look
        Targeted, // For now does nothing to keep enemy in place, in future allows for dodging/countering
        PlayerControlled // For when the player is controlling this enemy, should not be AI controlled
    }

    [Tooltip("Point relative to player for enemy to navigate towards")]
    protected Vector3 chasePoint;

    [Tooltip("Bool determining if enemy can be stopped perfectly")]
    protected bool inPerfectStopZone = false;

    [Tooltip("Bool Determining if we are in a process that blocks AI (like looking around, attacking, etc")]
    protected bool inProcess = false;

    [Tooltip("The enemy's Patrol Point Origin")]
    protected Vector3 patrolOrigin;
    [Header("Enemy Prefabs/Effects and references")]
    [Tooltip("Perfect counter material for the enemy"), ShowIf("dev")]
    public Material perfectCounterTimeMaterial;
    [Tooltip("Default material for the enemy"), ShowIf("dev")]
    public Material defaultMaterial;
    [Header("Debug/Dev Options")]
    [Tooltip("Current path state"), ShowIf("dev")]
    public PathState pathState = PathState.Unset;
    [Tooltip("The Current AI State of the enemy"), ShowIf("dev")]
    public AIMovementState aiState = AIMovementState.Patrolling;
    [Tooltip("Show Paths, Destinations, etc"), ShowIf("dev")]
    public bool debugging = false;
    [Tooltip("Destination Marker Prefab"), ShowIf("dev")]
    public GameObject destinationMarkerPrefab;
    [Tooltip("Line Renderer for Path"), ShowIf("dev")]
    public LineRenderer pathVisualizer;
    [Tooltip("Is Player controlling this enemy?"), ShowIf("dev")]
    [SerializeField, NaughtyAttributes.ReadOnly] protected bool playerControlling = false; // flag for determining actions (player or AI)
    [Tooltip("Pathfinding Priority"), ShowIf("dev")]
    public int pathfindingPriority;
    //Just so code in update isn't called after the enemy is dead
    protected bool dead = false;
    
    [Header("Audio")]
    [Tooltip("The Event Reference for this enemy's hit sound effect")]
    [SerializeField] protected EventReference hitEventReference;
    [Tooltip("The event reference for this enemy's death sound effect")]
    [SerializeField]protected EventReference deathEventReference;

    //FMOD Event for idle sound effects
    protected EventInstance idleAudio;

    /// <summary>
    /// Stops the idle sound effects of the goblin if it's currently playing
    /// </summary>
    protected void StopIdleAudio()
    {
        if (idleAudio.isValid())
        {
            idleAudio.setParameterByNameWithLabel("End", "True");
            idleAudio = new();
        }
    }


    /// <summary>
    /// Destorys the enemies counter indicator if it is active
    /// </summary>
    public void DestroyCounterIndicator()
    {
        if (counterIndicatorVFX != null)
        {
            Destroy(counterIndicatorVFX);
        }
        counterIndicatorVFX = null;
    }


    private float lastPrimaryChance = 0;
    private float lastSecondaryChance = 0;

    /// <summary>
    /// Handles editor validation - at the moment it normalizes attack chances
    /// </summary>
    private void OnValidate()
    {
        bool primaryChanged = !Mathf.Approximately(primaryAttackChance, lastPrimaryChance);
        bool secondaryChanged = !Mathf.Approximately(secondaryAttackChance, lastSecondaryChance);

        if (primaryChanged && !secondaryChanged)
        {
            primaryAttackChance = Mathf.Clamp01(primaryAttackChance);
            secondaryAttackChance = 1f - primaryAttackChance;
        }
        else if (secondaryChanged && !primaryChanged)
        {
            secondaryAttackChance = Mathf.Clamp01(secondaryAttackChance);
            primaryAttackChance = 1f - secondaryAttackChance;
        }
        else
        {
            float total = primaryAttackChance + secondaryAttackChance;
            if (total == 0f) total = 1f;
            primaryAttackChance /= total;
            secondaryAttackChance /= total;
        }

        // Store for next frame
        lastPrimaryChance = primaryAttackChance;
        lastSecondaryChance = secondaryAttackChance;
    }

    private void Update()
    {
        // keep the which character is the player updated
        if (playerController != null)
        {
            currentPlayer = playerController.currentCharacter;
        }
        else
        {
            Debug.LogWarning("Player controller is not set!");
        }
    }

    /// <summary>
    /// Sets the debug info string
    /// </summary>
    public void SetDebugString()
    {
        debugAIInfo = "Character: " + gameObject.ToString() + ", state: " + aiState.ToString() + ", attack status: " + attackState + ", inProcess = " + inProcess.ToString();
    }

    /// <summary>
    /// Function for handling movement
    /// </summary>
    public void AIMove()
    {
        if (aiState == AIMovementState.PlayerControlled || lobotimzed || dead || gameObject == null) return;

        if (currentPath == null) // No path, decelerate to 0
        {
            velocity -= velocity.normalized * deceleration * Time.deltaTime;
            GetCharacterController().Move(velocity * Time.deltaTime);
            return;
        }

        float currentSpeed = velocity.magnitude;
        float stoppingDistance = (currentSpeed * currentSpeed) / (2f * deceleration);

        if (Vector3.Distance(transform.position, currentPath.GetDestinationPosition(gameObject)) <= minStopDistance + stoppingDistance)
        {
            if (Vector3.Distance(transform.position, currentPath.GetDestinationPosition(gameObject)) <= minStopDistance) velocity = Vector3.zero;
            else velocity -= velocity.normalized * deceleration * Time.deltaTime;
            GetCharacterController().Move(velocity * Time.deltaTime);
            return;
        }

        if (currentCornerIndex < currentPath.GetCornerNodes().Count - 1)
        {
            if (Vector3.Distance(currentPath.GetCornerNodes()[currentCornerIndex].GetPosition(gameObject), transform.position) <= minStopDistance)
            {
                currentCornerIndex++;
            }
        }
        else
        {
            currentCornerIndex = Mathf.Min(currentCornerIndex, currentPath.GetCornerNodes().Count - 1);
        }

        Vector3 desiredVelocity;


        desiredVelocity = (currentPath.GetCornerNodes()[currentCornerIndex].GetPosition(gameObject) - transform.position).normalized * movementSpeed;

        float xChange = GetAccelerationValue(velocity.x, desiredVelocity.x) * Time.deltaTime;
        velocity.x += xChange;

        if (Mathf.Abs(velocity.x) >= movementSpeed) velocity.x = movementSpeed * Mathf.Sign(velocity.x); // If above max x velocity (movement speed straight in x direction)

        float zChange = GetAccelerationValue(velocity.z, desiredVelocity.z) * Time.deltaTime;
        velocity.z += zChange;

        if (Mathf.Abs(velocity.z) >= movementSpeed) velocity.z = movementSpeed * Mathf.Sign(velocity.z);


        if (velocity.magnitude > movementSpeed)
        {
            velocity = velocity.normalized * movementSpeed;
        }

        if (velocity.magnitude < 0.01f)
        {
            velocity = Vector3.zero;
        }

        velocity += Vector3.up * Physics.gravity.y * Time.deltaTime;

        GetCharacterController().Move(velocity * Time.deltaTime);
    }

    /// <summary>
    /// Function to handle the rotation of an AI controller
    /// </summary>
    public void AILook()
    {
        if (aiState == AIMovementState.PlayerControlled) return;

        Quaternion lookRotation;
        if (aiState == AIMovementState.Surrounding || aiState == AIMovementState.Retreating) // If surrounding then look at player
        {
            lookRotation = Quaternion.LookRotation(Vector3.Lerp(transform.forward, currentPlayer.transform.position - transform.position, 5 * Time.deltaTime));
        }
        else
        {
            lookRotation = Quaternion.LookRotation(Vector3.Lerp(transform.forward, new Vector3(velocity.x, 0, velocity.z), 5 * Time.deltaTime));
        }
        transform.rotation = lookRotation;
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

        if (Mathf.Abs(currentVelocity) <= 0.01f) return acceleration * desiredSign;

        if (currentSign == desiredSign) // If moving in same direction
        {
            if (Mathf.Abs(currentVelocity) > Mathf.Abs(desired)) // If going faster than desired in direction
            {
                return deceleration * -currentSign; // Reverse direction so adding substracts from magnitude
            }
            else // Otherwise accelerate
            {
                return acceleration * Mathf.Sign(desired);
            }
        }
        else // If needing to move in a different direction, move in desired direction
        {
            return deceleration * desired;
        }
    }

    public void SetDebuggingValues()
    {
        pathVisualizer.startWidth = .15f;
        pathVisualizer.endWidth = .15f;
        pathVisualizer.positionCount = 0;
    }

    public void SetPlayerInfo()
    {
        GameObject controller = GameObject.FindWithTag("PlayerController");
        playerController = controller.GetComponent<PlayerController>();

        hag = playerController.GetHag();
        currentPlayer = playerController.GetCurrentCharacter();
    }
    /// <summary>
    /// Sets if the player is controlling this enemy
    /// </summary>
    /// <param name="val"> Value to set </param>
    public override void SetControlled(bool val)
    {
        StopAllCoroutines();
        playerControlling = val;
        if (val)
        {
            DestroyCounterIndicator();
            lockedCharacter = null;
            attackingPrimary = false;
            attackingSecondary = false;
            health.ShowMiniHealthBar(false);
            aiState = AIMovementState.PlayerControlled;
            pathState = PathState.Unset;
        }
        else
        {
            aiState = AIMovementState.Patrolling;
        }
    }

    public override void Die()
    {
        dead = true;
        DoDeathSoundEffect();
        if (playerControlling)
        {
            if (GrandFinale.instance.GetActive())
            {
                GrandFinale.instance.Explode(0f, true);
            }
            playerControlling = false;
            PossessionAbility.CharacterControlChangeEvent?.Invoke(hag);
        }
        else
        {
            // Drop the upgrade only if the enemy is dead and the player is not controlling it
            DropSystem.Instance.TryDropItem(transform.position);
            // Spawn soul on death
            SoulSystem.Instance.SpawnSoul(transform.position);
        }

        GameObject.FindGameObjectWithTag("Lock Manager").GetComponent<LockManager>().IncrementKills();
        health.ShowMiniHealthBar(false);
        StopAllCoroutines();
        // Destory the enemy after a delay to avoid the error "Destroying object during on physics callbacks"
        Destroy(gameObject, 0.1f);
    }

    public void SetRangeChecks()
    {
        if (!hag.isActiveAndEnabled) return;

        currentInSightRange = (target.transform.position - transform.position).magnitude < sightRange;
        float distToChar = (currentPlayer.transform.position - transform.position).magnitude;
        if (currentInSightRange)
        {
            if (Physics.Raycast(transform.position, currentPlayer.transform.position - transform.position, distToChar, environment))
            {
                currentInSightRange = false;
            }
        }
    }

    /// <summary>
    /// Checks if the target is visible to the enemy with distance
    /// </summary>
    /// <param name="location"> Transform of the character </param>
    /// <returns> True if in range </returns>
    public bool CheckTargetInRange(Transform location)
    {
        if ((location.position - transform.position).magnitude < sightRange)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Checks if the target can "hear" the player
    /// We can make this more interesting (have it depend on player weight/speed as well) later
    /// </summary>
    /// <param name="location"> Location of target </param>
    /// <returns> True if it can hear the target </returns>
    public bool CanHearTarget(Transform location)
    {
        if ((location.position - transform.position).magnitude < hearingRange)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Searches for the player, handling variables if it can see them
    /// </summary>
    /// <returns> True if player is visible to enemy </returns>
    public bool LookForPlayer()
    {
        if (CheckTargetInRange(currentPlayer.transform) && CheckCharacterBehindEnvironment(currentPlayer.transform))
        {
            seenTarget = true;
            lastTargetLocation = currentPlayer.transform.position;
            return true;
        }
        return false;
    }

    public virtual void SetBehavior()
    {
        if (inAttackDelay) return;
        if (targetInSightRange && CheckCharacterBehindEnvironment(target.transform))
        {
            seenTarget = true;
            lastTargetLocation = target.transform.position;

            if (targetInPrimaryRange)
            {
                inAttackDelay = true;
                StartCoroutine(AttackWithDelay(attackDelayAI));
            }
            else
            {
                Chase();
            }
        }
        else if (seenTarget)
        {
            if ((lastTargetLocation - transform.position).magnitude > 0.1)
            {
            }
            else
            {
                seenTarget = false;
                Patrol();
            }
        }
        else
        {
            Patrol();
        }
    }

    /// <summary>
    /// Handles the hitstun actions for enemies
    /// </summary>
    /// <param name="duration"> Duration to stun for </param>
    /// <returns> Time </returns>
    public override IEnumerator StartHitStun(float duration)
    {
        if (duration > 0)
        {
            if (stunned) yield break;
            hitStunActual = Instantiate(hitStunPrefab, transform);
            stunned = true;
            float timeStarted = Time.time;
            while (Time.time - timeStarted < duration)
            {
                if (playerControlling) PlayerController.instance.SetAllowMovement(false);
                else aiState = AIMovementState.Blocked;
                yield return null;
            }
            if (attackingPrimary) // Reset primary and secondary abilities so enemies don't break
            {
                attackingPrimary = false;
                timeLastPrimary = Time.time;
            }
            if (attackingSecondary)
            {
                attackingSecondary = false;
                timeLastSecondary = Time.time;
            }
            if (playerControlling) PlayerController.instance.SetAllowMovement(true);
            else aiState = AIMovementState.Chasing;
            stunned = false;
            Destroy(hitStunActual);
            hitStunActual = null;
        }
    }

    public virtual void Chase()
    {

    }

    public virtual void Patrol()
    {

    }

    public virtual bool SetWalkPoint()
    {
        return false;
    }

    public override void CreateHitStun()
    {
    }

    public override void HandleHitStun()
    {
        if (hitStunActual != null)
        {
            if (Time.time - health.TimeLastHit > hitStunDuration / GetComponent<CharacterAnimator>().GetHitStunMult())
            {
                SetMovementValues(true);
            }
        }

        base.HandleHitStun();
    }

    public void SetPlayerControlledBuffs(bool val, Buffs playerBuffs)
    {
        if (val)
        {
            movementSpeed = playerBuffs.speedScalar * baseMovementSpeed;
            primaryCooldown = playerBuffs.primaryCooldownPercent * primaryCooldown;
            secondaryCooldown = playerBuffs.secondaryCooldownPercent * secondaryCooldown;
        }
        else
        {
            movementSpeed = baseMovementSpeed;
            primaryCooldown = basePrimaryCooldown;
            secondaryCooldown = baseSecondaryCooldown;
        }
    }

    private IEnumerator AttackWithDelay(float delayTime)
    {
        yield return new WaitForSeconds(delayTime);

        inAttackDelay = false;
        StartCoroutine(BeginPrimary());
    }

    public bool CheckCharacterBehindEnvironment(Transform pos)
    {
        float dist = (pos.position - transform.position).magnitude;
        if (Physics.Raycast(transform.position, pos.position - transform.position, dist, environment))
        {
            return false;
        }
        return true;
    }

    public override IEnumerator BeginPrimary()
    {
        if (!playerControlling)
        {
            Vector3 playerPosition = target.transform.position;
            Vector3 directionToPlayer = new Vector3(playerPosition.x, transform.position.y, playerPosition.z) - transform.position;
            Quaternion lookRotation = Quaternion.LookRotation(directionToPlayer);

            float yDistance = target.transform.position.y - transform.position.y;
            float angle = Mathf.Asin(yDistance / (target.transform.position - transform.position).magnitude) * Mathf.Rad2Deg;

            transform.rotation = Quaternion.Euler(-angle / 3, lookRotation.eulerAngles.y, 0);
        }

        yield return StartCoroutine(base.BeginPrimary());
    }


    public void StartPath()
    {
        if (currentPath == null) return;

        if (destinationMarker)
        {
            destinationMarker.transform.position = currentPath.GetDestinationPosition(gameObject);
            destinationMarker.transform.position = new Vector3(destinationMarker.transform.position.x, 1, destinationMarker.transform.position.z);
        }
        else
        {
            destinationMarker = Instantiate(destinationMarkerPrefab);
            destinationMarker.transform.position = currentPath.GetDestinationPosition(gameObject);
            destinationMarker.transform.position = new Vector3(destinationMarker.transform.position.x, 1, destinationMarker.transform.position.z);
        }

        pathVisualizer.positionCount = 0;

        pathVisualizer.positionCount = currentPath.GetCornerNodes().Count;

        if (pathVisualizer.positionCount < 1) return;

        pathVisualizer.SetPosition(0, transform.position);

        for (int i = 1; i < currentPath.GetCornerNodes().Count; i++)
        {
            pathVisualizer.SetPosition(i, new Vector3(currentPath.GetCornerNodes()[i].GetPosition().x, transform.position.y, currentPath.GetCornerNodes()[i].GetPosition().z));
        }
    }

    /// <summary>
    /// Draws a path the agent follows
    /// </summary>
    public void UpdatePath()
    {
        if (destinationMarker)
        {
            destinationMarker.transform.position = currentPath.GetDestinationPosition(gameObject);
            destinationMarker.transform.position = new Vector3(destinationMarker.transform.position.x, 1, destinationMarker.transform.position.z);
        }
        else
        {
            destinationMarker = Instantiate(destinationMarkerPrefab);
            destinationMarker.transform.position = currentPath.GetDestinationPosition(gameObject);
            destinationMarker.transform.position = new Vector3(destinationMarker.transform.position.x, 1, destinationMarker.transform.position.z);
        }

        pathVisualizer.positionCount = 0;

        pathVisualizer.positionCount = currentPath.GetCornerNodes().Count - currentCornerIndex;

        if (pathVisualizer.positionCount == 0) return;

        pathVisualizer.SetPosition(0, transform.position);

        for (int i = currentCornerIndex - 1; i < currentPath.GetCornerNodes().Count - currentCornerIndex; i++)
        {
            if (i >= 0)
            {
                pathVisualizer.SetPosition(i, new Vector3(currentPath.GetCornerNodes()[i].GetPosition().x, transform.position.y, currentPath.GetCornerNodes()[i].GetPosition().z));
            }
        }
    }

    /// <summary>
    /// Destroys the visualized path
    /// </summary>
    public void DestroyPath()
    {
        if (destinationMarker != null)
        {
            Destroy(destinationMarker);
            destinationMarker = null;
        }
        pathVisualizer.positionCount = 0;
    }

    /// <summary>
    /// Removes the enemy's target point
    /// </summary>
    public void RemoveTargetPoint()
    {
        surroundPoint = null;
    }

    /// <summary>
    /// Virtual function to find a path based on current state
    /// </summary>
    public virtual void FindPath()
    {

    }

    /// <summary>
    /// Sets the path for the AI
    /// </summary>
    /// <param name="path"> Path to set </param>
    public void SetPath(NavPath path)
    {
        currentPath = path;
        SetNextCorner();
    }

    /// <summary>
    /// When setting a new path, it sets the character to the corner after the closest one
    /// This stops jittery back and forth when chasing
    /// </summary>
    public void SetNextCorner()
    {
        if (currentPath == null || currentPath.GetCornerNodes().Count == 0)
        {
            currentCornerIndex = 0;
            return;
        }

        float shortestDist = Mathf.Infinity;
        int closestIndex = 0;
        int currentIndex = 0;

        foreach (Node node in currentPath.GetCornerNodes())
        {
            if (shortestDist > (node.GetPosition(gameObject) - transform.position).magnitude)
            {
                shortestDist = (node.GetPosition(gameObject) - transform.position).magnitude;
                closestIndex = currentIndex;
            }
            currentIndex++;
        }

        if (closestIndex >= currentPath.GetCornerNodes().Count - 1)
        {
            currentCornerIndex = closestIndex;
            return;
        }
        currentCornerIndex = closestIndex + 1; // If not the last node, go to the one past this
    }

    /// <summary>
    /// Set if the enemy is using A* or not
    /// </summary>
    /// <param name="val"> Value to set true/false </param>
    public void SetUsingSearch(bool val)
    {
        usingAStar = val;
    }

    public virtual bool ValidatePoint()
    {
        return true;
    }

    /// <summary>
    /// Checks if the path state is set
    /// </summary>
    /// <returns> True if set, false otherwise </returns>
    public bool HasSetPath()
    {
        if (pathState == PathState.Set) return true;
        return false;
    }

    /// <summary>
    /// Checks if the path state is searching
    /// </summary>
    /// <returns> True if searching, false otherwise </returns>
    public bool IsFindingPath()
    {
        if (pathState == PathState.Searching) return true;
        return false;
    }

    /// <summary>
    /// A function called by the surrounding points to make an enemy attack
    /// </summary>
    /// <param name="points"> Points the request came from </param>
    /// <returns> True if attack is done, false otherwise </returns>
    public virtual bool AttackFromSurrounding(SurroundingPoints points)
    {
        return false;
    }
    /// <summary>
    ///  Returns whether the player is currently controlling this enemy.
    /// </summary>
    public bool IsPlayerControlling() => playerControlling;

    /// <summary>
    /// Gets the priority of an enemy to be added for attacking
    /// </summary>
    /// <returns> Enemy priority </returns>
    public virtual int GetAttackingPriority()
    {
        return pathfindingPriority;
    }

    /// <summary>
    /// Checks if the enemy is surrounding
    /// </summary>
    /// <returns></returns>
    public bool IsSurrounding()
    {
        if (aiState == AIMovementState.Surrounding) return true;
        return false;
    }

    /// <summary>
    /// Set enemy to be targeted or have them retreat
    /// </summary>
    /// <param name="val"> Val determining the actions to take </param>
    public void SetTargeted(bool val)
    {
        if (val)
        {
            aiState = AIMovementState.Targeted;
        }
        else
        {
            aiState = AIMovementState.Retreating;
        }
    }

    /// <summary>
    /// Function to simplify setting movement values in attack coroutines
    /// </summary>
    /// <param name="val"> Value to set movement to </param>
    public void SetMovementValues(bool val)
    {
        if (playerControlling)
        {
            if (val) StartCoroutine(EnableMovement());
            else PlayerController.instance.SetAllowMovement(false);
        }

        if (val)
        {
            overrideBlock = true;
        }
        else
        {
            aiState = AIMovementState.Blocked;
        }
    }

    /// <summary>
    /// Function called every frame to set the correct AI state based on the current information
    /// Alternatively called after attacks/stuns end to allow movement again
    /// </summary>
    public void SetAIState()
    {
        if (overrideBlock || aiState != AIMovementState.Blocked)
        {
            if (overrideBlock)
            {
                overrideBlock = false;
            }
            // Check if player is visible, if not then patrol
            if (LookForPlayer())
            {
                // Check distance first - if it is greater than surrounding then chase
                if (Vector3.Distance(transform.position, currentPlayer.transform.position) >= maxSurroundingRadius + currentPlayer.sizeRadius + sizeRadius)
                {
                    TransitionToState(AIMovementState.Chasing);
                }
                else if (Vector3.Distance(transform.position, currentPlayer.transform.position) <= minSurroundingRadius + currentPlayer.sizeRadius + sizeRadius)
                {
                    TransitionToState(AIMovementState.Retreating);
                }
                else
                {
                    TransitionToState(AIMovementState.Surrounding);
                }
            }
            else TransitionToState(AIMovementState.Patrolling);
        }
    }

    /// <summary>
    /// Function to handle transitions between states
    /// </summary>
    /// <param name="state"> State to switch to </param>
    public void TransitionToState(AIMovementState state)
    {
        if (aiState == state || inProcess) return; // If no transition, do nothing

        if (aiState == AIMovementState.Patrolling) // Reset path
        {
            pathState = PathState.Unset;
        }
        else if (aiState == AIMovementState.Chasing)
        {
            if (state == AIMovementState.Patrolling)
            {
                pathState = PathState.Unset;
                // Do nothing else for now, in future when surroundPoint setting is revamped destroy point
            }
        }
        else if (aiState == AIMovementState.Surrounding)
        {
            if (state == AIMovementState.Patrolling)
            {
                pathState = PathState.Unset;
            }
        }
        else if (aiState == AIMovementState.Retreating)
        {
            if (state == AIMovementState.Patrolling)
            {
                pathState = PathState.Unset;
                // Do nothing else for now, in future when surroundPoint setting is revamped destroy point
            }
        }
        else if (aiState == AIMovementState.Blocked)
        {
            pathState = PathState.Unset;
        }

        aiState = state;
    }

    /// <summary>
    /// Returns alive state of enemy
    /// </summary>
    public bool IsDead => dead;

    /// <summary>
    /// Creates a costly area around the enemy that other enemies will avoid entering
    /// </summary>
    public void CreateLocalSurroundingArea()
    {
        if (dead)
        {
            if (surroundingCostlyNodes.Count > 0)
            {
                ResetSurroundingArea();
            }
            return;
        }

        if (Vector3.Distance(transform.position, previousCostlyPosition) > invalidAreaResetThreshold || surroundingCostlyNodes.Count == 0)
        {
            int numSet = 0;
            ResetSurroundingArea();
            float totalDist = maxSurroundingRadius + sizeRadius;
            List<List<int>> nodes = GraphBuilder.instance.GetNodesInRadius(gameObject, totalDist);
            foreach (List<int> position in nodes)
            {
                Node node = GraphBuilder.instance.GetNodeFromPosition(position);
                float dist = Vector3.Distance(node.GetPosition(gameObject), transform.position);
                float ratio = (totalDist - dist) / totalDist;
                node.AddCost(this, (int)(25 * ratio));

                surroundingCostlyNodes[position] = (int)(25 * ratio);
                numSet++;
            }
            previousCostlyPosition = transform.position;
        }
    }

    /// <summary>
    /// Resets the costly surrounding area values
    /// </summary>
    public void ResetSurroundingArea()
    {
        int numReset = 0;
        foreach (List<int> position in surroundingCostlyNodes.Keys)
        {
            numReset++;
            GraphBuilder.instance.AddNodeCost(position, this, -surroundingCostlyNodes[position]);
        }
        surroundingCostlyNodes = new Dictionary<List<int>, int>();
    }

    /// <summary>
    /// Adds or removes an enemy from the surrounding points
    /// </summary>
    public void ManageSurrounding()
    {
        float dist = Vector3.Distance(transform.position, currentPlayer.transform.position);
        if (dist <= currentPlayer.sizeRadius + sizeRadius + maxSurroundingRadius && dist >= currentPlayer.sizeRadius + minSurroundingRadius + sizeRadius)
        {
            SurroundingPoints.instance.AddSurroundingEnemy(this);
            if (playerControlling) ResetSurroundingArea();
            else CreateLocalSurroundingArea();
        }
        else
        {
            SurroundingPoints.instance.RemoveSurroundingEnemy(this);
            ResetSurroundingArea();
        }
    }
    /// <summary>
    /// Plays the hit sound effect assigned to this enemy. Created as a work around
    /// for enemy ability cost.
    /// </summary>
    /// <param name="damage">The amount of damage taken</param>
    public virtual void DoHitSoundEffect(float damage)
    {
        if (hitEventReference.IsNull) return;
        if (health.CurrentHealth - damage <= 0)
        {
            DoDeathSoundEffect();
            return;
        }
        EventInstance ev = RuntimeManager.CreateInstance(hitEventReference);
        RuntimeManager.AttachInstanceToGameObject(ev, gameObject);
        ev.setParameterByName("Damage", damage / health.GetMaxHealth());
        ev.setParameterByNameWithLabel("Possessed", playerControlling.ToString());
        ev.start();
        ev.release();

    }
    /// <summary>
    /// Plays the death sound effect of this enemy
    /// </summary>
    protected virtual void DoDeathSoundEffect()
    {
        if (deathEventReference.IsNull) return;
        //Stopping any playing sound effects on death.
        if (idleAudio.isValid())
        {
            idleAudio.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        }
        //Play Goblin's Death sound effect
        if (!deathEventReference.IsNull)
        {
            EventInstance ev = RuntimeManager.CreateInstance(deathEventReference);
            ev.setParameterByNameWithLabel("Possessed", playerControlling.ToString());
            RuntimeManager.AttachInstanceToGameObject(ev, gameObject);
            ev.start();
            ev.release();
        }
    }
}
