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
    [SerializeField] float knifeDuration = 1;
    [Tooltip("Knife Lunge Speed")]
    [SerializeField] float knifeStabSpeed = 10;
    [Tooltip("Knife Range")]
    [SerializeField] float knifeRange = 3;
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

    [Tooltip("Needs new destination")]
    private bool needsDestination = true;

    private bool isDashing = false;

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
        Character target;
        if (playerControlling)
        {
            PlayerController.instance.SetAllowMovement(false);
            target = PlayerController.instance.TargetEnemy();
        }
        else
        {
            target = currentPlayer;
            aiState = AIMovementState.Blocked;
        }

        attackStateCoroutine = StartCoroutine(KnifeApproach(target));
    }

    public IEnumerator KnifeApproach(Character target)
    {
        attackState = AttackState.Approaching;
        while (Vector3.Distance(target.transform.position, transform.position) > knifeRange)
        {
            Vector3 desired = (target.transform.position - transform.position).normalized * movementSpeed;
            velocity = Vector3.Lerp(velocity, desired, acceleration * Time.deltaTime);
            yield return null;
        }

        attackStateCoroutine = StartCoroutine(KnifeWindup(target));
    }

    public IEnumerator KnifeWindup(Character target)
    {
        attackState = AttackState.Windup;
        // For now wait 0.25 seconds, in future wait for animation trigger
        yield return new WaitForSeconds(0.25f);
        attackStateCoroutine = StartCoroutine(HandleStab(target));
    }

    /// <summary>
    /// Coroutine handling the AI state changes, AI delay, and locking movement for the player when stabbing
    /// </summary>
    /// <returns> Time breaks </returns>
    public IEnumerator HandleStab(Character target)
    {
        if (!playerControlling) yield return new WaitForSeconds(attackDelayAI);
        

        timeLastPrimary = Time.time;
        attackingPrimary = true;

        Vector3 offsetPosition = transform.position + transform.forward * offSetForward;
        GameObject knifeHitbox = Instantiate(knifePrefab, offsetPosition, transform.rotation);
        knifeHitbox.GetComponent<DefaultHitbox>().Init(this, dmg: knifeDamage, forwardVelocity: thrustSpeed, status: knifeEffects, attackDuration: knifeDuration);

        Vector3 targetVelocity = transform.forward * knifeStabSpeed;
        targetVelocity.y = 0; // Ensure no flying goblins
        Vector3 stabVelocity = velocity;

        float timeStarted = Time.time;

        while (Time.time - timeStarted < (3 * knifeDuration / 4)) // Accelerate forward 3/4 the attack
        {
            if (playerControlling) PlayerController.instance.SetAllowMovement(false); // Helps if player possesses enemy mid-attack
            stabVelocity = Vector3.Lerp(stabVelocity, targetVelocity, Time.deltaTime / (3*knifeDuration/4));
            GetComponent<CharacterController>().Move(stabVelocity * Time.deltaTime);
            yield return null; 
        }

        while (stabVelocity.magnitude > 0) // Decelerate to zero within remaining duration time
        {
            if (playerControlling) PlayerController.instance.SetAllowMovement(false); // Helps if player possesses enemy mid-attack
            stabVelocity = Vector3.Lerp(stabVelocity, Vector3.zero, Time.deltaTime / (knifeDuration / 4));
            if (stabVelocity.magnitude < 0.05f)
            {
                stabVelocity = Vector3.zero;
            }
            GetComponent<CharacterController>().Move(stabVelocity * Time.deltaTime);
            yield return null;
        }

        if (!playerControlling) aiState = AIMovementState.Chasing;
        else PlayerController.instance.SetAllowMovement(true);

        attackingPrimary = false;
    }

    public override void SecondaryAttack()
    {
        attackingSecondary = true;
        timeLastSecondary = Time.time;

        GameObject hitbox = Instantiate(spinHitbox, transform);
        hitbox.GetComponent<DefaultHitbox>().Init(this, dmg: spinDamage, attackDuration: spinDuration, status: spinEffects);

        StartCoroutine(HandleSpin());
    }

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
            pathState = PathState.Searching;
            StartCoroutine(GraphBuilder.instance.AStarSearch(this, currentPlayer.transform.position + chasePoint));
        }
        else if (aiState == AIMovementState.Surrounding) // Handles the same as chasing, just in closer range
        {
            pathState = PathState.Searching;
            StartCoroutine(GraphBuilder.instance.AStarSearch(this, currentPlayer.transform.position + chasePoint));
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
            needsDestination = true;
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

                needsDestination = false;
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

        SetChasePoint();

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

        if (Vector3.Distance(transform.position, currentPlayer.transform.position) <= maxSurroundDistance) // If within range
        {
            aiState = AIMovementState.Surrounding;
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
            AIMove();
            if (debugging)
            {
                UpdatePath(false);
            }
        }
        AILook();

        if (Vector3.Distance(transform.position, currentPlayer.transform.position) > maxSurroundDistance) // If within a meter and a half of surrounding radius
        {
            aiState = AIMovementState.Chasing;
            SetChasePoint();
            if (currentPlayer.TryGetComponent(out SurroundingPoints points))
            {
                points.RemoveSurroundingEnemy(this);
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
}
