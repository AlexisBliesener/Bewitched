using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Enemy;

public class Guard : Enemy
{
    [Header("Guard Settings")]
    [Tooltip("Lance Handle Prefab")]
    [SerializeField] GameObject lanceHandlePrefab;
    [Tooltip("Lance Tip Prefab")]
    [SerializeField] GameObject lanceTipPrefab;
    [Tooltip("Thrust Speed")]
    [SerializeField] float thrustSpeed = 20;
    [Tooltip("Lance Handle Damage")]
    [SerializeField] float lanceHandleDamage = 20;
    [Tooltip("Lance Tip Damage")]
    [SerializeField] float lanceTipDamage = 5;
    [Tooltip("Lance Thrust Duration")]
    [SerializeField] float lanceDuration = 0.5f;
    [Tooltip("Lance range")]
    [SerializeField] float lanceRange = 1.2f;

    [SerializeField] AttackStatusEffects lanceTipEffects;
    [SerializeField] AttackStatusEffects lanceHandleEffects;

    [Tooltip("Shield Prefab")]
    [SerializeField] GameObject shieldPrefab;

    [Tooltip("Shield raise time")]
    [SerializeField] float shieldRaiseTime = 0.2f;
    [Tooltip("Shield lower time")]
    [SerializeField] float shieldLowerTime = 0.3f;
    [Tooltip("Angle range for AI to lower shield")]
    [SerializeField] float aiShieldAngleThreshold = 20;
    [Tooltip("AI Time delay before shield drops")]
    [SerializeField] float aiShieldDropDelay = 0.5f;
    [Tooltip("Shield knockback value")]
    [SerializeField] float shieldKnockbackAmount = 8;

    private float timeLastValidShield = 0;

    [Tooltip("Guard animator script that controls the guard animations")]
    private GuardAnimator guardAnimator; 

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

    private Vector3 targetPos;

    private GameObject shieldObject;

    [Tooltip("Shield status enum")]
    private enum ShieldStatus
    {
        Lowered,
        Raising,
        Raised,
        Lowering
    }

    [Tooltip("The status of the shield for the guard")]
    private ShieldStatus shieldStatus = ShieldStatus.Lowered;

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
        guardAnimator = GetComponent<GuardAnimator>();
    }

    // Update is called once per frame
    protected override void FixedUpdate()
    {
        if (dead || lobotimzed) return;
        ManageSurrounding();
        ResetAttackingArea();
        currentPlayer = target = playerController.GetCurrentCharacter();
        SetAIState();
        SetBehavior();
        CreateLocalInvalidArea();
        HandleAutoShield();
        SetDebugString();
        if (playerControlling)
        {
            lockedCharacter = PlayerController.instance.GetLockedTarget();
        }
        else
        {
            lockedCharacter = currentPlayer;
        }

        base.FixedUpdate();
    }

    /// <summary>
    /// Function to handle the rotation of an AI guard
    /// </summary>
    public override void AILook()
    {
        if (aiState == AIMovementState.PlayerControlled || playerControlling || shieldStatus != ShieldStatus.Lowered) return;

        Quaternion lookRotation;
        if (aiState == AIMovementState.Surrounding || aiState == AIMovementState.Retreating) // If surrounding then look at player
        {
            lookRotation = Quaternion.LookRotation(Vector3.Lerp(transform.forward, currentPlayer.transform.position - transform.position, GetRotationSpeed() * Time.deltaTime));
        }
        else
        {
            lookRotation = Quaternion.LookRotation(Vector3.Lerp(transform.forward, new Vector3(velocity.x, 0, velocity.z), GetRotationSpeed() * Time.deltaTime));
        }
        transform.rotation = lookRotation;
    }

    /// <summary>
    /// Starts the primary attack
    /// Chooses between windup and regular hit
    /// </summary>
    public override IEnumerator BeginPrimary()
    {
        while (shieldStatus != ShieldStatus.Lowered)
        {
            if (shieldStatus == ShieldStatus.Raised) ReleaseSecondary();
            yield return null;
        }

        if (gameObject != null)
        {
            if (playerControlling)
            {
                if (!inPrimaryWindup && (currentPrimaryComboStep == -1 || Time.time - timeLastPrimary >= primaryComboMinTime[currentPrimaryComboStep == -1 ? 0 : currentPrimaryComboStep] / guardAnimator.GetPrimaryComboMult(currentPrimaryComboStep == -1 ? 0 : currentPrimaryComboStep)))
                {
                    health.SubHealth(primaryAttackCost, this);
                    currentPrimaryComboStep += 1;
                    if (currentPrimaryComboStep >= primaryComboSteps)
                    {
                        currentPrimaryComboStep = 0;
                    }

                    if(lockedCharacter != null && Vector3.Distance(lockedCharacter.transform.position, this.gameObject.transform.position) > moveToTargetDistance)
                    {
                        currentPrimaryComboStep = 0;
                    }

                    timeLastPrimary = Time.time;

                    guardAnimator.SwitchState("PrimaryAttack", currentPrimaryComboStep);
                    yield return StartCoroutine(guardAnimator.WaitForDelay("PrimaryAttack", currentPrimaryComboStep));
                    PrimaryAttack();
                }
            }
            else
            {
                currentPrimaryComboStep = -1;
                timeLastPrimary = Time.time;
                guardAnimator.SwitchState("PrimaryAttack", 0);
                yield return StartCoroutine(guardAnimator.WaitForDelay("PrimaryAttack", 0));
                PrimaryAttack();
            }
        }
    }

    /// <summary>
    /// Starts the lance thrust
    /// </summary>
    public override void PrimaryAttack()
    {
        hitCharacter = false;
        SetMovementValues(false);

        if (lockedCharacter)
        {
            lockedCharacter.SetAttacker(this);
            if (lockedCharacter.TryGetComponent(out Enemy enemy))
            {
                enemy.SetTargeted(true);
            }
        }

        Character tempLockedChar = lockedCharacter;

        attackingPrimary = true;

        if (playerControlling)
        {
            if (tempLockedChar != null && Vector3.Distance(tempLockedChar.transform.position, this.gameObject.transform.position) > moveToTargetDistance)
            {
                inPrimaryWindup = true;
                attackStateCoroutine = StartCoroutine(LanceWindup(tempLockedChar));
            }
            else
            {
                attackStateCoroutine = StartCoroutine(HandleLanceThrust(tempLockedChar));
            }
        }
        else
        {
            inPrimaryWindup = true;
            attackStateCoroutine = StartCoroutine(LanceWindup(tempLockedChar));
        }
    }

    /// <summary>
    /// Starts the windup for the lance
    /// </summary>
    public IEnumerator LanceWindup(Character tempLockedCharacter)
    {
        inCounter = false;
        attackState = AttackState.Windup;
        // save the current position to use the y value later
        targetPos = transform.position;
        float windupStart = Time.time;

        while (Time.time - windupStart < 0.708 / guardAnimator.GetPrimaryWindupMult())
        {
            SetMovementValues(false);
            if (tempLockedCharacter)
            {
                Vector3 direc = tempLockedCharacter.transform.position - transform.position;
                direc.y = 0;
                Quaternion rotationVal = Quaternion.LookRotation(direc.normalized);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, rotationVal, rotationalVelocity);
            }

            yield return null;
        }

        if (playerControlling) // Since the player should only be controlling here if possessed at this point, reset target if player controlled
        {
            tempLockedCharacter = PlayerController.instance.GetLockedTarget();
        }

        attackStateCoroutine = StartCoroutine(LanceApproach(tempLockedCharacter));
    }

    /// <summary>
    /// Approach function for thrusting
    /// </summary>
    public IEnumerator LanceApproach(Character tempLockedCharacter)
    {
        attackState = AttackState.Approaching;
        inPrimaryWindup = false;
        bool triggerSet = false;
        if (tempLockedCharacter)
        {
            float dis = Vector3.Distance(tempLockedCharacter.transform.position, transform.position);
            Vector3 direction = (tempLockedCharacter.transform.position - transform.position).normalized;
            float oldY = targetPos.y;
            targetPos = tempLockedCharacter.transform.position - direction * (GetCharacterController().radius + tempLockedCharacter.GetCharacterController().radius + lanceRange);
            float buffer = sizeRadius + lanceRange;
            RaycastHit hit;
            // Raycast to check for environment collision
            if (Physics.Raycast(transform.position + (direction * buffer), direction, out hit, dis, characters)) // Use buffer for characters so ray doesn't hit self
            {
                //Debug.Log(hit.collider.gameObject);
                // Move just before character hit point
                targetPos = hit.point - direction * buffer;
            }
            if (Physics.Raycast(transform.position, direction, out hit, dis, environmentLayer)) // Use position for environment as that can be thinner
            {
                //Debug.Log(hit.collider.gameObject);
                // Move just before environment hit point if beyond buffer, stay at same position otherwise
                if ((hit.point - transform.position).magnitude < buffer) targetPos = transform.position;
                else targetPos = hit.point - direction * buffer;
            }
            targetPos.y = oldY;
            dis = (targetPos - transform.position).magnitude;
            SetCostlyAttackingLine(direction, dis, 1.5f * sizeRadius);
            transform.DOMove(targetPos, chaseTime * dis);
            transform.DOLookAt(targetPos, chaseTime * dis);

            float timeStarted = Time.time;
            timeLastPrimary = Time.time + chaseTime * dis * counterWindowLength;

            if (playerControlling)
            {
                if (tempLockedCharacter != null)
                {
                    CameraController.instance.OnAttack(tempLockedCharacter.transform.position - transform.position, chaseTime * dis);
                }
                else
                {
                    CameraController.instance.OnAttack(transform.forward, chaseTime * dis);
                }
            }

            while (Time.time - timeStarted < chaseTime * dis)
            {
                if (tempLockedCharacter == null || Vector3.Distance(transform.position, tempLockedCharacter.transform.position) < sizeRadius + lanceRange)
                {
                    DOTween.Kill(gameObject); // Kill tweens if we are too close
                    targetPos = transform.position;
                    guardAnimator.ExitPrimaryWindup();
                }
                else if (tempLockedCharacter == null)
                {
                    guardAnimator.ExitPrimaryWindup();
                }

                if (Time.time - timeStarted >= counterWindowLength * chaseTime * dis) //  not dodgable
                {
                    if (!triggerSet)
                    {
                        guardAnimator.ExitPrimaryWindup();
                        triggerSet = true;
                    }

                    if (counterIndicatorVFX != null)
                    {
                        DestroyCounterIndicator();
                        if (PlayerController.instance.GetCounterAvailable() == this) PlayerController.instance.SetCounterAvaliable(null);
                    }
                }
                else // attack is dodgable
                {
                    if (counterIndicatorVFX == null)
                    {
                        counterIndicatorVFX = Instantiate(counterIndicatorVFXPrefab, transform);
                        counterIndicatorVFX.transform.localPosition = new Vector3(0, 2.5f, 0);
                        PlayerController.instance.SetCounterAvaliable(this);
                    }
                }
                SetMovementValues(false);
                GetCharacterController().enabled = false;
                yield return null;
            }

            if (targetPos != Vector3.negativeInfinity)
            {
                transform.position = targetPos;
            }
            GetCharacterController().enabled = true;
        }

        if (!triggerSet)
        {
            guardAnimator.ExitPrimaryWindup();
        }

        if (counterIndicatorVFX != null)
        {
            DestroyCounterIndicator();
        }

        attackState = AttackState.Attacking;

        GameObject lanceHandle = Instantiate(lanceHandlePrefab, transform);
        lanceHandle.GetComponent<DefaultHitbox>().Init(this, dmg: lanceHandleDamage, forwardVelocity: thrustSpeed, status: lanceHandleEffects, attackDuration: lanceDuration);
        lanceHandle.transform.position += transform.right * 0.25f;

        GameObject lanceTip = Instantiate(lanceTipPrefab, transform);
        lanceTip.GetComponent<DefaultHitbox>().Init(this, dmg: lanceTipDamage, status: lanceTipEffects, attackDuration: lanceDuration);
        lanceHandle.GetComponent<DefaultHitbox>().AttachHitbox(lanceTip.GetComponent<DefaultHitbox>());
        lanceTip.transform.position += transform.right * 0.25f;

        targetPos = Vector3.negativeInfinity;

        float hitboxStartTime = Time.time;

        while (Time.time - hitboxStartTime < 0.25f / guardAnimator.GetPrimaryComboMult(currentPrimaryComboStep == -1 ? 0 : currentPrimaryComboStep))
        {
            SetMovementValues(false);
            yield return null;
        }

        if (!playerControlling)
        {
            if (!hitCharacter) // If missed, vulnerable for half a second
            {
                float timeStart = Time.time;
                while (Time.time - timeStart > 0.1f)
                {
                    SetMovementValues(false);
                    yield return null;
                }
            }

            guardAnimator.EndPrimary();
        }

        SetMovementValues(true);

        attackState = AttackState.Neutral;
        pathState = PathState.Unset;
        aiState = AIMovementState.Retreating;


        if (tempLockedCharacter)
        {
            tempLockedCharacter.SetAttacker(null);
            if (tempLockedCharacter.TryGetComponent(out Enemy enemy))
            {
                enemy.SetTargeted(false);
            }
        }

        lockedCharacter = null;
        attackingPrimary = false;
        SurroundingPoints.instance.RemoveAttackingEnemy(this);

        yield break;
    }

    /// <summary>
    /// Coroutine handling the AI state changes, AI delay, and locking movement for the player when thrusting
    /// </summary>
    /// <returns> Time breaks </returns>
    public IEnumerator HandleLanceThrust(Character tempLockedCharacter)
    {
        guardAnimator.SetPrimaryMovementNeeded(false);
        attackState = AttackState.Attacking;

        GameObject lanceHandle = Instantiate(lanceHandlePrefab, transform);
        lanceHandle.GetComponent<DefaultHitbox>().Init(this, dmg: lanceHandleDamage, forwardVelocity: thrustSpeed, status: lanceHandleEffects, attackDuration: lanceDuration);
        lanceHandle.transform.position += transform.right * 0.25f;

        GameObject lanceTip = Instantiate(lanceTipPrefab, transform);
        lanceTip.GetComponent<DefaultHitbox>().Init(this, dmg: lanceTipDamage, status: lanceTipEffects, attackDuration: lanceDuration);
        lanceHandle.GetComponent<DefaultHitbox>().AttachHitbox(lanceTip.GetComponent<DefaultHitbox>());
        lanceTip.transform.position += transform.right * 0.25f;

        if (playerControlling)
        {
            CameraController.instance.OnAttack(transform.forward, 0.01f);
        }

        RaycastHit hitInfo;
        Vector3 moveDist;
        Vector3 direction;
        if (PlayerController.instance.movementInputV3 != Vector3.zero)
        {
            direction = Camera.main.transform.TransformVector(PlayerController.instance.movementInputV3);
        }
        else
        {
            direction = PlayerController.instance.currentCharacter.transform.forward;
        }

        direction.y = 0f; // Prevent tilting
        if (Physics.Raycast(PlayerController.instance.currentCharacter.transform.position, direction, out hitInfo, nonLockPrimaryMovement + GetCharacterController().radius * 1.1f, environmentLayer))
        {
            moveDist = (direction.normalized * (hitInfo.distance - GetCharacterController().radius * 1.1f));
        }
        else
        {
            moveDist = (direction.normalized * nonLockPrimaryMovement);
        }
        transform.DOMove(PlayerController.instance.currentCharacter.transform.position + moveDist, 0.25f / guardAnimator.GetPrimaryComboMult(currentPrimaryComboStep == -1 ? 0 : currentPrimaryComboStep));
        transform.DOLookAt(PlayerController.instance.currentCharacter.transform.position + moveDist, 0.25f / guardAnimator.GetPrimaryComboMult(currentPrimaryComboStep == -1 ? 0 : currentPrimaryComboStep));

        float hitboxStartTime = Time.time;

        while (Time.time - hitboxStartTime < 0.25f / guardAnimator.GetPrimaryComboMult(currentPrimaryComboStep == -1 ? 0 : currentPrimaryComboStep))
        {
            SetMovementValues(false);
            yield return null;
        }

        if (!playerControlling)
        {
            if (!hitCharacter) // If missed, vulnerable for half a second
            {
                yield return new WaitForSeconds(0.5f);
            }
        }

        SetMovementValues(true);

        attackState = AttackState.Neutral;
        pathState = PathState.Unset;
        aiState = AIMovementState.Retreating;

        if (tempLockedCharacter)
        {
            tempLockedCharacter.SetAttacker(null);
            if (tempLockedCharacter.TryGetComponent(out Enemy enemy))
            {
                enemy.SetTargeted(false);
            }
        }

        if (!playerControlling)
        {
            guardAnimator.EndPrimary();
        }

        attackingPrimary = false;
        SurroundingPoints.instance.RemoveAttackingEnemy(this);
    }

    public override IEnumerator BeginSecondary()
    {
        guardAnimator.SwitchState("SecondaryAttack");
        yield return StartCoroutine(characterAnimator.WaitForDelay("SecondaryAttack", 0));
        if (gameObject)
        {
            if (PlayerController.instance.currentCharacter == this)
            {
                health.SubHealth(secondaryAttackCost, this);
            }
            SecondaryAttack();

        }
    }

    public override void SecondaryAttack()
    {
        StartCoroutine(RaiseShield());
    }

    /// <summary>
    /// Raises the shield
    /// </summary>
    public IEnumerator RaiseShield()
    {
        if (shieldStatus == ShieldStatus.Lowering || shieldStatus == ShieldStatus.Raised || shieldStatus == ShieldStatus.Raising) yield break;
        shieldStatus = ShieldStatus.Raising;
        float timeStarted = Time.time;
        while (Time.time - timeStarted < shieldRaiseTime)
        {
            if (shieldStatus == ShieldStatus.Lowering) yield break;
            yield return null;
        }

        if (shieldStatus == ShieldStatus.Raising && shieldObject == null)
        {
            shieldObject = Instantiate(shieldPrefab, transform);
            shieldObject.transform.position += transform.forward * sizeRadius;
            shieldObject.GetComponent<ShieldHitbox>().Init(this, attackDuration: Mathf.Infinity);
            shieldStatus = ShieldStatus.Raised;
            timeLastValidShield = Time.time;
            shieldObject.GetComponent<ShieldHitbox>().SetKnockbackAmount(shieldKnockbackAmount);
        }
    }

    /// <summary>
    /// Releases the shield
    /// </summary>
    public override void ReleaseSecondary()
    {
        guardAnimator.SetSecondaryAttackEnded();
        StartCoroutine(LowerShield());
    }

    public IEnumerator LowerShield()
    {
        shieldStatus = ShieldStatus.Lowering;
        if (shieldObject)
        {
            Destroy(shieldObject);
            shieldObject = null;
        }
        float timeStarted = Time.time;
        while (Time.time - timeStarted < shieldLowerTime)
        {
            shieldStatus = ShieldStatus.Lowering;
            yield return null;
        }
        shieldStatus = ShieldStatus.Lowered;
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
    public override IEnumerator FindPath()
    {
        if (aiState == AIMovementState.Patrolling)
        {
            if (pathState == PathState.Unset)
            {
                pathState = PathState.Searching;
                yield return StartCoroutine(SetPatrollingPoint());
            }

        }
        else if (aiState == AIMovementState.Chasing)
        {
            yield return StartCoroutine(SurroundingPoints.instance.FindPathToPlayer(this, false));
        }
        else if (aiState == AIMovementState.Surrounding) // Handles the same as chasing, just in closer range
        {
            yield return StartCoroutine(SurroundingPoints.instance.FindPathToPlayer(this, true));
        }
        else if (aiState == AIMovementState.Retreating) // Handles the same as chasing, just in closer range
        {
            yield return StartCoroutine(SurroundingPoints.instance.FindPathToRetreat(this));
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
    public IEnumerator SetPatrollingPoint()
    {
        walkPoint = patrolPoints[targetPointIndex];
        yield return StartCoroutine(GraphBuilder.instance.AStarSearch(this, transform.position, walkPoint));
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
            AIMove();
            if (debugging)
            {
                UpdatePath();
            }
        }
        AILook();
    }

    /// <summary>
    /// Retreat from close distance, get back to surrounding
    /// </summary>sec
    public void Retreat()
    {
        lookAtPlayer = true;

        if ((pathState == PathState.Set || (pathState == PathState.Searching && currentPath != null)) && Time.time - timeSinceRetreat > retreatWaitTime)
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
    /// <returns> Cost of attack done </returns>
    public override int AttackFromSurrounding(SurroundingPoints points)
    {
        if (dead || lobotimzed) return 0;
        float totalOdds = 0;
        float remaining = points.GetAvailableAttackPoints();

        // For now keep as just primary, once more combat is done then if being attacked it will block
        if (CheckPrimaryUsable() && primaryAICost <= remaining)
        {
            totalOdds += primaryAttackChance;
        }

        if (totalOdds > 0)
        {
            StartCoroutine(BeginPrimary());
            points.AddAttackingEnemy(this, primaryAICost);
            return primaryAICost;
        }
        else
        {
            return 0;
        }
    }

    /// <summary>
    /// Override for speed getter
    /// </summary>
    /// <returns> Speed depending on shielding status </returns>
    public override float GetSpeed()
    {
        if (shieldStatus != ShieldStatus.Lowered) return base.GetSpeed() / 3f;
        return base.GetSpeed();
    }

    /// <summary>
    /// Override for rotation speed getter
    /// </summary>
    /// <returns> Rotation speed depending on shield status </returns>
    public override float GetRotationSpeed()
    {
        if (shieldStatus != ShieldStatus.Lowered) return 0;
        return base.GetRotationSpeed();
    }

    /// <summary>
    /// Handles the AI raising and lowering the shield based on distance, attack state, and player controlling state
    /// </summary>
    public void HandleAutoShield()
    {
        float dist = Vector3.Distance(transform.position, currentPlayer.transform.position);
        float angle = Vector3.Angle(currentPlayer.transform.position - transform.position, transform.forward);
        if (angle < aiShieldAngleThreshold / 4 && dist <= (currentPlayer.sizeRadius + sizeRadius + maxSurroundingRadius) && attackState == AttackState.Neutral && !playerControlling)
        {
            if (shieldStatus == ShieldStatus.Lowered)
            {
                StartCoroutine(BeginSecondary());
            }
            if (shieldStatus == ShieldStatus.Raised)
            {
                timeLastValidShield = Time.time;
            }
        }
        else if (!playerControlling && shieldStatus == ShieldStatus.Raised && Time.time - timeLastValidShield > aiShieldDropDelay)
        {
            ReleaseSecondary();
        }
    }


    /// <summary>
    /// Sets if the player is controlling this enemy
    /// </summary>
    /// <param name="val"> Value to set </param>
    public override void SetControlled(bool val)
    {
        base.SetControlled(val);
         ReleaseSecondary();
    }

    public override IEnumerator StartHitStun(float duration)
    {
        if (duration > 0)
        {
            if (stunned) yield break;
            if (hitStunActual != null) Destroy(hitStunActual);
            hitStunActual = Instantiate(hitStunPrefab, transform);
            stunned = true;
            float timeStarted = Time.time;
            while (Time.time - timeStarted < duration)
            {
                SetMovementValues(false);
                yield return null;
            }
            inPrimaryWindup = false;
            if (attackingPrimary) // Reset primary and secondary abilities so enemies don't break
            {
                attackingPrimary = false;
                timeLastPrimary = Time.time;
            }
            if (attackingSecondary)
            {
                attackingSecondary = false;
                timeLastSecondary = Time.time;
                ReleaseSecondary();
            }
            SetMovementValues(true);
            if (attackState != AttackState.Neutral)
            {
                attackState = AttackState.Neutral;
                SurroundingPoints.instance.RemoveAttackingEnemy(this);
            }
            stunned = false;
            Destroy(hitStunActual);
            hitStunActual = null;
        }
    }
}
