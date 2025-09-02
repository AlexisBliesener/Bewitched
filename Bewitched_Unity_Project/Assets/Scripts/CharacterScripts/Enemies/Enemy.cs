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

    [Tooltip("Highest Priority")]
    public int highestPriority;

    [Tooltip("Lowest Priority")]
    public int lowestPriority;

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

    protected Vector3 previousVelocity = new Vector3(0, 0, 0);

    protected float timePlayerLastSeen;

    public void SetAgentValues()
    {
        agent.stoppingDistance = minStopDistance;
        agent.speed = movementSpeed;
        agent.acceleration = acceleration;
        agent.avoidancePriority = Random.Range(highestPriority, lowestPriority);
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

    public bool IsLookingAtPlayer(Transform location)
    {
        Vector3 playerDirection = (location.position - transform.position).normalized;
        float dp = Vector3.Dot(transform.forward, playerDirection);


        if (dp >= Mathf.Cos(Mathf.Deg2Rad * maxSightAngle))
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
        if (CheckTargetInRange(currentPlayer.transform) && CheckCharacterBehindEnvironment(currentPlayer.transform) && IsLookingAtPlayer(currentPlayer.transform))
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

        HandleDeceleration();
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

    /// <summary>
    /// Handles decelerating the character based on the values in Character.cs
    /// </summary>
    public void HandleDeceleration()
    {
        if (agent.velocity.magnitude < previousVelocity.magnitude)
        {
            agent.acceleration = deceleration;
        }
        else
        {
            agent.acceleration = acceleration;
        }

        previousVelocity = agent.velocity;
    }

    public void StartPath()
    {
        if (destinationMarker == null)
        {
            destinationMarker = Instantiate(destinationMarkerPrefab);
            destinationMarker.transform.position = walkPoint;
        }

        pathVisualizer.positionCount = 0;

        pathVisualizer.positionCount = agent.path.corners.Length;
        pathVisualizer.SetPosition(0, transform.position);

        if (agent.path.corners.Length < 2)
        {
            return;
        }

        for (int i = 1; i < agent.path.corners.Length; i++)
        {
            pathVisualizer.SetPosition(i, agent.path.corners[i]);
        }
    }

    /// <summary>
    /// Draws a path the agent follows
    /// </summary>
    public void UpdatePath()
    {
        if (destinationMarker)
        {
            destinationMarker.transform.position = agent.destination;
        }

        pathVisualizer.positionCount = 0;

        pathVisualizer.positionCount = agent.path.corners.Length;

        if (pathVisualizer.positionCount == 0) return;

        pathVisualizer.SetPosition(0, transform.position);

        if (agent.path.corners.Length < 2)
        {
            return;
        }

        for (int i = 1; i < agent.path.corners.Length; i++)
        {
            pathVisualizer.SetPosition(i, agent.path.corners[i]);
        }
    }

    public void DestroyPath()
    {
        if (destinationMarker != null)
        {
            Destroy(destinationMarker);
            destinationMarker = null;
        }
        pathVisualizer.positionCount = 0;
    }

    public void RemoveTargetPoint()
    {
        surroundPoint = null;
    }
}
