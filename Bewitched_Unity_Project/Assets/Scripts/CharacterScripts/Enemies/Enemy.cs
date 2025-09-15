using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public abstract class Enemy : Character
{
    [Header("Enemy AI Settings")]
    [Tooltip("Navmesh Agent on this character")]
    public NavMeshAgent agent;

    [Tooltip("Minimum Stopping Distance")]
    public float minStopDistance = 0.5f;

    [Tooltip("Minimum Slow Distance")]
    public float minSlowDistance = 3;

    [Tooltip("Pathfinding Priority")]
    public int pathfindingPriority;

    [Tooltip("Last seen time buffer")]
    public float seenBuffer = 0.5f;

    protected PlayerController playerController;

    protected Hag hag;
    protected Character currentPlayer;

    [Tooltip("Masks")]
    public LayerMask ground;
    public LayerMask environment;

    [Tooltip("Sight Range")]
    public float sightRange;

    [Tooltip("Maximum Sight Angle")]
    public float maxSightAngle;

    [Tooltip("Hearing Range")]
    public float hearingRange;

    [Tooltip("Walk Point Range")]
    public float patrolRange;

    [Tooltip("Time before searching")]
    public float timeBeforeSearch = 5;

    [Tooltip("Mini Health Bar Prefab")]
    public GameObject miniBarPrefab;

    [Tooltip("AI Attack Delay")]
    public float attackDelayAI = 0.5f;

    [Tooltip("Leave Body Explosion Range")]
    public float leaveBodyExplosionRadius = 5;

    [Tooltip("Leave Body Explosion Minimum Damage")]
    public float leaveBodyExplosionMinimumDamage = 10;
    [Tooltip("Leave Body Explosion Maximum Damage")]
    public float leaveBodyExplosionMaximumDamage = 40;

    [Tooltip("Leave Body Explosion Minimum Knockback")]
    public float leaveBodyExplosionMinimumKnockback = 10;
    [Tooltip("Leave Body Explosion Maximum Knockback")]
    public float leaveBodyExplosionMaximumKnockback = 30;

    [Tooltip("Point that the Goblin runs to while chasing/surrounding")]
    protected GameObject surroundPoint;

    [Header("Debug Options")]
    [Tooltip("Show Paths, Destinations, etc")]
    public bool debugging = false;
    [Tooltip("Destination Marker Prefab")]
    public GameObject destinationMarkerPrefab;
    [Tooltip("Line Renderer for Path")]
    public LineRenderer pathVisualizer;

    protected GameObject destinationMarker;

    protected bool walkPointSet = false;
    protected bool playerInSightRange, currentInSightRange, targetInSightRange = false;
    protected bool targetInPrimaryRange = false;

    protected bool playerControlling = false; // flag for determining actions (player or AI)

    protected Vector3 walkPoint;

    protected Character target;

    protected Vector3 lastTargetLocation;

    protected bool seenTarget = false;

    protected GameObject minibar;

    protected bool isStunned = false;

    protected bool inAttackDelay = false;

    protected Vector3 velocity = new Vector3(0, 0, 0);

    protected float timePlayerLastSeen;

    protected NavPath currentPath;

    protected bool reachedWalkpoint = true;

    protected bool lookAtPlayer = false;

    [Tooltip("Bool determining if this enemy is using the A* search")]
    protected bool usingAStar = false;

    [Tooltip("Corner node index we are currently on in our path")]
    protected int currentCornerIndex = 0;

    public bool tempDebugging = false;

    protected enum PathState
    {
        Unset,
        Searching,
        Set
    }

    [Tooltip("Current path state")]
    protected PathState pathState = PathState.Unset;

    /// <summary>
    /// Function for handling movement
    /// </summary>
    public void AIMove()
    {
        if (velocity.magnitude >= 0.01f)
        {
            AnimateMove();
        }
        else
        {
            AnimateIdle();
        }

        if (currentPath == null) // No path, decelerate to 0
        {
            Vector3 direction = Vector3.zero;
            velocity = Vector3.Lerp(velocity, direction, Time.deltaTime * deceleration);
            GetComponent<CharacterController>().Move(velocity * Time.deltaTime);
            return;
        }

        float currentSpeed = velocity.magnitude;
        float stoppingDistance = (currentSpeed * currentSpeed) / (2f * deceleration);

        if (Vector3.Distance(transform.position, currentPath.GetDestinationPosition(gameObject)) <= minStopDistance + stoppingDistance)
        {
            velocity = Vector3.Lerp(velocity, Vector3.zero, Time.deltaTime * deceleration);
            GetComponent<CharacterController>().Move(velocity * Time.deltaTime);
            Vector3 lookDir = Vector3.RotateTowards(transform.forward, (currentPath.GetDestinationPosition(gameObject) - transform.position).normalized, Time.deltaTime * 5, 0);
            transform.rotation = Quaternion.LookRotation(lookDir);
            if (lookAtPlayer)
            {
                Quaternion look = Quaternion.LookRotation(Vector3.Lerp(transform.forward, currentPlayer.transform.position - transform.position, 5 * Time.deltaTime));
                transform.rotation = look;
            }
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

        if (tempDebugging)
        {
            Debug.Log("X: " + xChange + " Z:" + zChange);
        }


        if (velocity.magnitude > movementSpeed)
        {
            velocity = velocity.normalized * movementSpeed;
        }

        if (velocity.magnitude < 0.01f)
        {
            velocity = Vector3.zero;
        }

        GetComponent<CharacterController>().Move(velocity * Time.deltaTime);
        GetComponent<CharacterController>().Move(Vector3.down);

        Quaternion lookRotation;
        if (lookAtPlayer)
        {
            lookRotation = Quaternion.LookRotation(Vector3.Lerp(transform.forward, currentPlayer.transform.position - transform.position, 5 * Time.deltaTime));
        }
        else
        {
            lookRotation = Quaternion.LookRotation(Vector3.Lerp(transform.forward, velocity, 5 * Time.deltaTime));
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

    public void SetAgentValues()
    {
        agent.stoppingDistance = minStopDistance;
        agent.speed = movementSpeed;
        agent.acceleration = acceleration;
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

    public override void SetControlled(bool val)
    {
        StopAllCoroutines();
        playerControlling = val;
        SetPlayerControlledBuffs(val, PlayerController.instance.playerBuffs);

        if (val)
        {
            agent.enabled = false;
            if (minibar)
            {
                Destroy(minibar);
                minibar = null;
            }
        }
        else
        {
            agent.enabled = true;
        }
    }

    public override void Die()
    {
        if (playerControlling)
        {
            playerControlling = false;
            PlayerController.CharacterControlChangeEvent?.Invoke(hag);
        }

        GameObject.FindGameObjectWithTag("Lock Manager").GetComponent<LockManager>().IncrementKills();
        Destroy(minibar);
        minibar = null;
        StopAllCoroutines();
        Destroy(gameObject);
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
            lastTargetLocation = target.transform.position;
            return true;
        }
        return false;
    }

    public virtual void SetBehavior()
    {
        if(!agent.enabled) return;
        if (inAttackDelay) return;
        if (targetInSightRange && CheckCharacterBehindEnvironment(target.transform))
        {
            seenTarget = true;
            lastTargetLocation = target.transform.position;

            if (targetInPrimaryRange)
            {
                agent.enabled = false;
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
                agent.SetDestination(lastTargetLocation);
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

    public virtual void Chase()
    {
        if ((target.transform.position - transform.position).magnitude - target.sizeRadius < 1)
        {
            agent.stoppingDistance = target.sizeRadius + minStopDistance;
            agent.SetDestination(transform.position);
            AnimateIdle();
        }
        else
        {
            agent.stoppingDistance = target.sizeRadius + minStopDistance;
            agent.SetDestination(target.transform.position);
            AnimateMove();
        }
    }

    public virtual void Patrol()
    {
        if(!agent.enabled) return;
        if (!walkPointSet)
        {
            SetWalkPoint();
        }

        if (walkPointSet)
        {
            agent.stoppingDistance = minStopDistance;
            agent.SetDestination(walkPoint);
            AnimateMove();
        }

        Vector3 distance = transform.position - walkPoint;

        if (distance.magnitude < 1)
        {
            walkPointSet = false;
        }
    }

    public virtual bool SetWalkPoint()
    {
        float randomX = Random.Range(-patrolRange, patrolRange);
        float randomZ = Random.Range(-patrolRange, patrolRange);

        walkPoint = new Vector3(transform.position.x + randomX, transform.position.y, transform.position.z + randomZ);
        if (NavMesh.SamplePosition(walkPoint, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            NavMeshPath path = new NavMeshPath();
            if (agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
            {
                walkPoint = hit.position;
                walkPointSet = true;
                agent.SetDestination(walkPoint);
                AnimateMove();

                if (debugging)
                {
                    destinationMarker = Instantiate(destinationMarkerPrefab);
                    destinationMarker.transform.position = walkPoint;
                }

                return true;
            }
        }
        return false;
    }

    public override void SubHealth(float dmg)
    {
        base.SubHealth(dmg);

        if (minibar == null && !playerControlling)
        {
            minibar = Instantiate(miniBarPrefab);
            minibar.GetComponent<MiniHealthBar>().SetCharacter(this);
        }
    }

    public float GetTimeLastHit()
    {
        return timeLastHit;
    }

    public override void CreateHitStun()
    {
        if (!playerControlling)
        {
            if (hitStunActual == null) hitStunActual = Instantiate(hitStunPrefab, transform);
            agent.enabled = false;
        }
    }

    public override void HandleHitStun()
    {
        if (hitStunActual != null)
        {
            if (Time.time - timeLastHit > hitStunDuration)
            {
                if (playerControlling) StartCoroutine(EnableMovement());
                else agent.enabled = true;
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

        agent.enabled = true;
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

        return base.BeginPrimary();
    }

    public override void Explode()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, leaveBodyExplosionRadius, characters);

        foreach (Collider hit in hits)
        {
            Character hitChar = hit.GetComponent<Character>();
            if (hitChar != null)
            {
                if (CheckCharacterBehindEnvironment(hitChar.transform) && hitChar.teamID != teamID)
                {
                    float dist = (hitChar.transform.position - transform.position).magnitude;
                    Vector3 direction = (hitChar.transform.position - transform.position).normalized;

                    float dmg = Mathf.Lerp(leaveBodyExplosionMinimumDamage, leaveBodyExplosionMaximumDamage, (leaveBodyExplosionRadius - dist) / leaveBodyExplosionRadius);
                    float knockback = Mathf.Lerp(leaveBodyExplosionMinimumKnockback, leaveBodyExplosionMaximumKnockback, (leaveBodyExplosionRadius - dist) / leaveBodyExplosionRadius);

                    hitChar.SubHealth(dmg);
                    hitChar.GetComponent<KnockbackControl>().AddImpact(direction, knockback);
                }
            }
        }
    }

    public void StartPath(bool usingAgent = true)
    {
        if (usingAgent == false && currentPath == null) return;

        if (destinationMarker)
        {
            if (usingAgent)
            {
                destinationMarker.transform.position = agent.destination;
            }
            else
            {
                destinationMarker.transform.position = currentPath.GetDestinationPosition(gameObject);
                destinationMarker.transform.position = new Vector3(destinationMarker.transform.position.x, 1, destinationMarker.transform.position.z);
            }
        }
        else
        {
            if (!usingAgent)
            {
                destinationMarker = Instantiate(destinationMarkerPrefab);
                destinationMarker.transform.position = currentPath.GetDestinationPosition(gameObject);
                destinationMarker.transform.position = new Vector3(destinationMarker.transform.position.x, 1, destinationMarker.transform.position.z);
            }
        }

        pathVisualizer.positionCount = 0;

        if (usingAgent)
        {
            pathVisualizer.positionCount = agent.path.corners.Length;
        }
        else
        {
            pathVisualizer.positionCount = currentPath.GetCornerNodes().Count;
        }

        if (pathVisualizer.positionCount < 1) return;

        pathVisualizer.SetPosition(0, transform.position);

        if (usingAgent)
        {
            for (int i = 1; i < agent.path.corners.Length; i++)
            {
                pathVisualizer.SetPosition(i, agent.path.corners[i]);
            }
        }
        else
        {
            for (int i = 1; i < currentPath.GetCornerNodes().Count; i++)
            {
                pathVisualizer.SetPosition(i, new Vector3(currentPath.GetCornerNodes()[i].GetPosition().x, transform.position.y, currentPath.GetCornerNodes()[i].GetPosition().z));
            }
        }
    }

    /// <summary>
    /// Draws a path the agent follows
    /// </summary>
    public void UpdatePath(bool usingAgent = true)
    {
        if (destinationMarker)
        {
            if (usingAgent)
            {
                destinationMarker.transform.position = agent.destination;
            }
            else
            {
                destinationMarker.transform.position = currentPath.GetDestinationPosition(gameObject);
                destinationMarker.transform.position = new Vector3(destinationMarker.transform.position.x, 1, destinationMarker.transform.position.z);
            }
        }
        else
        {
            destinationMarker = Instantiate(destinationMarkerPrefab);
            destinationMarker.transform.position = currentPath.GetDestinationPosition(gameObject);
            destinationMarker.transform.position = new Vector3(destinationMarker.transform.position.x, 1, destinationMarker.transform.position.z);
        }

        pathVisualizer.positionCount = 0;

        if (usingAgent)
        {
            pathVisualizer.positionCount = agent.path.corners.Length;
        }
        else
        {
            pathVisualizer.positionCount = currentPath.GetCornerNodes().Count - currentCornerIndex;
        }

        if (pathVisualizer.positionCount == 0) return;

        pathVisualizer.SetPosition(0, transform.position);

        if (usingAgent)
        {
            for (int i = 1; i < agent.path.corners.Length; i++)
            {
                pathVisualizer.SetPosition(i, agent.path.corners[i]);
            }
        }
        else
        {
            for (int i = currentCornerIndex-1; i < currentPath.GetCornerNodes().Count - currentCornerIndex; i++)
            {
                if (i >= 0)
                {
                    pathVisualizer.SetPosition(i, new Vector3(currentPath.GetCornerNodes()[i].GetPosition().x, transform.position.y, currentPath.GetCornerNodes()[i].GetPosition().z));
                }
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
}
