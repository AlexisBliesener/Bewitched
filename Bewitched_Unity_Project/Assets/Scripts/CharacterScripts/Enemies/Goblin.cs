using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Goblin : Enemy
{

    [Header("Goblin Settings")]
    [Tooltip("Knife Prefab")]
    [SerializeField] GameObject knifePrefab;
    [Tooltip("Thrust Speed")]
    [SerializeField] float thrustSpeed = 10;
    [Tooltip("Knife Damage")]
    [SerializeField] float knifeDamage = 20;
    [Tooltip("Knife duration")]
    [SerializeField] float knifeLungeRange = 2;
    [Tooltip("Knife Lunge Speed")]
    [SerializeField] float knifeStabSpeed = 10;
    [Tooltip("Knife Range")]
    [SerializeField] float knifeRange = 3;
    [Tooltip("Locking radius for the knife lunge")]
    [SerializeField] float knifeLungeRadius = 2;
    [Tooltip("Knife Effects")]
    [SerializeField] AttackStatusEffects knifeEffects;
    [Tooltip("Dash Hitbox")]
    [SerializeField] GameObject dashHitbox;
    [Tooltip("Dash Speed")]
    [SerializeField] float dashSpeed = 50;
    [Tooltip("Dash Duration")]
    [SerializeField] float dashDuration = 0.5f;
    [Tooltip("Dash Damage")]
    [SerializeField] float dashDamage = 30;
    [Tooltip("Dash Effects")]
    [SerializeField] AttackStatusEffects dashEffects;
    [Tooltip("Offset of the hitbox forward")]
    [SerializeField] private float offSetForward = 0.5f;

    [Tooltip("Spin Hitbox")]
    [SerializeField] GameObject spinHitbox;
    [Tooltip("Spin Damage")]
    [SerializeField] float spinDamage = 30;
    [Tooltip("Spin Duration")]
    [SerializeField] float spinDuration = 5;
    [Tooltip("Spin Speed")]
    [SerializeField] float spinSpeed = 15;
    [Tooltip("Spin Rotational Speed")]
    [SerializeField] float spinRotationalSpeed = 120;
    [Tooltip("Standard Acceleration Period")]
    [SerializeField] float standardAccelerationPeriod = 0.5f;
    [Tooltip("Low Health Acceleration Period")]
    [SerializeField] float lowHealthAccelerationPeriod = 0.25f;
    [Tooltip("Low Health Angle Variation Range")]
    [SerializeField] float lowHealthAngleRange = 80; // maximum 40 degree change
    [Tooltip("Maximum drift speed")]
    [SerializeField] float maxDriftSpeed = 4;
    [Tooltip("Spin Effects")]
    [SerializeField] AttackStatusEffects spinEffects;

    [Header("Goblin AI Settings")]
    [Tooltip("Minimum Patrol Distance")]
    [SerializeField] float minPatrolDistance = 3;
    [Tooltip("Maximum Patrol Distance")]
    [SerializeField] float maxPatrolDistance = 5;
    [Tooltip("Range the Goblin can communicate with other Goblins")]
    [SerializeField] float communicationRange = 8;

    [Tooltip("Bool Determining if we are in a process that blocks AI (like looking around, attacking, etc")]
    private bool inProcess = false;

    [Tooltip("The Goblin's Patrol Point Origin")]
    private Vector3 patrolOrigin;

    [Tooltip("Previous spinning velocity (used for determining if we have deflected when speeding up")]
    private Vector3 prevSpinVelocity = Vector3.zero;

    private void Start()
    {
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

    private void Update()
    {
        currentPlayer = playerController.GetCurrentCharacter();
        if (!playerControlling)
        {
            SetBehavior();
        }
        HandleHitStun();
    }

    public override void PrimaryAttack()
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
        }

        if (lockedCharacter)
        {
            if (lockedCharacter.TryGetComponent(out Enemy enemy))
            {
                enemy.SetTargeted(true);
            }
        }

        attackStateCoroutine = StartCoroutine(KnifeWindup());
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
            float timeStarted = Time.time;
            while (Vector3.Distance(lockedCharacter.transform.position, transform.position) > knifeRange)
            {
                if (Time.time - timeStarted > chaseTime)
                {
                    if (!playerControlling)
                    {
                        aiState = AIMovementState.Retreating;
                        pathState = PathState.Unset;
                    }
                    else PlayerController.instance.SetAllowMovement(true);

                    if (lockedCharacter)
                    {
                        if (lockedCharacter.TryGetComponent(out Enemy enemy))
                        {
                            enemy.SetTargeted(false);
                        }
                        if (lockedCharacter == currentPlayer) PlayerController.instance.SetAllowMovement(true);
                    }

                    lockedCharacter = null;
                    attackingPrimary = false;
                    timeLastPrimary = Time.time;

                    break;
                }

                Vector3 desiredVelocity = (lockedCharacter.transform.position - transform.position).normalized;
                desiredVelocity.y = 0;
                desiredVelocity = desiredVelocity.normalized * approachSpeed;
                Quaternion rotationVal = Quaternion.LookRotation(desiredVelocity);
                float xChange = GetAccelerationValue(velocity.x, desiredVelocity.x) * Time.deltaTime;
                velocity.x += xChange;

                if (Mathf.Abs(velocity.x) >= approachSpeed) velocity.x = approachSpeed * Mathf.Sign(velocity.x); // If above max x velocity (movement speed straight in x direction)

                float zChange = GetAccelerationValue(velocity.z, desiredVelocity.z) * Time.deltaTime;
                velocity.z += zChange;

                if (Mathf.Abs(velocity.z) >= approachSpeed) velocity.z = approachSpeed * Mathf.Sign(velocity.z);


                if (velocity.magnitude > approachSpeed)
                {
                    velocity = velocity.normalized * approachSpeed;
                }

                if (velocity.magnitude < 0.01f)
                {
                    velocity = Vector3.zero;
                }

                GetComponent<CharacterController>().Move(velocity * Time.deltaTime);
                GetComponent<CharacterController>().Move(Vector3.down);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, rotationVal, rotationalVelocity);
                yield return null;
            }
        }

        float timeStartSlow = Time.time;
        if (!playerControlling) // If AI, slow down and allow for well-timed attack to interrupt
        {
            inPerfectStopZone = true;
            while (Time.time - timeStartSlow < 0.5f)
            {
                Vector3 direc = lockedCharacter.transform.position - transform.position;
                direc.y = 0;
                Quaternion rotationVal = Quaternion.LookRotation(direc.normalized);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, rotationVal, rotationalVelocity);

                if (velocity.magnitude < 0.1f)
                {
                    velocity = Vector3.zero;
                }
                else
                {
                    GetComponent<CharacterController>().Move(velocity * Time.deltaTime);
                }

                yield return null;
            }
            inPerfectStopZone = false;
        }

        attackStateCoroutine = StartCoroutine(HandleStab());
    }

    /// <summary>
    /// Starts the windup for the knife
    /// </summary>
    /// <returns> Time </returns>
    public IEnumerator KnifeWindup()
    {
        attackState = AttackState.Windup;
        float timeStarted = Time.time;
        // For now wait 0.25 seconds, in future wait for animation trigger
        while (Time.time - timeStarted < 0.25f)
        {
            if (lockedCharacter)
            {
                Vector3 direc = lockedCharacter.transform.position - transform.position;
                direc.y = 0;
                Quaternion rotationVal = Quaternion.LookRotation(direc.normalized);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, rotationVal, rotationalVelocity);
                yield return null;
            }
        }
        attackStateCoroutine = StartCoroutine(KnifeApproach());
    }

    /// <summary>
    /// Coroutine handling the AI state changes, AI delay, and locking movement for the player when stabbing
    /// </summary>
    /// <returns> Time breaks </returns>
    public IEnumerator HandleStab()
    {
        attackState = AttackState.Attacking;
        // Check for target in spherecast, if target is there, lock target and do stab, otherwise quickly miss stab and end
        Collider[] colliders = Physics.OverlapSphere(transform.position + transform.forward.normalized * knifeLungeRange / 3, knifeLungeRange, characters);
        if (colliders.Length > 0)
        {
            float minDist = Mathf.Infinity;
            Character closest = null;
            foreach (Collider collider in colliders)
            {
                if (collider.TryGetComponent(out Character hitChar))
                {
                    if (minDist > (hitChar.transform.position - transform.position).magnitude && hitChar != this)
                    {
                        closest = hitChar;
                        minDist = (hitChar.transform.position - transform.position).magnitude;
                    }
                }
            }
            lockedCharacter = closest;
            if (lockedCharacter == currentPlayer) PlayerController.instance.SetAllowMovement(false);
        }

        Vector3 offsetPosition = transform.position + transform.forward * offSetForward;
        GameObject knifeHitbox = Instantiate(knifePrefab, offsetPosition, transform.rotation);
        knifeHitbox.GetComponent<DefaultHitbox>().Init(this, dmg: knifeDamage, forwardVelocity: thrustSpeed, status: knifeEffects, attackDuration: 10);

        float distanceTravelled = 0;

        while (distanceTravelled < knifeLungeRange) // Accelerate forward towards the player
        {
            if (hitCharacter) break;
            if (playerControlling) PlayerController.instance.SetAllowMovement(false); // Helps if player possesses enemy mid-attack
            Vector3 desiredVelocity = transform.forward.normalized;
            desiredVelocity.y = 0;
            desiredVelocity = desiredVelocity.normalized * knifeStabSpeed;
            float xChange = GetAccelerationValue(velocity.x, desiredVelocity.x) * Time.deltaTime;
            velocity.x += xChange;

            if (Mathf.Abs(velocity.x) >= knifeStabSpeed) velocity.x = knifeStabSpeed * Mathf.Sign(velocity.x); // If above max x velocity (movement speed straight in x direction)

            float zChange = GetAccelerationValue(velocity.z, desiredVelocity.z) * Time.deltaTime;
            velocity.z += zChange;

            if (Mathf.Abs(velocity.z) >= knifeStabSpeed) velocity.z = knifeStabSpeed * Mathf.Sign(velocity.z);


            if (velocity.magnitude > knifeStabSpeed)
            {
                velocity = velocity.normalized * knifeStabSpeed;
            }

            if (velocity.magnitude < 0.01f)
            {
                velocity = Vector3.zero;
            }

            GetComponent<CharacterController>().Move(velocity * Time.deltaTime);
            GetComponent<CharacterController>().Move(Vector3.down);

            distanceTravelled += velocity.magnitude * Time.deltaTime;
            yield return null;
        }

        Destroy(knifeHitbox.gameObject);

        if (!hitCharacter) // If missed, vulnerable while decelerating to zero
        {
            while (velocity.magnitude > 0) // Decelerate to zero
            {
                if (playerControlling) PlayerController.instance.SetAllowMovement(false); // Helps if player possesses enemy mid-attack
                velocity -= velocity.normalized * deceleration * Time.deltaTime;
                if (velocity.magnitude < 0.1f)
                {
                    velocity = Vector3.zero;
                }
                GetComponent<CharacterController>().Move(velocity * Time.deltaTime);
                yield return null;
            }
        }
        else
        {
            velocity = Vector3.zero;
        }

        if (!playerControlling)
        {
            aiState = AIMovementState.Retreating;
            pathState = PathState.Unset;
        }
        else PlayerController.instance.SetAllowMovement(true);

        if (lockedCharacter)
        {
            if (lockedCharacter.TryGetComponent(out Enemy enemy))
            {
                enemy.SetTargeted(false);
            }
            if (lockedCharacter == currentPlayer) PlayerController.instance.SetAllowMovement(true);
        }

        lockedCharacter = null;
        attackingPrimary = false;
        timeLastPrimary = Time.time;
    }

    public override void SecondaryAttack()
    {
        attackingSecondary = true;
        timeLastSecondary = Time.time;

        GameObject hitbox = Instantiate(spinHitbox, transform);
        hitbox.GetComponent<DefaultHitbox>().Init(this, dmg: spinDamage, attackDuration: spinDuration, status: spinEffects);

        StartCoroutine(HandleSpin());
    }

    /// <summary>
    /// Handles the spinning attack itself
    /// </summary>
    /// <returns> Time </returns>
    public IEnumerator HandleSpin()
    {
        if (playerControlling) PlayerController.instance.SetAllowMovement(false);
        else yield return new WaitForSeconds(attackDelayAI);

        float timeStarted = Time.time;
        float rotationalSpeed = 0;
        velocityToMove = transform.forward;
        velocityToMove.y = 0;
        velocityToMove = velocityToMove.normalized * spinSpeed;

        Vector3 prevTargetVelocity = velocityToMove;

        Vector3 drift;
        bool lowHealthActions;

        float accelerationTime;
        // Handle low health AI behavior here in the future, (apply random rotational offset depending on health)
        if (!IsLowHealth() || playerControlling)
        {
            accelerationTime = standardAccelerationPeriod;
            drift = Vector3.zero;
            lowHealthActions = false;
        }
        else
        {
            accelerationTime = lowHealthAccelerationPeriod;
            drift = Quaternion.AngleAxis(Random.Range(-lowHealthAngleRange / 2, lowHealthAngleRange / 2), Vector3.up) * velocityToMove.normalized * maxDriftSpeed;
            lowHealthActions = true;
        }

        while (Time.time - timeStarted < spinDuration)
        {
            if (playerControlling) PlayerController.instance.SetAllowMovement(false); // Helps if player possesses enemy mid-attack

            if (velocityToMove != prevTargetVelocity)
            {
                velocityToMove = velocity.normalized * spinSpeed; // If we deflected while speeding up then adjust target velocity
                if (lowHealthActions) drift = Quaternion.AngleAxis(Random.Range(-lowHealthAngleRange / 2, lowHealthAngleRange / 2), Vector3.up) * velocityToMove.normalized * maxDriftSpeed;
            }

            if (Time.time - timeStarted < accelerationTime)
            {
                velocity = Vector3.Lerp(velocity, velocityToMove + drift, Time.deltaTime / accelerationTime);

                rotationalSpeed = Mathf.Lerp(rotationalSpeed, spinRotationalSpeed, Time.deltaTime / accelerationTime);
            }
            else
            {
                velocity = velocityToMove;
            }

            GetComponent<CharacterController>().Move(velocity * Time.deltaTime);
            transform.Rotate(Vector3.up, rotationalSpeed * Time.deltaTime);

            prevTargetVelocity = velocityToMove;

            yield return null;
        }

        while (Time.time - timeStarted < spinDuration + 0.5f) // Spend the remaining half second slowing down
        {
            if (playerControlling) PlayerController.instance.SetAllowMovement(false); // Helps if player possesses enemy mid-attack
            velocity = Vector3.Lerp(velocity, Vector3.zero, Time.deltaTime / 0.5f);
            rotationalSpeed = Mathf.Lerp(rotationalSpeed, 0, Time.deltaTime / 0.5f);
            GetComponent<CharacterController>().Move(velocity * Time.deltaTime);
            transform.Rotate(Vector3.up, rotationalSpeed * Time.deltaTime);
            yield return null;
        }

        attackingSecondary = false;

        if (!playerControlling) aiState = AIMovementState.Chasing;
        else PlayerController.instance.SetAllowMovement(true);
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
            surroundPoint = currentPlayer.GetComponent<SurroundingPoints>().AssignPoint(this);
            if (surroundPoint)
            {
                pathState = PathState.Searching;
                StartCoroutine(GraphBuilder.instance.AStarSearch(this, surroundPoint.transform.position));
            }
        }
        else if (aiState == AIMovementState.Surrounding) // Handles the same as chasing, just in closer range
        {
            surroundPoint = currentPlayer.GetComponent<SurroundingPoints>().AssignPoint(this);
            if (surroundPoint)
            {
                pathState = PathState.Searching;
                StartCoroutine(GraphBuilder.instance.AStarSearch(this, surroundPoint.transform.position));
            }
        }
        else if (aiState == AIMovementState.Retreating) // Handles the same as chasing, just in closer range
        {
            surroundPoint = currentPlayer.GetComponent<SurroundingPoints>().AssignPoint(this);
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

    public override void DeflectVelocity(Vector3 direction)
    {
        base.DeflectVelocity(direction);
        velocityToMove = direction.normalized;
        velocityToMove.y = 0;
        velocityToMove = velocityToMove.normalized;
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
                PrimaryAttack();
            }
            else
            {
                SecondaryAttack();
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
}
