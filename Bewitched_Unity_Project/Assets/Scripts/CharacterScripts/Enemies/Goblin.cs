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
    [Tooltip("Dash Hitbox")]
    [SerializeField] GameObject dashHitbox;
    [Tooltip("Dash Speed")]
    [SerializeField] float dashSpeed = 50;
    [Tooltip("Dash Duration")]
    [SerializeField] float dashDuration = 0.5f;
    [Tooltip("Dash Damage")]
    [SerializeField] float dashDamage = 30;

    [Header("Goblin AI Settings")]
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

    private void Start()
    {
        SetPlayerInfo();
        SetHealthToMax();
        SetBaseStats();
        SetAgentValues();
        SetDebuggingValues();
        SetPatrolOrigin();

        agent.SetAreaCost(3, Mathf.Infinity);

        StartCoroutine(LookAround());
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
        base.PrimaryAttack();

        GameObject shank = Instantiate(knifePrefab, transform);
        shank.GetComponent<KnifeHitBox>().Init(this, knifeDamage, thrustSpeed);

        timeLastPrimary = Time.time;
        attackingPrimary = true;
    }

    public override void SecondaryAttack()
    {
        base.SecondaryAttack();

        Dash();
        attackingSecondary = true;
        timeLastSecondary = Time.time;
    }

    public void Dash()
    {
        isDashing = true;
        invincible = true;
        PlayerController.instance.SetAllowMovement(false);

        GameObject hitbox = Instantiate(dashHitbox, transform);
        hitbox.GetComponent<DashHitBox>().Init(this, dashDamage);

        StartCoroutine(HandleDashMovement(hitbox));
    }

    private IEnumerator HandleDashMovement(GameObject hitbox)
    {
        float timeSinceStarted = 0f;

        while (timeSinceStarted < dashDuration)
        {
            if (hitbox.GetComponent<DashHitBox>().HitWall())
            {
                StartCoroutine(EnableMovement());
                isDashing = false;
                invincible = false;
                attackingSecondary = false;

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
        invincible = false;
        attackingSecondary = false;
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
            Chase();
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
        StopCoroutine(GraphBuilder.instance.AStarSearch(this, walkPoint));

        if (aiState == GoblinAIState.Patrolling)
        {
            if (!usingAStar && needsDestination)
            {
                usingAStar = true;
                SetPatrollingPoint();
            }
            else
            {
                if (!reachedWalkpoint && debugging)
                {
                    usingAStar = true;
                    StartCoroutine(GraphBuilder.instance.AStarSearch(this, currentPath.GetDestinationPosition(gameObject))); // Rebuild path
                    if (debugging)
                    {
                        UpdatePath(false);
                    }
                }
            }
        }
        else if (aiState == GoblinAIState.Chasing)
        {
            surroundPoint = currentPlayer.FindClosestSurroundingPoint(this);
            if (surroundPoint) // If there is a valid point
            {
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
        else if (CanHearTarget(target.transform))
        {
            TransitionToSearch();
        }

        AIMove();

        if (currentPath != null)
        {
            if (currentPath.ReachedDestination(this)) // If we are within stopping range
            {
                agent.SetDestination(transform.position); // Stop character
                reachedWalkpoint = true;
                StartCoroutine(LookAround()); // Look around
            }

            if (debugging)
            {
                UpdatePath(false);
            }
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
        if (aiState == GoblinAIState.Patrolling) // If a valid patrol point
        {
            if (currentPath == null)
            {
                return false;
            }

            if (!currentPath.PathComplete())
            {
                return false;
            }

            float distance = currentPath.GetDistance();

            if (distance <= maxPatrolDistance || Vector3.Distance(transform.position, patrolOrigin) >= patrolRange)
            {
                AnimateMove();


                if (debugging)
                {
                    StartPath(false);
                }

                needsDestination = false;
                reachedWalkpoint = false;
                return true;
            }
            return false;
        }
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
        AnimateIdle(); // Play animation (temporarily idle)
        float timer = 0;

        while (timer < .5f) // Wait .5 seconds for now, will change this to be a bool checking the end of looking animation
        {
            if (LookForPlayer())
            {
                StartCoroutine(SpotPlayer());
                yield break;
            }
            timer += Time.deltaTime;
            yield return null;
        }

        needsDestination = true;
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
        if (debugging)
        {
            DestroyPath();
        }

        inProcess = true;

        timePlayerLastSeen = Time.time;

        // Play animation/noise that the player has been seen
        if (!fromGoblin)
        {
            yield return new WaitForSeconds(1);
        }

        // Alert nearby Goblins of player

        inProcess = false;
        aiState = GoblinAIState.Chasing;
    }

    /// <summary>
    /// Chase function for the Goblin - should set paths that focus on surrounding the player
    /// </summary>
    public override void Chase()
    {
        if (!LookForPlayer() && !RequestLocation()) // If Goblin cannot see player and not being communicated location, search
        {
            TransitionToSearch();
            return;
        }
            
        if (debugging)
        {
            UpdatePath(false);
        }

        if (Vector3.Distance(transform.position, currentPlayer.transform.position) <= surroundingRadius + 0.5) // If within half a meter of surrounding radius
        {
            // Handle Surrounding
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

        if (surroundPoint) // If has a surround point assigned still
        {
            agent.SetDestination(surroundPoint.transform.position);
        }
        else
        {
            agent.SetDestination(lastTargetLocation);
        }

        if (debugging)
        {
            UpdatePath(false);
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
}
