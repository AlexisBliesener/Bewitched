using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using FMOD.Studio;
using FMODUnity;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.AI;


public class Goblin : Enemy
{
    [Header("References/Prefabs"), ShowIf("dev")]
    [Tooltip("Knife Prefab")]
    [SerializeField] GameObject knifePrefab;
    [Tooltip("Dash Hitbox"), ShowIf("dev")]
    [SerializeField] GameObject dashHitbox;
    [Tooltip("Dash Effects"), ShowIf("dev")]
    [SerializeField] AttackStatusEffects dashEffects;
    [Tooltip("Spin Hitbox"), ShowIf("dev")]
    [SerializeField] GameObject spinHitbox;
    [Tooltip("Spin Effects"), ShowIf("dev")]
    [SerializeField] AttackStatusEffects spinEffects;
    [Tooltip("Knife Effects"), ShowIf("dev")]
    [SerializeField] AttackStatusEffects[] knifeEffects;
    [Header("Knife Settings for Goblin")]
    [Tooltip("Knife duration")]
    [SerializeField] float knifeDuration = 0.25f;
    [Tooltip("Thrust Speed")]
    [SerializeField] float[] thrustSpeed = { 10 };
    [Tooltip("Knife Damage")]
    [SerializeField] float[] knifeDamage = { 20 };
    [Header("Dash Settings for Goblin")]
    [Tooltip("Dash Speed"), Range(0, 100)]
    [SerializeField] float dashSpeed = 50;
    [Tooltip("Dash Duration"), Range(0, 10)]
    [SerializeField] float dashDuration = 0.5f;
    [Tooltip("Dash Damage"), Range(0, 200)]
    [SerializeField] float dashDamage = 30;
    [Tooltip("Offset of the hitbox forward"), Range(0, 10)]
    [SerializeField] private float offSetForward = 0.5f;
    [Header("Spin Settings for Goblin")]
    [Tooltip("Spin Damage"), Range(0, 200)]
    [SerializeField] float spinDamage = 30;
    [Tooltip("Distance to dodge in first part of spin"), Range(0, 10)]
    [SerializeField] float spinDodgeDistance = 2;
    [Tooltip("Input time after dodging for the spin dodge"), Range(0, 10)]
    [SerializeField] float spinDodgeInputTime = .75f;
    [Tooltip("Distance for first spin jump"), Range(0, 10)]
    [SerializeField] float spinDistance = 8;
    [Tooltip("Distance dropoff per bounce"), Range(0, 10)]
    [SerializeField] float spinDistanceDropoff = 2.5f;
    [Tooltip("Spin Duration"), Range(0, 50)]
    [SerializeField] float spinDuration = 10;
    [Tooltip("Spin Speed"), Range(0, 100)]
    [SerializeField] float spinSpeed = 15;
    [Tooltip("Spin Rotational Speed"), Range(0, 360)]
    [SerializeField] float spinRotationalSpeed = 120;
    [Tooltip("Standard Acceleration Period"), Range(0, 10)]
    [SerializeField] float standardAccelerationPeriod = 0.5f;
    [Tooltip("Low Health Acceleration Period"), Range(0, 10)]
    [SerializeField] float lowHealthAccelerationPeriod = 0.25f;
    [Tooltip("Low Health Angle Variation Range"), Range(0, 360)]
    [SerializeField] float lowHealthAngleRange = 80; // maximum 40 degree change
    [Tooltip("Maximum drift speed"), Range(0, 10)]
    [SerializeField] float maxDriftSpeed = 4;
    [Tooltip("The max angular distance a deflect will auto-target the player on wall/character spin collisions"), Range(0, 360)]
    [SerializeField] float maxSpinDeflectAngle = 30;

    [Header("Goblin AI Settings")]
    [Tooltip("Minimum Patrol Distance"), Range(0, 100)]
    [SerializeField] float minPatrolDistance = 3;
    [Tooltip("Maximum Patrol Distance"), Range(0, 100)]
    [SerializeField] float maxPatrolDistance = 5;
    [Tooltip("Range the Goblin can communicate with other Goblins"), Range(0, 100)]
    [SerializeField] float communicationRange = 8;

    [Tooltip("Goblin animator script that controls the goblin animations")]
    private GoblinAnimator animator;
    [Tooltip("The position the goblin will try to move to on attack")]
    private Vector3 targetPos = Vector3.negativeInfinity;

    //The sound effect for the spin attack
    //FMOD Event for idle sound effects
    EventInstance idleAudio;

    private int numDeflections = 0;

    private void Start()
    {
        animator = GetComponentInChildren<GoblinAnimator>();
        primaryComboSteps = 3;
        SetPlayerInfo();
        health.SetHealthToMax();
        SetBaseStats();
        SetAgentValues();
        SetDebuggingValues();
        SetPatrolOrigin();

        aiState = AIMovementState.Patrolling;

        // Set update position to false so the agent does not try to move the character since we are controlling it (AiMove function)
        agent.updatePosition = false;
        agent.updateRotation = false;
        agent.enabled = false; // Disable navmesh agent since we are not using it at all
    }

    protected void FixedUpdate()
    {
        currentPlayer = playerController.GetCurrentCharacter();

        SetBehavior();

        if(playerControlling)
        {
            lockedCharacter = PlayerController.instance.GetLockedTarget();
        }
        else
        {
            lockedCharacter = currentPlayer;
        }

        if (lockedCharacter != null && targetPos != Vector3.negativeInfinity)
        {
            animator.SetPrimaryMovementNeeded(true);
        }
        else
        {
            animator.SetPrimaryMovementNeeded(false);
        }
    }

    public override void PrimaryAttack()
    {
        hitCharacter = false;
        if (playerControlling)
        {
            PlayerController.instance.SetAllowMovement(false);
        }
        else
        {
            aiState = AIMovementState.Blocked;
            attackIndicator = Instantiate(attackIndicatorPrefab, transform);
            attackIndicator.transform.localPosition = new Vector3(0, 2.5f, 0);
        }

        if (lockedCharacter)
        {
            lockedCharacter.SetAttacker(this);
            if (lockedCharacter.TryGetComponent(out Enemy enemy))
            {
                enemy.SetTargeted(true);
            }
        }
        attackingPrimary = true;

        if (lockedCharacter != null && Vector3.Distance(lockedCharacter.transform.position, this.gameObject.transform.position) > moveToTargetDistance)
        {
            attackStateCoroutine = StartCoroutine(KnifeWindup());
        }
        else
        {
            attackStateCoroutine = StartCoroutine(HandleStab());
        }
        
    }

    /// <summary>
    /// Starts the windup for the knife
    /// </summary>
    /// <returns> Time </returns>
    public IEnumerator KnifeWindup()
    {
        inCounter = false;
        attackState = AttackState.Windup;
        float timeStarted = Time.time;
        // For now wait 0.25 seconds, in future wait for animation trigger
        // Strider 9/30/25: moved this to a variable, need to adjust
        while (Time.time - timeStarted < windupTime)
        {
            if (lockedCharacter)
            {
                Vector3 direc = lockedCharacter.transform.position - transform.position;
                direc.y = 0;
                Quaternion rotationVal = Quaternion.LookRotation(direc.normalized);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, rotationVal, rotationalVelocity);
            }
            yield return null;
        }
        attackStateCoroutine = StartCoroutine(KnifeApproach());
    }

    /// <summary>
    /// Approach function for stabbing
    /// </summary>
    /// <returns> Time </returns>
    public IEnumerator KnifeApproach()
    {
        attackState = AttackState.Approaching;

        if (lockedCharacter)
        {
            targetPos = lockedCharacter.transform.position - (lockedCharacter.transform.position - transform.position).normalized * 1.5f;
            targetPos.y = transform.position.y;
            GetCharacterController().enabled = false;
            transform.DOMove(targetPos, chaseTime);
            transform.DOLookAt(targetPos, chaseTime);

            if (lockedCharacter != null)
            {
                CameraController.instance.GetCombatCamScript().TargetSet(lockedCharacter.gameObject);
            }

            float timeStarted = Time.time;
            while (Time.time - timeStarted < chaseTime)
            {
                if (Time.time - timeStarted >= 3 * chaseTime / 4) // Fourth quarter, not dodgable
                {
                    //   dodgable = false;
                    if (attackIndicator != null)
                    {
                        attackIndicator.GetComponent<MeshRenderer>().material = defaultMaterial;
                        if(PlayerController.instance.GetCounterAvailable() == this) PlayerController.instance.SetCounterAvaliable(null);
                    }
                }
                else // First 3 quarters, attack is dodgable
                {
                    //    dodgable = true;
                    if (attackIndicator != null)
                    {
                        attackIndicator.GetComponent<MeshRenderer>().material = perfectCounterTimeMaterial;
                        PlayerController.instance.SetCounterAvaliable(this);
                    }
                }
                SetMovementValues(false);
                GetCharacterController().enabled = false;
                yield return null;
            }
            transform.position = targetPos;
            GetCharacterController().enabled = true;
        }
        targetPos = Vector3.negativeInfinity;
        DestoryAttackIndicator();

        attackStateCoroutine = StartCoroutine(HandleStab());
        yield break;
    }

    /// <summary>
    /// Coroutine handling the AI state changes, AI delay, and locking movement for the player when stabbing
    /// </summary>
    /// <returns> Time breaks </returns>
    public IEnumerator HandleStab()
    {
        animator.SetPrimaryMovementNeeded(false);
        attackState = AttackState.Attacking;

        Vector3 offsetPosition = transform.position + transform.forward * offSetForward;
        GameObject knifeHitbox = Instantiate(knifePrefab, offsetPosition, transform.rotation);
        if (!playerControlling) { currentPrimaryComboStep = 0; }
        knifeHitbox.GetComponent<DefaultHitbox>().Init(this, dmg: knifeDamage[currentPrimaryComboStep == -1 ? 0 : currentPrimaryComboStep], forwardVelocity: thrustSpeed[currentPrimaryComboStep == -1 ? 0 : currentPrimaryComboStep], status: knifeEffects[currentPrimaryComboStep == -1 ? 0 : currentPrimaryComboStep], attackDuration: knifeDuration);

        float hitboxStartTime = Time.time;
        while (Time.time - hitboxStartTime < 0.25f)
        {
            SetMovementValues(false);
            yield return null;
        }

        if (!hitCharacter) // If missed, vulnerable for half a second
        {
            yield return new WaitForSeconds(0.5f);
        }

        SetMovementValues(true);

        attackState = AttackState.Neutral;
        pathState = PathState.Unset;

        if (lockedCharacter)
        {
            lockedCharacter.SetAttacker(null);
            if (lockedCharacter.TryGetComponent(out Enemy enemy))
            {
                enemy.SetTargeted(false);
            }
        }

        lockedCharacter = null;
        attackingPrimary = false;
    }

    /// <summary>
    /// Starts the secondary attack
    /// </summary>
    public override void SecondaryAttack()
    {
        hitCharacter = false;
        if (playerControlling)
        {
            PlayerController.instance.SetAllowMovement(false);
            lockedCharacter = PlayerController.instance.GetLockedTarget();
        }
        else
        {
            lockedCharacter = currentPlayer;
            aiState = AIMovementState.Blocked;
            attackIndicator = Instantiate(attackIndicatorPrefab, transform);
            attackIndicator.transform.localPosition = new Vector3(0, 2.5f, 0);
        }

        if (lockedCharacter)
        {
            lockedCharacter.SetAttacker(this);
            if (lockedCharacter.TryGetComponent(out Enemy enemy))
            {
                enemy.SetTargeted(true);
            }
        }

        attackStateCoroutine = StartCoroutine(SpinWindup());
    }

    /// <summary>
    /// Handles the windup for the spin
    /// </summary>
    /// <returns> Time </returns>
    public IEnumerator SpinWindup()
    {
        inCounter = false;
        attackingSecondary = true;
        attackState = AttackState.Windup;

        float timeStarted = Time.time;

        if (playerControlling) lockedCharacter = PlayerController.instance.GetLockedTarget();
        else
        {
            attackIndicator = Instantiate(attackIndicatorPrefab, transform);
            attackIndicator.transform.localPosition = new Vector3(0, 2.5f, 0);
            lockedCharacter = currentPlayer;
        }

        if (lockedCharacter)
        {
            lockedCharacter.SetAttacker(this);
            if (lockedCharacter.TryGetComponent(out Enemy enemy))
            {
                enemy.SetTargeted(true);
            }
        }

        // For now wait 0.5 seconds, in future wait for animation trigger
        while (Time.time - timeStarted < 0.125f)
        {
            if (lockedCharacter)
            {
                Vector3 direc = lockedCharacter.transform.position - transform.position;
                direc.y = 0;
                Quaternion rotationVal = Quaternion.LookRotation(direc.normalized);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, rotationVal, rotationalVelocity);
            }

            if (playerControlling && attackIndicator != null) Destroy(attackIndicator); // Destroy attack indicator if possessed
            yield return null;
        }
        numDeflections = 0;
        attackStateCoroutine = StartCoroutine(HandleSpin(spinDistance, spinRotationalSpeed));
    }

    /// <summary>
    /// Handles the spinning attack itself
    /// </summary>
    /// <param name="distance"> Distance this spin can travel </param>
    /// <param name="desiredRotation"> Rotation to reach for goblin spin </param>
    /// <param name="newDirection"> Velocity to move at, zero by default if unset </param>
    /// <returns> Time </returns>
    public IEnumerator HandleSpin(float distance, float desiredRotation, Vector3 direction = default)
    {
        attackState = AttackState.Attacking;
        if (distance < 0.5f)
        {
            velocity = Vector3.zero; // Clamping velocity
            rotationalVelocity = 0;

            attackingSecondary = false;
            timeLastSecondary = Time.time;

            SetMovementValues(true);

            attackStateCoroutine = null;
            yield break;
        }

        SetMovementValues(false);

        float rotationalSpeed = 0;
        Vector3 desiredVelocity;

        Debug.Log(direction);
        bool slowTime = false;

        GameObject hitbox = Instantiate(spinHitbox, transform);
        hitbox.GetComponent<DeflectingHitbox>().Init(this, dmg: spinDamage, status: spinEffects, attackDuration: spinDuration);

        if (direction == Vector3.zero) // If first use, set desiredVelocity alone and have AI pause
        {
            if (!playerControlling) yield return new WaitForSeconds(attackDelayAI);

            if (lockedCharacter)
            {
                desiredVelocity = (lockedCharacter.transform.position - transform.position).normalized;
            }
            else
            {
                desiredVelocity = transform.forward.normalized;
            }
        }
        else // If deflecting slow time for a period to give reaction time
        {
            slowTime = true;
            desiredVelocity = direction;
        }

        desiredVelocity.y = 0;
        desiredVelocity = desiredVelocity.normalized * spinSpeed;

        Vector3 drift;

        float accelerationTime;
        // Handle low health AI behavior here in the future, (apply random rotational offset depending on health)
        if (!IsLowHealth() || playerControlling)
        {
            accelerationTime = standardAccelerationPeriod;
            drift = Vector3.zero;
        }
        else
        {
            accelerationTime = lowHealthAccelerationPeriod;
            drift = Quaternion.AngleAxis(Random.Range(-lowHealthAngleRange / 2, lowHealthAngleRange / 2), Vector3.up) * velocityToMove.normalized * maxDriftSpeed;
        }

        float timeStarted;
        if (slowTime)
        {
            timeStarted = Time.time;
        }
        else
        {
            timeStarted = 0;
        }

        float timeSinceBegan = 0;

        float distanceTravelled = 0;
        while (distanceTravelled < distance)
        {
            if (slowTime && Time.time - timeStarted < 0.05f)
            {
                Time.timeScale = 0.5f;
                yield return null;
            }
            else if (Time.timeScale == 0.5f)
            {
                Time.timeScale = 1;
            }

            timeSinceBegan += Time.deltaTime;
            SetMovementValues(false); // Helps if player possesses enemy mid-attack

            if (velocity.magnitude < spinSpeed) // If still accelerating
            {
                velocity = Vector3.Lerp(velocity, desiredVelocity, timeSinceBegan / accelerationTime);
                rotationalVelocity = Mathf.Lerp(rotationalVelocity, desiredRotation, timeSinceBegan / accelerationTime);
            }

            float currentMagnitude = velocity.magnitude; // Set magnitude before adding drift

            velocity = velocity += drift * Time.deltaTime; // Add drift to velocity

            velocity = velocity.normalized * currentMagnitude; // Set to same magnitude as before, drift does not allow for faster speeds, only misdirection

            if (Mathf.Abs(rotationalSpeed) > Mathf.Abs(desiredRotation)) // Correct rotational speed
            {
                rotationalVelocity = spinRotationalSpeed;
            }

            GetCharacterController().Move(velocity * Time.deltaTime);
            transform.Rotate(Vector3.up, rotationalVelocity * Time.deltaTime);

            distanceTravelled += velocity.magnitude * Time.deltaTime;

            yield return null;
        }

        // If reached this point (no deflects) slow down, destroy hitbox halfway through, and end
        float timeSinceSlowBegan = 0;
        Destroy(hitbox);

        // end spin portion of the secondary attack animation, move into stagger portion
        animator.SetSecondaryAttackEnded();

        while (timeSinceSlowBegan < 0.5f)
        {
            velocity = Vector3.Lerp(velocity, Vector3.zero, timeSinceSlowBegan / 0.5f);
            rotationalVelocity = Mathf.Lerp(rotationalVelocity, 0, timeSinceSlowBegan / 0.5f);

            GetCharacterController().Move(velocity * Time.deltaTime);
            transform.Rotate(Vector3.up, rotationalVelocity * Time.deltaTime);

            timeSinceSlowBegan += Time.deltaTime;

            yield return null;
        }

        if (lockedCharacter)
        {
            lockedCharacter.SetAttacker(null);
            if (lockedCharacter.TryGetComponent(out Enemy enemy))
            {
                enemy.SetTargeted(false);
            }
            transform.Rotate(Vector3.up, rotationalSpeed * Time.deltaTime);

            yield return null;
        }

        if (lockedCharacter)
        {
            lockedCharacter.SetAttacker(null);
            if (lockedCharacter.TryGetComponent(out Enemy enemy))
            {
                enemy.SetTargeted(false);
            }
        }

        velocity = Vector3.zero; // Clamping velocity
        rotationalVelocity = 0;

        attackingSecondary = false;
        timeLastSecondary = Time.time;

        SetMovementValues(true);
        attackStateCoroutine = null;
    }


    /// <summary>
    /// Function that follows the flow chart to set behavior for the goblin
    /// </summary>
    public override void SetBehavior()
    {
        target = playerController.currentCharacter; // Always update this
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
            surroundPoint = currentPlayer.GetSurroundingPoints().AssignPoint(this);
            if (surroundPoint)
            {
                pathState = PathState.Searching;
                StartCoroutine(GraphBuilder.instance.AStarSearch(this, surroundPoint.transform.position));
            }
        }
        else if (aiState == AIMovementState.Surrounding) // Handles the same as chasing, just in closer range
        {
            surroundPoint = currentPlayer.GetSurroundingPoints().AssignPoint(this);
            if (surroundPoint)
            {
                pathState = PathState.Searching;
                StartCoroutine(GraphBuilder.instance.AStarSearch(this, surroundPoint.transform.position));
            }
        }
        else if (aiState == AIMovementState.Retreating) // Handles the same as chasing, just in closer range
        {
            surroundPoint = currentPlayer.GetSurroundingPoints().AssignPoint(this);
            if (surroundPoint)
            {
                pathState = PathState.Searching;
                StartCoroutine(GraphBuilder.instance.AStarSearch(this, surroundPoint.transform.position));
            }
        }
    }

    /// <summary>
    /// Override function handling patrol functionality for the Goblin
    /// This patrol method sets a point before the first frame and the goblin will patrol
    /// randomly within a circle of that point
    /// </summary>
    public override void Patrol()
    {
        if (!idleAudio.isValid()) AudioManager.TryPlayInstance("GoblinIdle", out idleAudio, true, gameObject);

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
                UpdatePath(false);
            }
        }
        else // If no current path, mark as available
        {
            reachedWalkpoint = false;
        }
    }

    /// <summary>
    /// Called in first frame, sets the patrol origin to Goblin position
    /// </summary>
    public void SetPatrolOrigin()
    {
        patrolOrigin = transform.position;
    }

    /// <summary>
    /// Override function for setting a patrol point
    /// This version uses a point of origin separate from the Goblin to place points
    /// </summary>
    public void SetPatrollingPoint()
    {
        float randomX = Random.Range(-patrolRange, patrolRange);
        float randomZ = Random.Range(-patrolRange, patrolRange);

        walkPoint = new Vector3(patrolOrigin.x + randomX, patrolOrigin.y, patrolOrigin.z + randomZ);
        walkPoint = GraphBuilder.instance.FindClosestNode(walkPoint).GetPosition(gameObject);

        StartCoroutine(GraphBuilder.instance.AStarSearch(this, walkPoint));
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

        if (aiState == AIMovementState.Patrolling) // If a valid patrol point
        {
            float distance = currentPath.GetDistance();

            if ((distance >= minPatrolDistance && distance <= maxPatrolDistance) || Vector3.Distance(transform.position, patrolOrigin) >= patrolRange)
            {
                if (debugging)
                {
                    StartPath(false);
                }

                reachedWalkpoint = false;
                pathState = PathState.Set;
                return true;
            }
            pathState = PathState.Unset;
            return false;
        }
        pathState = PathState.Set;
        return true;
    }

    /// <summary>
    /// Coroutine to handle the goblin when it reaches it's patrol destination
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

        while (timer < 1.5f) // Wait 1.5 seconds for now, will change this to be a bool checking the end of looking animation
        {
            if (LookForPlayer())
            {
                StartCoroutine(SpotPlayer());
                yield break;
            }
            timer += Time.deltaTime;
            yield return null;
        }

        inProcess = false;
        if (debugging)
        {
            StartPath(false);
        }
    }

    /// <summary>
    /// Coroutine that plays when the player is spotted
    /// </summary>
    /// <param name="fromGoblin"> Whether the goblin was told where the player is </param>
    /// <returns> Waits for animation to be done </returns>
    private IEnumerator SpotPlayer(bool fromGoblin = false)
    {
        aiState = AIMovementState.Chasing;
        if (debugging)
        {
            DestroyPath();
        }

        inProcess = true;

        timePlayerLastSeen = Time.time;

        // Play animation/noise that the player has been seen
        if (!fromGoblin)
        {
            yield return new WaitForSeconds(0.25f);
        }

        // Alert nearby Goblins of player

        inProcess = false;
    }

    /// <summary>
    /// Chase function for the Goblin - should set paths that focus on surrounding the player
    /// </summary>
    public override void Chase()
    {
        StopIdleAudio();
        lookAtPlayer = false;

        if (pathState == PathState.Set || (pathState == PathState.Searching && currentPath != null))
        {
            AIMove();
            if (debugging)
            {
                UpdatePath(false);
            }
        }
        AILook();

        if (currentPath != null)
        {
            if (Vector3.Distance(transform.position, currentPath.GetDestinationPosition(gameObject)) <= chaseToSurroundingRadius) // If within range
            {
                aiState = AIMovementState.Surrounding;
                if (currentPlayer.TryGetComponent(out SurroundingPoints points))
                {
                    points.AddSurroundingEnemy(this);
                }
            }
        }
    }

    /// <summary>
    /// Function handling tasks when surrounding
    /// </summary>
    public void Surround()
    {
        StopIdleAudio();
        lookAtPlayer = true;

        if (pathState == PathState.Set || (pathState == PathState.Searching && currentPath != null))
        {
            if (Vector3.Distance(transform.position, currentPlayer.transform.position) > chaseToSurroundingRadius)
            {
                AIMove();
                if (debugging)
                {
                    UpdatePath(false);
                }
            }
        }
        AILook();

        if (currentPath != null)
        {
            if (Vector3.Distance(transform.position, currentPath.GetDestinationPosition(gameObject)) > surroundingToChaseRadius) // If within a meter and a half of surrounding radius
            {
                aiState = AIMovementState.Chasing;
                if (currentPlayer.TryGetComponent(out SurroundingPoints points))
                {
                    points.RemoveSurroundingEnemy(this);
                }
            }
        }
    }

    /// <summary>
    /// Retreat from close distance, get back to surrounding
    /// </summary>
    public void Retreat()
    {
        lookAtPlayer = true;
        if (pathState == PathState.Set || (pathState == PathState.Searching && currentPath != null))
        {
            Debug.Log("Moving: " + gameObject);
            AIMove();
            if (debugging)
            {
                UpdatePath(false);
            }
        }
        AILook();

        if (currentPath != null)
        {
            if (Vector3.Distance(transform.position, currentPath.GetDestinationPosition(gameObject)) <= chaseToSurroundingRadius) // If within range
            {
                aiState = AIMovementState.Surrounding;
                if (currentPlayer.TryGetComponent(out SurroundingPoints points))
                {
                    points.AddSurroundingEnemy(this);
                }
            }
        }
    }


    /// <summary>
    /// Function that tells Goblins to communicate the player's location with each other
    /// Called when a chasing Goblin cannot directly see the player
    /// If another Goblin in range can see the player, they tell the caller where the player is
    /// </summary>
    /// <returns> True if another goblin in range can see the player </returns>
    public bool RequestLocation()
    {
        Collider[] charColliders = Physics.OverlapSphere(transform.position, communicationRange, characters);

        foreach (Collider hit in charColliders)
        {
            if (hit.TryGetComponent(out Goblin otherGoblin))
            {
                if (otherGoblin.LookForPlayer())
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the priority of a goblin to be added for attacking
    /// </summary>
    /// <returns> Enemy priority </returns>
    public override int GetAttackingPriority()
    {
        int val = base.GetAttackingPriority();
        if (IsLowHealth()) val += 2;

        return val;
    }

    /// <summary>
    /// Deflects the velocity depending on the playercontrolling status, target direction, and collision type
    /// </summary>
    /// <param name="other"> Other collider in collision </param>
    public void DeflectVelocity(Collider other, DeflectingHitbox caller)
    {
        numDeflections++;
        Vector3 deflectDirection;

        Vector3 closestPoint = other.ClosestPoint(transform.position);
        Vector3 contactDirection = (transform.position - closestPoint).normalized;

        deflectDirection = Vector3.Reflect(velocity.normalized, contactDirection);

        int rotationMultiplier = 1;
        if (numDeflections % 2 != 0)
        {
            rotationMultiplier = -1;
        }
        if (attackStateCoroutine != null) // If coroutine has ended, end this
        {
            StopCoroutine(attackStateCoroutine);
            attackStateCoroutine = StartCoroutine(HandleSpin(spinDistance - spinDistanceDropoff * numDeflections, spinRotationalSpeed * rotationMultiplier, deflectDirection));
            rotationalVelocity = -rotationalVelocity / 2; // Reverse rotational speed and halve it
            Destroy(caller.gameObject);
        }
        velocity = Vector3.zero; // Zero out velocity to instantly change directions
        Destroy(caller.gameObject);
    }

    /// <summary>
    /// Handles Goblin attacking chance and triggering
    /// </summary>
    /// <param name="points"> The points calling this function </param>
    /// <returns> True if attacking, false otherwise </returns>
    public override bool AttackFromSurrounding(SurroundingPoints points)
    {
        float totalOdds = 0;
        List<Goblin> goblins = points.GetEnemiesSameType(this);

        if (CheckPrimaryUsable())
        {
            totalOdds += primaryAttackChance;
        }
        if (CheckSecondaryUsable())
        {
            if (goblins.Count >= 1) // Only do this if other goblins are around
            {
                totalOdds += secondaryAttackChance;
            }
        }

        if (totalOdds > 0)
        {
            float choice = Random.Range(0, totalOdds);
            if (choice <= primaryAttackChance) // Primary attack selected
            {
                StartCoroutine( BeginPrimary());
            }
            else
            {
                StartCoroutine( BeginSecondary());
                // Coordinate other goblin attack here
            }
            points.RemoveSurroundingEnemy(this);
            return true;
        }
        else
        {
            return false;
        }
    }

    public override void Die()
    {
        //Stopping any playing sound effects on death.
        if (idleAudio.isValid())
        {
            idleAudio.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        }
        //Play Goblin's Death sound effect
        if (AudioManager.TryPlayInstance("GoblinDeath", out EventInstance ev, true, gameObject))
        {
            ev.setParameterByNameWithLabel("Possessed", playerControlling ? "True" : "False");
        }
        base.Die();
    }

    /// <summary>
    /// Set possessed to be true/false
    /// </summary>
    /// <param name="val"> Value to set </param>
    public override void SetControlled(bool val)
    {
        base.SetControlled(val);
    }

    /// <summary>
    /// Stops the idle sound effects of the goblin if it's currently playing
    /// </summary>
    void StopIdleAudio()
    {
        if (idleAudio.isValid())
        {
            idleAudio.setParameterByNameWithLabel("End", "True");
            idleAudio = new();
        }
    }

    //Override to implement Goblin's hit sound effect
    protected override void OnDamaged(float amount)
    {
        base.OnDamaged(amount);
        if (AudioManager.TryGetReference("GoblinHit", out EventReference eventRef))
        {
            EventInstance ev = RuntimeManager.CreateInstance(eventRef);
            RuntimeManager.AttachInstanceToGameObject(ev, gameObject);
            ev.setParameterByName("Damage", amount / health.GetMaxHealth());
            ev.setParameterByNameWithLabel("Possessed", playerControlling ? "True" : "False");
            ev.start();
            ev.release();
        }
    }
}