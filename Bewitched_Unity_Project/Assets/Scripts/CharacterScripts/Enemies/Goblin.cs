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

    private bool isDashing = false;

    private void Start()
    {
        SetPlayerInfo();
        SetHealthToMax();
        SetBaseStats();
        SetAgentValues();
        SetDebuggingValues();
        SetPatrolOrigin();
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
        HandleDeceleration();
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
        if (!agent.enabled || inProcess) return;

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
            Chase();
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
            // Start search mode
        }

        if (walkPointSet)
        {
            agent.stoppingDistance = minStopDistance;
        }

        if (agent.remainingDistance <= minStopDistance) // If we are within stopping range
        {
            agent.SetDestination(transform.position); // Stop character
            StartCoroutine(LookAround()); // Look around
        }

        if (debugging)
        {
            UpdatePath();
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
    /// Override function for setting a walkpoint
    /// This version uses a point of origin separate from the Goblin to place points
    /// </summary>
    /// <returns></returns>
    public override bool SetWalkPoint()
    {
        float randomX = Random.Range(-patrolRange, patrolRange);
        float randomZ = Random.Range(-patrolRange, patrolRange);

        walkPoint = new Vector3(patrolOrigin.x + randomX, patrolOrigin.y, patrolOrigin.z + randomZ);
        if (NavMesh.SamplePosition(walkPoint, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            NavMeshPath path = new NavMeshPath();
            float distance = 0;
            for (int i = 1; i < path.corners.Length; i++) // Finds the distance of the path, not just the transform distance
            {
                distance += Vector3.Distance(path.corners[i - 1], path.corners[i]);
            }
            if (agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete && distance <= maxPatrolDistance)
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

        while (!SetWalkPoint()) 
        {
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
        if (debugging)
        {
            DestroyPath();
        }

        inProcess = true;

        // Play animation/noise that the player has been seen
        yield return new WaitForSeconds(1);

        // Alert nearby Goblins of player

        inProcess = false;
        aiState = GoblinAIState.Chasing;
    }

    /// <summary>
    /// Chase function for the Goblin - should set paths that focus on surrounding the player
    /// </summary>
    public override void Chase()
    {
        surroundPoint = currentPlayer.FindClosestSurroundingPoint(this);

        if (surroundPoint) // If there is a valid point
        {
            agent.SetDestination(surroundPoint.transform.position);
            
            if (debugging)
            {
                UpdatePath();
            }
        }
    }
}
