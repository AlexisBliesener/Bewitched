using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ogre : Enemy
{
    [Header("Ogre Settings")]
    [Tooltip("Ogre Bat Prefab")]
    [SerializeField] GameObject batHitboxPrefab;
    [Tooltip("Pivot Prefab")]
    [SerializeField] GameObject batPivot;

    [Tooltip("Minimum Bat Swing Damage")]
    [SerializeField] float minimumBatSwingDamage;
    [Tooltip("Maximum Bat Swing Damage")]
    [SerializeField] float maximumBatSwingDamage;

    [Tooltip("Minimum Bat Swing Angle")]
    [SerializeField] float minimumBatSwingAngle;
    [Tooltip("Maximum Bat Swing Angle")]
    [SerializeField] float maximumBatSwingAngle;

    [Tooltip("Minimum Bat Swing Knockback")]
    [SerializeField] float minimumBatSwingKnockback;
    [Tooltip("Maximum Bat Swing Knockback")]
    [SerializeField] float maximumBatSwingKnockback;

    [Tooltip("Bat Swing Duration")]
    [SerializeField] float batSwingDuration;
    [Tooltip("Maximum Bat Swing Charge Time")]
    [SerializeField] float batSwingChargeTime;

    [Tooltip("Bat Swing Status Effects")]
    [SerializeField] AttackStatusEffects batSwingEffects = new AttackStatusEffects();

    [Tooltip("Ogre Slam Bat Hitbox")]
    [SerializeField] GameObject slamHitboxPrefab;
    [Tooltip("Ogre Jump Gravity")]
    [SerializeField] float ogreJumpGravity;
    [Tooltip("Ogre Jump Speed")]
    [SerializeField] float ogreJumpSpeed;
    [Tooltip("Ogre Jump Bat Damage")]
    [SerializeField] float ogreJumpBatDamage;
    [Tooltip("Ogre Jump Slam Damage")]
    [SerializeField] float ogreJumpSlamDamage;
    [Tooltip("Ogre Jump Minimum Knockback")]
    [SerializeField] float ogreJumpKnockbackMinimum;
    [Tooltip("Ogre Jump Maximum Knockback")]
    [SerializeField] float ogreJumpKnockbackMaximum;
    [Tooltip("Ogre Slam Knockback Range")]
    [SerializeField] float ogreJumpSlamImpactRange = 8;

    [Tooltip("Slam Bat Status Effects")]
    [SerializeField] AttackStatusEffects slamBatEffects;

    [Tooltip("Slam Impact Status Effects")]
    [SerializeField] AttackStatusEffects slamImpactEffects;

    [Tooltip("Minimum time for ogre to sit")]
    [SerializeField] float minSittingTime = 3;
    [Tooltip("Maximum time for ogre to sit")]
    [SerializeField] float maxSittingTime = 7;

    bool isSwinging = false;
    bool isCharging = false;

    float currentBatSwingDamage;
    float currentBatSwingAngle;
    float currentBatSwingKnockback;

    float timeSwingStarted;

    float jumpVelocity = 0;

    Quaternion minAngle;
    Quaternion maxAngle;

    // Secondary stuff

    GameObject slamBatHitbox;

    float groundHeight;
    bool jumping = false;

    [Tooltip("Bool determining if ogre is going to patrol point")]
    bool outGoing = false;

    void Start()
    {
        SetPlayerInfo();
        health.SetHealthToMax();
        SetBaseStats();
        SetPatrolOrigin();
    }

    private void FixedUpdate()
    {
        currentPlayer = playerController.GetCurrentCharacter();

        SetBehavior();
    }

    public override void PrimaryAttack()
    {
        isCharging = true;
        currentBatSwingKnockback = minimumBatSwingKnockback;
        currentBatSwingDamage = minimumBatSwingDamage;
        currentBatSwingAngle = minimumBatSwingAngle;

        timeSwingStarted = Time.time;
        PlayerController.instance.SetAllowMovement(false);
        attackingPrimary = true;

        //if (!playerControlling || releasePrimaryImm) ReleasePrimary();
        //releasePrimaryImm = false;
    }

    public override void SecondaryAttack()
    {
        attackingSecondary = true;
        timeLastSecondary = Time.time;
        PlayerController.instance.SetAllowMovement(false);

        groundHeight = transform.position.y;
        jumping = true;
        jumpVelocity = ogreJumpSpeed;
    }

    //public override void ReleasePrimary()
    //{
    //    base.ReleasePrimary();
    //    if (!isCharging) return;

    //    isCharging = false;
    //    timeLastPrimary = Time.time;

    //    minAngle = Quaternion.Euler(0, currentBatSwingAngle / 2, 0) * Quaternion.LookRotation(transform.forward);
    //    maxAngle = Quaternion.Euler(0, -currentBatSwingAngle / 2, 0) * Quaternion.LookRotation(transform.forward);

    //    GameObject pivot = Instantiate(batPivot, transform);
    //    pivot.GetComponent<DefaultHitbox>().Init(this, attackDuration: batSwingDuration);
    //    pivot.SetActive(false);

    //    GameObject batHitbox = Instantiate(batHitboxPrefab, transform);
    //    batHitbox.GetComponent<DefaultHitbox>().Init(this, dmg: currentBatSwingDamage, status: batSwingEffects, attackDuration: batSwingDuration);
    //    pivot.GetComponent<DefaultHitbox>().AttachHitbox(batHitbox.GetComponent<DefaultHitbox>());

    //    pivot.SetActive(true);
    //    Debug.Log(batSwingDuration);

    //    StartCoroutine(SwingBat(pivot));
    //}

    public void ChargeBatSwing()
    {
        if (isCharging)
        {
            float timeVal = (Time.time - timeSwingStarted) / batSwingChargeTime;

            if (timeVal < 1)
            {
                currentBatSwingAngle = Mathf.Lerp(minimumBatSwingAngle, maximumBatSwingAngle, timeVal);
                currentBatSwingDamage = Mathf.Lerp(minimumBatSwingDamage, maximumBatSwingDamage, timeVal);
                currentBatSwingKnockback = Mathf.Lerp(minimumBatSwingKnockback, maximumBatSwingKnockback, timeVal);
            }
        }
    }

    private IEnumerator SwingBat(GameObject pivot)
    {
        float timeSinceStarted = 0f;

        while (timeSinceStarted < batSwingDuration)
        {
            pivot.transform.rotation = Quaternion.Lerp(minAngle, maxAngle, timeSinceStarted / batSwingDuration);
            timeSinceStarted += Time.deltaTime;
            yield return null;
        }

        Destroy(pivot);

        StartCoroutine(EnableMovement());
        SetPrimaryStatus(false);
        isSwinging = false;
    }

    public void HandleJumpMovement()
    {
        if (attackingSecondary)
        {
            jumpVelocity -= ogreJumpGravity * Time.deltaTime;

            transform.position = new Vector3(transform.position.x, transform.position.y + jumpVelocity * Time.deltaTime, transform.position.z);

            if (jumpVelocity <= 0 && jumping)
            {
                jumping = false;
                // Instantiate bat hitbox
                slamBatHitbox = Instantiate(slamHitboxPrefab, transform);
                slamBatHitbox.GetComponent<DefaultHitbox>().Init(this, dmg: ogreJumpBatDamage, slamDMG: ogreJumpSlamDamage, status: slamBatEffects, attackDuration: 10);
            }

            if (transform.position.y <= groundHeight) // Hit ground
            {
                transform.position = new Vector3(transform.position.x, groundHeight, transform.position.z);

                slamBatHitbox.GetComponent<DefaultHitbox>().SlamImpact(slamImpactEffects);

                attackingSecondary = false;
                StartCoroutine(EnableMovement());
            }
        }
    }

    public override bool CheckPrimaryUsable()
    {
        if (isCharging) return false;
        return base.CheckPrimaryUsable();
    }

    /// <summary>
    /// Runs the proper function based on the state of the AI
    /// </summary>
    public override void SetBehavior()
    {
        target = playerController.currentCharacter; // Always update this
        if (playerControlling || inProcess) return;

        if (aiState == AIMovementState.Patrolling)
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
    /// Patrol handling for the ogre
    /// </summary>
    public override void Patrol()
    {
        if (LookForPlayer())
        {
            StartCoroutine(SpotPlayer());
            return;
        }

        AIMove();
        //AIRotate();

        if (pathState == PathState.Set)
        {
            Debug.Log(Vector3.Distance(currentPath.GetDestinationPosition(gameObject), transform.position));
            if (currentPath.ReachedDestination(this)) // If we are within stopping range
            {
                Debug.Log("Reached");
                pathState = PathState.Unset;
                StartCoroutine(LookAround()); // Look around
            }

            if (debugging)
            {
                UpdatePath(false);
            }
            AIMove();
            //AIRotate();
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
    /// This version uses a point of origin separate from the Ogre to place a point
    /// </summary>
    public void SetPatrollingPoint()
    {
        if (!outGoing)
        {
            float randomX = Random.Range(-patrolRange, patrolRange);
            float randomZ = Random.Range(-patrolRange, patrolRange);

            walkPoint = new Vector3(patrolOrigin.x + randomX, patrolOrigin.y, patrolOrigin.z + randomZ);
            walkPoint = GraphBuilder.instance.FindClosestNode(walkPoint).GetPosition(gameObject);
        }
        else
        {
            walkPoint = GraphBuilder.instance.FindClosestNode(patrolOrigin).GetPosition(gameObject);
        }
        Debug.Log(walkPoint);
        Debug.DrawRay(transform.position, Vector3.up * 10, Color.yellow, 10);

        StartCoroutine(GraphBuilder.instance.AStarSearch(this, walkPoint));
    }

    /// <summary>
    /// Validates a walkpoint
    /// </summary>
    /// <returns> True if reachable </returns>
    public override bool ValidatePoint()
    {
        Debug.Log(walkPoint);
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
        Debug.DrawRay(transform.position, Vector3.up * 10, Color.green, 10);
        Debug.Log("Valid");

        pathState = PathState.Set;
        return true;
    }

    /// <summary>
    /// Handles the Ogre's behavior when it sees a player
    /// </summary>
    /// <returns> Waits for animations/sounds </returns>
    private IEnumerator SpotPlayer()
    {
        aiState = AIMovementState.Chasing;
        if (debugging)
        {
            DestroyPath();
        }
        inProcess = true;

        // Roar sound here
        yield return new WaitForSeconds(0.5f); // Half a second for now - ogre should roar at player and start chasing

        inProcess = false;
    }

    /// <summary>
    /// Coroutine to handle the ogre when it reaches its patrol destination
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

        if (outGoing) // If we were going to the patrol point
        {
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
            outGoing = false;
        }
        else // If returning to origin
        {
            if (LookForPlayer())
            {
                StartCoroutine(SpotPlayer());
                yield break;
            }
            timer += Time.deltaTime;
            yield return null;

            outGoing = true;
            StartCoroutine(Sit());
            yield break;
        }

        inProcess = false;
        if (debugging)
        {
            StartPath(false);
        }
    }

    /// <summary>
    /// Waits for a random amount of time before setting a new path
    /// </summary>
    /// <returns></returns>
    private IEnumerator Sit()
    {
        inProcess = true;

        yield return new WaitForSeconds(Random.Range(minSittingTime, maxSittingTime));

        inProcess = false;
    }

    /// <summary>
    /// Chase function for the Ogre - should set paths that focus on attacking the player head on
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
            if (Vector3.Distance(transform.position, currentPath.GetDestinationPosition(gameObject)) > surroundingToChaseRadius) // If out of range
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
    /// Handles Goblin attacking chance and triggering
    /// </summary>
    /// <param name="points"> The points calling this function </param>
    /// <returns> True if attacking, false otherwise </returns>
    public override bool AttackFromSurrounding(SurroundingPoints points)
    {
        float totalOdds = 0;

        if (CheckPrimaryUsable())
        {
            totalOdds += primaryAttackChance;
        }
        if (CheckSecondaryUsable()) // In the future use this if being attacked by player
        {
            totalOdds += secondaryAttackChance;
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
            }
            points.RemoveSurroundingEnemy(this);
            return true;
        }
        return false;
    }
}
