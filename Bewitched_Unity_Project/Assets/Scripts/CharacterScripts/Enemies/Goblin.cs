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

    private enum GoblinAIState
    {
        Patrolling,
        Chasing,
        Searching,
        Surrounding,
        AttackStab,
        AttackSpin
    }

    [Tooltip("The Current AI State of the Goblin")]
    private GoblinAIState aiState = GoblinAIState.Patrolling;

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

        aiState = GoblinAIState.Patrolling;

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
        if (playerControlling) PlayerController.instance.SetAllowMovement(false);

        StartCoroutine(HandleStab());
    }

    /// <summary>
    /// Coroutine handling the AI state changes, AI delay, and locking movement for the player when stabbing
    /// </summary>
    /// <returns> Time breaks </returns>
    public IEnumerator HandleStab()
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

        Debug.Log(stabVelocity.magnitude);

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

        if (!playerControlling) aiState = GoblinAIState.Chasing;
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

        prevSpinVelocity = velocity;

        float timeStarted = Time.time;
        float rotationalSpeed = 0;
        Vector3 targetVelocity = transform.forward;
        targetVelocity.y = 0;
        targetVelocity = targetVelocity.normalized * spinSpeed;

        float accelerationTime;
        // Handle low health AI behavior here in the future, (apply random rotational offset depending on health)
        if (!IsLowHealth()) accelerationTime = standardAccelerationPeriod;
        else accelerationTime = lowHealthAccelerationPeriod;

        while (Time.time - timeStarted < spinDuration)
        {
            if (playerControlling) PlayerController.instance.SetAllowMovement(false); // Helps if player possesses enemy mid-attack
            if (Time.time - timeStarted < accelerationTime)
            {
                if (velocity != prevSpinVelocity)
                {
                    targetVelocity = velocity.normalized * spinSpeed; // If we deflected while speeding up then adjust target velocity
                    Debug.Log("Deflected");
                }

                velocity = Vector3.Lerp(velocity, targetVelocity, Time.deltaTime / accelerationTime);

                rotationalSpeed = Mathf.Lerp(rotationalSpeed, spinRotationalSpeed, Time.deltaTime / accelerationTime);
            }

            GetComponent<CharacterController>().Move(velocity * Time.deltaTime);
            transform.Rotate(Vector3.up, rotationalSpeed * Time.deltaTime);
            yield return null;

            prevSpinVelocity = velocity;
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

        if (!playerControlling) aiState = GoblinAIState.Chasing;
        else PlayerController.instance.SetAllowMovement(true);
    }

    /// <summary>
    /// Temporarily here, just for show at the moment
    /// </summary>
    /// <returns> False </returns>
    public bool IsLowHealth()
    {
        return false;
    }

    public void Dash()
    {
        isDashing = true;
        health.SetInvincible(true);
        PlayerController.instance.SetAllowMovement(false);

        GameObject hitbox = Instantiate(dashHitbox, transform);
        hitbox.GetComponent<DefaultHitbox>().Init(this, dmg: dashDamage, attackDuration: dashDuration, status: dashEffects);

        StartCoroutine(HandleDashMovement(hitbox));
    }

    private IEnumerator HandleDashMovement(GameObject hitbox)
    {
        float timeSinceStarted = 0f;

        while (timeSinceStarted < dashDuration)
        {
            if (hitbox.GetComponent<DefaultHitbox>().HasHitWall())
            {
                StartCoroutine(EnableMovement());
                isDashing = false;
                health.SetInvincible(false);
                attackingSecondary = false;
                aiState = GoblinAIState.Chasing;

                transform.position = transform.position - transform.forward.normalized * dashSpeed * Time.deltaTime;

                yield break;
            }

            transform.position = transform.position + transform.forward.normalized * dashSpeed * Time.deltaTime;
            timeSinceStarted += Time.deltaTime;
            yield return null;
        }

        transform.position = transform.position + transform.forward.normalized * dashSpeed * Time.deltaTime;

        Destroy(hitbox);

        StartCoroutine(EnableMovement());
        isDashing = false;
        health.SetInvincible(false);
        attackingSecondary = false;
        aiState = GoblinAIState.Chasing;
    }

    /// <summary>
    /// Function that follows the flow chart to set behavior for the goblin
    /// </summary>
    public override void SetBehavior()
    {
        target = playerController.currentCharacter; // Always update this
        if (playerControlling || inProcess) return;

        if (aiState == GoblinAIState.Patrolling) // If patrolling
        {
            Patrol();
        }
        else if (aiState == GoblinAIState.Chasing)
        {
            Chase();
        }
        else if (aiState == GoblinAIState.Surrounding)
        {
            Surround();
        }
        else if (aiState == GoblinAIState.Searching)
        {
            Search();
        }
    }

    /// <summary>
    /// Handles finding a path in the graph based on the state
    /// </summary>
    public override void FindPath()
    {
        if (aiState == GoblinAIState.Patrolling)
        {
            if (pathState == PathState.Unset)
            {
                pathState = PathState.Searching;
                SetPatrollingPoint();
            }
            
        }
        else if (aiState == GoblinAIState.Chasing)
        {
            surroundPoint = currentPlayer.FindClosestSurroundingPoint(this);
            if (surroundPoint) // If there is a valid point
            {
                pathState = PathState.Searching;
                StartCoroutine(GraphBuilder.instance.AStarSearch(this, surroundPoint.transform.position));
            }
        }
        else if (aiState == GoblinAIState.Surrounding) // Handles the same as chasing, just in closer range
        {
            surroundPoint = currentPlayer.FindClosestSurroundingPoint(this);
            if (surroundPoint) // If there is a valid point
            {
                pathState = PathState.Searching;
                StartCoroutine(GraphBuilder.instance.AStarSearch(this, surroundPoint.transform.position));
            }
        }
        else if (aiState == GoblinAIState.Searching)
        {
            pathState = PathState.Searching;
            if (surroundPoint) // If still set
            {
                StartCoroutine(GraphBuilder.instance.AStarSearch(this, surroundPoint.transform.position));
            }
            else
            {
                StartCoroutine(GraphBuilder.instance.AStarSearch(this, lastTargetLocation));
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
        else if (CanHearTarget(target.transform))
        {
            TransitionToSearch();
        }

        AIMove();

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

        if (aiState == GoblinAIState.Patrolling) // If a valid patrol point
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
        aiState = GoblinAIState.Chasing;
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
        if (!LookForPlayer() && !RequestLocation()) // If Goblin cannot see player and not being communicated location, search
        {
            TransitionToSearch();
            return;
        }


        if (pathState == PathState.Set || (pathState == PathState.Searching && currentPath != null))
        {
            AIMove();
            if (debugging)
            {
                UpdatePath(false);
            }
        }

        if (Vector3.Distance(transform.position, currentPlayer.transform.position) <= currentPlayer.surroundingRadius + 1.5) // If within a meter and a half of surrounding radius
        {
            aiState = GoblinAIState.Surrounding;
            if (currentPlayer.TryGetComponent(out SurroundingPoints points))
            {
                points.AddSurroundingEnemy(this);
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
            AIMove();
            if (debugging)
            {
                UpdatePath(false);
            }
        }

        if (Vector3.Distance(transform.position, currentPlayer.transform.position) > surroundingRadius + 1.5) // If within a meter and a half of surrounding radius
        {
            aiState = GoblinAIState.Chasing;
            if (currentPlayer.TryGetComponent(out SurroundingPoints points))
            {
                points.RemoveSurroundingEnemy(this);
            }
        }
        else if (Vector3.Distance(transform.position, currentPlayer.transform.position) <= primaryAttackRange)
        {
            ReactAttack();
        }
    }

    /// <summary>
    /// Function that executes when the Goblin is searching
    /// Will navigate to the last known player location then go back to patrolling
    /// </summary>
    public void Search()
    {
        if (LookForPlayer()) // Constantly look for player
        {
            StartCoroutine(SpotPlayer());
            return;
        }
        else if (RequestLocation())
        {
            StartCoroutine(SpotPlayer(fromGoblin: true));
            return;
        }

        if (CanHearTarget(currentPlayer.transform)) // Constantly listen for player
        {
            TransitionToSearch(); // Resets last player position
        }

        if (pathState == PathState.Set || (pathState == PathState.Searching && currentPath != null))
        {
            AIMove();
            if (debugging)
            {
                UpdatePath(false);
            }
        }

        if ((agent.destination - transform.position).magnitude <= agent.stoppingDistance)
        {
            // Lost target, start patrolling again
            aiState = GoblinAIState.Patrolling;
        }
    }

    /// <summary>
    /// Function that is called when changing to search mode
    /// Sets state and last target location
    /// </summary>
    public void TransitionToSearch()
    {
        lastTargetLocation = currentPlayer.transform.position;
        aiState = GoblinAIState.Searching;
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
    /// When surrounding, the goblin may receive a command to attack
    /// When this happens, there is a 50% chance that it will stab and a 50% chance it will spin
    /// </summary>
    /// <param name="points"> The surrounding points calling this function </param>
    /// <returns> True if attack is started, false otherwise </returns>
    public override bool AttackFromSurrounding(SurroundingPoints points)
    {
        int attackChoice;

        if (CheckPrimaryUsable() && CheckSecondaryUsable())
        {
            attackChoice = Random.Range(0, 2);
        }
        else if (CheckPrimaryUsable())
        {
            attackChoice = 0;
        }
        else if (CheckSecondaryUsable())
        {
            attackChoice = 1;
        }
        else
        {
            return false;
        }

        points.RemoveSurroundingEnemy(this);

        if (attackChoice == 0) // Stabbing
        {
            Debug.Log("Stabbing");
            aiState = GoblinAIState.AttackStab;
            StartCoroutine(HandleStab());
        }
        else // Spinning
        {
            aiState = GoblinAIState.AttackSpin;
            SecondaryAttack();

            foreach (Goblin goblin in points.GetEnemiesSameType(this)) // Gets all available goblins to also spin
            {
                if (goblin.CheckSecondaryUsable())
                {
                    goblin.SecondaryAttack();
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Attack decider called when reacting to the player getting too close
    /// </summary>
    public void ReactAttack()
    {
        int attackChoice;

        if (CheckPrimaryUsable() && CheckSecondaryUsable())
        {
            attackChoice = Random.Range(0, 5);
        }
        else if (CheckPrimaryUsable())
        {
            attackChoice = 0;
        }
        else if (CheckSecondaryUsable())
        {
            attackChoice = 4;
        }
        else
        {
            return;
        }

        if (attackChoice < 4) // Stabbing
        {
            aiState = GoblinAIState.AttackStab;
            StartCoroutine(HandleStab());
        }
        else // Spinning
        {
            aiState = GoblinAIState.AttackSpin;
            SecondaryAttack();
        }
    }
}
