using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Guard : Enemy
{
    [Header("Guard Settings")]
    [Tooltip("Lance Handle Prefab")]
    [SerializeField] GameObject lanceHandlePrefab;
    [Tooltip("Lance Tip Prefab")]
    [SerializeField] GameObject lanceTipPrefab;
    [Tooltip("Thrust Speed")]
    [SerializeField] float thrustSpeed = 10;
    [Tooltip("Lance Handle Damage")]
    [SerializeField] float lanceHandleDamage = 20;
    [Tooltip("Lance Tip Damage")]
    [SerializeField] float lanceTipDamage = 5;
    [Tooltip("Lance Thrust Duration")]
    [SerializeField] float lanceDuration = 0.5f;

    [SerializeField] AttackStatusEffects lanceTipEffects;
    [SerializeField] AttackStatusEffects lanceHandleEffects;

    [Tooltip("Shield Prefab")]
    [SerializeField] GameObject shieldPrefab;

    [Tooltip("Shield Bash Minimum Speed")]
    [SerializeField] float minimumShieldBashSpeed;
    [Tooltip("Shield Bash Maximum Speed")]
    [SerializeField] float maximumShieldBashSpeed;

    [Tooltip("Shield Bash Minimum Damage")]
    [SerializeField] float minimumShieldBashDamage;
    [Tooltip("Shield Bash Maximum Damage")]
    [SerializeField] float maximumShieldBashDamage;

    [Tooltip("Shield Bash Minimum Knockback")]
    [SerializeField] float minimumShieldBashKnockback;
    [Tooltip("Shield Bash Maximum Knockback")]
    [SerializeField] float maximumShieldBashKnockback;

    [Tooltip("Shield Bash Effects")]
    [SerializeField] AttackStatusEffects shieldBashEffects;

    [Tooltip("Charge Time to Max")]
    [SerializeField] float maxShieldBashChargeTime;
    [Tooltip("Shield Bash Duration")]
    [SerializeField] float bashDuration;

    [Tooltip("Movement Speed When Charging")]
    [SerializeField] float chargingMovementSpeed = 2;

    bool chargingShieldBash = false;

    float currentShieldBashSpeed;
    float currentShieldBashDamage;
    float currentShieldBashKnockback;

    float timeStartedBash;

    [Header("Guard AI Settings")]

    [Tooltip("Number of patrol points")]
    [SerializeField] int numPatrolPoints = 3;

    [Tooltip("Index of current patrol point")]
    private int targetPointIndex = 0;

    [Tooltip("Bool determining if the guard is moving away from its origin")]
    private bool outGoing = false;

    [Tooltip("Sphere prefab representing a patrol point")]
    [SerializeField] GameObject patrolPointPrefab;

    [Tooltip("Patrol points to move through")]
    [SerializeField] List<Vector3> patrolPoints = new List<Vector3>();

    [Tooltip("Editor gameobjects for visually moving points")]
    private List<GameObject> patrolObjs = new List<GameObject>();

    #region Menu Functions

    /// <summary>
    /// Creates the patrol objects either from scratch or from current positions
    /// </summary>
    [ContextMenu("Create Patrol Objects")]
    public void CreatePatrolObjects()
    {
        if (numPatrolPoints > 0)
        {
            if (patrolPoints.Count != numPatrolPoints) // If mismatching number of points create new objects and points
            {
                DeletePatrolObjects();
                patrolPoints = new List<Vector3>();
                for (int i = 0; i < numPatrolPoints; i++)
                {
                    GameObject point = Instantiate(patrolPointPrefab);
                    point.transform.position = new Vector3(transform.position.x + i * 2, transform.position.y + 1, transform.position.z);

                    // Update color of sphere too so that it is a gradient from black towards white
                    patrolObjs.Add(point);
                    patrolPoints.Add(new Vector3(transform.position.x + i * 2, transform.position.y, transform.position.z));
                }
            }
            else if (patrolObjs.Count != patrolPoints.Count) // If object and position counts are mismatched (needs updating)
            {
                DeletePatrolObjects();
                for (int i = 0; i < numPatrolPoints; i++)
                {
                    GameObject point = Instantiate(patrolPointPrefab);
                    point.transform.position = patrolPoints[i];
                    // Update color of sphere too so that it is a gradient from black towards white
                    patrolObjs.Add(point);
                }
            }
        }
    }

    /// <summary>
    /// Sets the patrol points based on position of objects
    /// </summary>
    [ContextMenu("Set Patrol Points")]
    public void SetPatrolPoints()
    {
        patrolPoints = new List<Vector3>();
        for (int i = 0; i < patrolObjs.Count; i++)
        {
            Vector3 point = patrolObjs[i].transform.position;
            patrolPoints.Add(point);
        }
    }

    /// <summary>
    /// Deletes all patrol objects in the scene
    /// </summary>
    [ContextMenu("Delete Patrol Objects")]
    public void DeletePatrolObjects()
    {
        foreach (GameObject obj in patrolObjs)
        {
            DestroyImmediate(obj);
        }
        patrolObjs = new List<GameObject>();
    }

    /// <summary>
    /// Removes all the patrol point positions
    /// </summary>
    [ContextMenu("Destroy all patrol points")]
    public void RemoveAllPoints()
    {
        DeletePatrolObjects();
        patrolPoints = new List<Vector3>();
    }

    #endregion

    // Start is called before the first frame update
    void Start()
    {
        SetPlayerInfo();
        health.SetHealthToMax();
        SetBaseStats();
        sizeRadius = GetComponent<CharacterController>().radius;

        targetPointIndex = 0;
        outGoing = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (dead || lobotimzed) return;
        currentPlayer = target = playerController.GetCurrentCharacter();
        HandleHitStun();
        SetAIState();
        SetBehavior();
        CreateLocalInvalidArea();
    }

    public override void PrimaryAttack()
    {
        GameObject lanceHandle = Instantiate(lanceHandlePrefab, transform);
        lanceHandle.GetComponent<DefaultHitbox>().Init(this, dmg: lanceHandleDamage, forwardVelocity: thrustSpeed, status: lanceHandleEffects, attackDuration: lanceDuration);

        GameObject lanceTip = Instantiate(lanceTipPrefab, transform);
        lanceTip.GetComponent<DefaultHitbox>().Init(this, dmg: lanceTipDamage, status: lanceTipEffects, attackDuration: lanceDuration);
        lanceHandle.GetComponent<DefaultHitbox>().AttachHitbox(lanceTip.GetComponent<DefaultHitbox>());

        timeLastPrimary = Time.time;
        attackingPrimary = true;
    }

    public override void SecondaryAttack()
    {
        chargingShieldBash = true;
        currentShieldBashDamage = minimumShieldBashDamage;
        currentShieldBashKnockback = minimumShieldBashKnockback;
        currentShieldBashSpeed = minimumShieldBashSpeed;

        baseMovementSpeed = movementSpeed;
        movementSpeed = chargingMovementSpeed;
        timeStartedBash = Time.time;
        attackingSecondary = true;

        //if (releaseSecondaryImm) ReleaseSecondary();
        //releaseSecondaryImm = false;
    }

    public void ChargeShieldBash()
    {
        if (chargingShieldBash)
        {
            float timeVal = (Time.time - timeStartedBash) / maxShieldBashChargeTime;

            if (timeVal < 1) // If charging for more than maximum time do nothing
            {
                currentShieldBashDamage = Mathf.Lerp(minimumShieldBashDamage, maximumShieldBashDamage, timeVal);
                currentShieldBashKnockback = Mathf.Lerp(minimumShieldBashKnockback, maximumShieldBashKnockback, timeVal);
                currentShieldBashSpeed = Mathf.Lerp(minimumShieldBashSpeed, maximumShieldBashSpeed, timeVal);
            }
        }
    }

    //public override void ReleaseSecondary()
    //{
    //    base.ReleaseSecondary();
    //    if (!chargingShieldBash) return;

    //    chargingShieldBash = false;
    //    timeLastSecondary = Time.time;
    //    playerController.SetAllowMovement(false);

    //    health.SetInvincible(true);

    //    GameObject hitbox = Instantiate(shieldPrefab, transform);
    //    hitbox.GetComponent<DefaultHitbox>().Init(this, dmg: currentShieldBashDamage, status: shieldBashEffects, attackDuration: bashDuration);
    //    StartCoroutine(HandleBashMovement(hitbox));
    //}

    private IEnumerator HandleBashMovement(GameObject hitbox)
    {
        float timeSinceStarted = 0f;

        while (timeSinceStarted < bashDuration)
        {
            if (hitbox.GetComponent<DefaultHitbox>().HasHitWall())
            {
                StartCoroutine(EnableMovement());
                health.SetInvincible(false);
                movementSpeed = baseMovementSpeed;
                attackingSecondary = false;

                transform.position = transform.position - transform.forward.normalized * currentShieldBashSpeed * Time.deltaTime;

                yield break;
            }

            transform.position = transform.position + transform.forward.normalized * currentShieldBashSpeed * Time.deltaTime;
            timeSinceStarted += Time.deltaTime;
            yield return null;
        }

        transform.position = transform.position + transform.forward.normalized * currentShieldBashSpeed * Time.deltaTime;

        Destroy(hitbox);

        StartCoroutine(EnableMovement());
        health.SetInvincible(false);
        movementSpeed = baseMovementSpeed;
        attackingSecondary = false;
    }

    public override Vector3 GetCurrentSpeedVector()
    {
        return currentShieldBashSpeed * transform.forward.normalized;
    }

    public override bool CheckSecondaryUsable()
    {
        if (chargingShieldBash) return false;
        return base.CheckSecondaryUsable();
    }

    /// <summary>
    /// Function that follows the flow chart to set behavior for the guard
    /// </summary>
    public override void SetBehavior()
    {
        if (playerControlling || inProcess) return;

        if (aiState == AIMovementState.Patrolling) // If patrolling
        {
            Patrol();
        }
        else if (aiState == AIMovementState.Chasing)
        {
            Chase();
        }
        else if (aiState == AIMovementState.Surrounding)
        {
            Surround();
        }
        else if (aiState == AIMovementState.Retreating)
        {
            Retreat();
        }
    }

    /// <summary>
    /// Handles finding a path in the graph based on the state
    /// </summary>
    public override void FindPath()
    {
        if (aiState == AIMovementState.Patrolling)
        {
            if (pathState == PathState.Unset)
            {
                pathState = PathState.Searching;
                SetPatrollingPoint();
            }

        }
        else if (aiState == AIMovementState.Chasing)
        {
            StartCoroutine(SurroundingPoints.instance.FindPathToPlayer(this, false));
        }
        else if (aiState == AIMovementState.Surrounding) // Handles the same as chasing, just in closer range
        {
            StartCoroutine(SurroundingPoints.instance.FindPathToPlayer(this, true));
        }
        else if (aiState == AIMovementState.Retreating) // Handles the same as chasing, just in closer range
        {
            StartCoroutine(SurroundingPoints.instance.FindPathToPlayer(this, true));
        }
    }

    /// <summary>
    /// Override function handling patrol functionality for the Guard
    /// This patrol method goes to points along a path, then follows the path back to the start
    /// </summary>
    public override void Patrol()
    {
        // Check if player is visible
        if (LookForPlayer())
        {
            StartCoroutine(SpotPlayer());
            return;
        }

        AIMove();
        AILook();

        if (pathState == PathState.Set)
        {
            if (currentPath.ReachedDestination(this)) // If we are within stopping range
            {
                pathState = PathState.Unset;
                StartCoroutine(LookAround()); // Look around
            }

            if (debugging)
            {
                UpdatePath();
            }
        }
        else // If no current path, mark as available
        {
            reachedWalkpoint = false;
        }
    }

    /// <summary>
    /// Override function for setting a patrol point
    /// This version uses a pre-made list of points and selects points in sequence for a route
    /// </summary>
    public void SetPatrollingPoint()
    {
        walkPoint = patrolPoints[targetPointIndex];
        StartCoroutine(GraphBuilder.instance.AStarSearch(this, transform.position, walkPoint));
    }

    /// <summary>
    /// Validates a walkpoint
    /// </summary>
    /// <returns> True if reachable </returns>
    public override bool ValidatePoint()
    {
        if (currentPath == null)
        {
            pathState = PathState.Unset;
            return false;
        }

        if (!currentPath.PathComplete())
        {
            pathState = PathState.Unset;
            return false;
        }

        pathState = PathState.Set;
        return true;
    }

    /// <summary>
    /// Coroutine to handle the guard when it reaches it's patrol point
    /// </summary>
    /// <returns> Waits for animation to be done and looks for player </returns>
    private IEnumerator LookAround()
    {
        if (debugging)
        {
            DestroyPath();
        }

        inProcess = true;
        // AnimateIdle(); // Play animation (temporarily idle)
        float timer = 0;

        // If we are at 0 or end of points, flip outgoing
        if (targetPointIndex == 0) outGoing = true;
        else if (targetPointIndex == patrolPoints.Count - 1) outGoing = false;

        if (outGoing) targetPointIndex++; // If outgoing increase index
        else targetPointIndex--; // Otherwise decrease index

        targetPointIndex = Mathf.Clamp(targetPointIndex, 0, patrolPoints.Count - 1);

        while (timer < 1) // Wait 1 second for now, will change this to be a bool checking the end of looking animation
        {
            if (LookForPlayer())
            {
                TransitionToState(AIMovementState.Chasing);
                yield break;
            }
            timer += Time.deltaTime;
            yield return null;
        }

        inProcess = false;
        if (debugging)
        {
            StartPath();
        }
    }

    /// <summary>
    /// Coroutine that plays when the player is spotted
    /// </summary>
    /// <returns> Waits for animation to be done </returns>
    private IEnumerator SpotPlayer()
    {
        aiState = AIMovementState.Chasing;
        if (debugging)
        {
            DestroyPath();
        }

        inProcess = true;

        timePlayerLastSeen = Time.time;

        inProcess = false;
        yield break;
    }

    /// <summary>
    /// Chase function for the Guard - should set paths that focus on surrounding the player
    /// </summary>
    public override void Chase()
    {
        lookAtPlayer = false;

        if (pathState == PathState.Set || (pathState == PathState.Searching && currentPath != null))
        {
            AIMove();
            if (debugging)
            {
                UpdatePath();
            }
        }
        AILook();
    }

    /// <summary>
    /// Function handling tasks when surrounding
    /// </summary>
    public void Surround()
    {
        lookAtPlayer = true;

        if (pathState == PathState.Set || (pathState == PathState.Searching && currentPath != null))
        {
            if (Vector3.Distance(transform.position, currentPlayer.transform.position) > chaseToSurroundingRadius)
            {
                AIMove();
                if (debugging)
                {
                    UpdatePath();
                }
            }
        }
        AILook();
    }

    /// <summary>
    /// Retreat from close distance, get back to surrounding
    /// </summary>
    public void Retreat()
    {
        lookAtPlayer = true;

        if (pathState == PathState.Set || (pathState == PathState.Searching && currentPath != null))
        {
            AIMove();
            if (debugging)
            {
                UpdatePath();
            }
        }
        AILook();
    }

    /// <summary>
    /// Handles Guard attacking chance and triggering
    /// </summary>
    /// <param name="points"> The points calling this function </param>
    /// <returns> True if attacking, false otherwise </returns>
    public override bool AttackFromSurrounding(SurroundingPoints points)
    {
        float totalOdds = 0;

        // For now keep as just primary, once more combat is done then if being attacked it will block
        if (CheckPrimaryUsable())
        {
            totalOdds += primaryAttackChance;
        }

        if (totalOdds > 0)
        {
            PrimaryAttack();
            points.RemoveSurroundingEnemy(this);
            ResetSurroundingArea();
            return true;
        }
        else
        {
            return false;
        }
    }
}
