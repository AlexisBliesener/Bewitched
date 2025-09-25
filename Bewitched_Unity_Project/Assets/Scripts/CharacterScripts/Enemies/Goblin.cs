using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using DG.Tweening;

public class Goblin : Enemy
{

    [Header("Goblin Settings")]
    [Tooltip("Knife Prefab")]
    [SerializeField] GameObject knifePrefab;
    [Tooltip("Thrust Speed")]
    [SerializeField] float thrustSpeed = 10;
    [Tooltip("Knife Damage")]
    [SerializeField] float knifeDamage = 20;
    [Tooltip("Knife Lunge Speed")]
    [SerializeField] float knifeStabSpeed = 10;
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
    [Tooltip("Distance for first spin jump")]
    [SerializeField] float spinDistance = 8;
    [Tooltip("Distance dropoff per bounce")]
    [SerializeField] float spinDistanceDropoff = 2.5f;
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

    public Material dodgeTimeMaterial;
    public Material defaultMaterial;

    private bool deflect = false;

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

    private void FixedUpdate()
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

        attackStateCoroutine = StartCoroutine(KnifeWindup());
    }

    /// <summary>
    /// Approach function for stabbing
    /// </summary>
    /// <returns> Time </returns>
    public IEnumerator KnifeApproach()
    {
        Debug.Log("Approaching");
        attackDodged = false;
        dodgable = false;

        attackState = AttackState.Approaching;

        if (lockedCharacter)
        {
            Vector3 targetPos = lockedCharacter.transform.position - (lockedCharacter.transform.position - transform.position).normalized * 2;
            targetPos.y = transform.position.y;
            transform.DOMove(targetPos, chaseTime);

            float timeStarted = Time.time;
            while (Time.time - timeStarted < chaseTime)
            {
                if (Time.time - timeStarted >= 3 * chaseTime / 4) // Fourth quarter, not dodgable
                {
                    dodgable = false;
                    if (attackIndicator != null)
                    {
                        attackIndicator.GetComponent<MeshRenderer>().material = defaultMaterial;
                    }
                }
                else // First 3 quarters, attack is dodgable
                {
                    dodgable = true;
                    if (attackIndicator != null)
                    {
                        attackIndicator.GetComponent<MeshRenderer>().material = dodgeTimeMaterial;
                    }
                }

                if (!attackDodged) // Only stay locked if not dodged
                {
                    Vector3 direc = lockedCharacter.transform.position - transform.position;
                    direc.y = 0;
                    Quaternion rotationVal = Quaternion.LookRotation(direc.normalized);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, rotationVal, rotationalVelocity);
                }
                yield return null;
            }
        }
        if (attackIndicator != null)
        {
            Destroy(attackIndicator);
        }
        attackIndicator = null;

        Debug.Log("Not Approaching");

        attackStateCoroutine = StartCoroutine(HandleStab());
        yield break;
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

        Vector3 offsetPosition = transform.position + transform.forward * offSetForward;
        GameObject knifeHitbox = Instantiate(knifePrefab, offsetPosition, transform.rotation);
        knifeHitbox.GetComponent<DefaultHitbox>().Init(this, dmg: knifeDamage, forwardVelocity: thrustSpeed, status: knifeEffects, attackDuration: 0.25f);

        yield return new WaitForSeconds(0.25f);

        if (!hitCharacter) // If missed, vulnerable for half a second
        {
            yield return new WaitForSeconds(0.5f);
        }

        if (!playerControlling)
        {
            aiState = AIMovementState.Retreating;
            attackState = AttackState.Neutral;
            pathState = PathState.Unset;
        }
        else PlayerController.instance.SetAllowMovement(true);

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
        timeLastPrimary = Time.time;
    }

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

        attackingSecondary = true;
        timeLastSecondary = Time.time;

        StartCoroutine(SpinWindup());
    }

    public IEnumerator SpinWindup()
    {
        attackState = AttackState.Windup;
        float timeStarted = Time.time;
        // For now wait 0.5 seconds, in future wait for animation trigger
        while (Time.time - timeStarted < 0.5f)
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
        StartCoroutine(HandleSpin(spinDistance));
    }

    /// <summary>
    /// Handles the spinning attack itself
    /// </summary>
    /// <param name="distance"> Distance this spin can travel </param>
    /// <param name="fromCollision"> If being called from a collision </param>
    /// <returns> Time </returns>
    public IEnumerator HandleSpin(float distance, bool fromCollision = false)
    {
        if (playerControlling) PlayerController.instance.SetAllowMovement(false);
        else yield return new WaitForSeconds(attackDelayAI);

        deflect = false;

        float rotationalSpeed = 0;

        Vector3 desiredVelocity;

        if (lockedCharacter && (Vector3.Distance(lockedCharacter.transform.position, transform.position) <= distance))
        {
            desiredVelocity = (lockedCharacter.transform.position - transform.position).normalized;
        }
        else
        {
            desiredVelocity = transform.forward.normalized;
        }
        desiredVelocity.y = 0;
        desiredVelocity = desiredVelocity.normalized * spinSpeed;

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

        float distanceTravelled = 0;
        while (distanceTravelled < distance)
        {
            if (playerControlling) PlayerController.instance.SetAllowMovement(false); // Helps if player possesses enemy mid-attack

            if (deflect)
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
