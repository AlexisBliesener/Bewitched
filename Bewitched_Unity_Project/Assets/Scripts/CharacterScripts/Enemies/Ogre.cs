using DG.Tweening;
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

    [Tooltip("Bat Swing Damage")]
    [SerializeField] float batSwingDamage;
    [Tooltip("Bat Swing Angle")]
    [SerializeField] float batSwingAngle;
    [Tooltip("Bat Swing Duration")]
    [SerializeField] float batSwingDuration;
    [Tooltip("Bat Windup Period")]
    [SerializeField] float batWindupPeriod;

    [Tooltip("Bat Swing Status Effects")]
    [SerializeField] AttackStatusEffects batSwingEffects;

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

    [Tooltip("Scream radius")]
    [SerializeField] float screamRange = 5;
    [Tooltip("Scream windup time")]
    [SerializeField] float screamWindupDuration = 0.5f;

    [Tooltip("Scream effects")]
    [SerializeField] AttackStatusEffects screamEffects;

    [Tooltip("Minimum time for ogre to sit")]
    [SerializeField] float minSittingTime = 3;
    [Tooltip("Maximum time for ogre to sit")]
    [SerializeField] float maxSittingTime = 7;

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

        attackingPrimary = true;
        // Debug.Log("Starting swing");
        attackStateCoroutine = StartCoroutine(BatWindup());
    }

    public override void SecondaryAttack()
    {
        attackingSecondary = true;
        timeLastSecondary = Time.time;
        attackStateCoroutine = StartCoroutine(ScreamWindup());
    }

    /// <summary>
    /// Handles the windup for the bat
    /// This version looks to the right of the locked character (will alternate in the future)
    /// </summary>
    /// <returns> Time </returns>
    public IEnumerator BatWindup()
    {
        inCounter = false;
        attackState = AttackState.Windup;
        float timeStarted = 0;
        // For now wait 0.25 seconds, in future wait for animation trigger
        while (timeStarted < batWindupPeriod)
        {
            timeStarted += Time.deltaTime;
            yield return null;
        }
        attackStateCoroutine = StartCoroutine(BatApproach());
    }

    /// <summary>
    /// Handles the approach for the bat swing
    /// </summary>
    /// <returns> Time </returns>
    public IEnumerator BatApproach()
    {
        attackState = AttackState.Approaching;

        if (lockedCharacter)
        {
            Vector3 targetPos = lockedCharacter.transform.position - (lockedCharacter.transform.position - transform.position).normalized * 1.5f;
            targetPos.y = transform.position.y;
            GetCharacterController().enabled = false;
            transform.DOMove(targetPos, chaseTime);
            transform.DOLookAt(targetPos, chaseTime);

            float timeStarted = Time.time;
            while (Time.time - timeStarted < chaseTime)
            {
                if (Time.time - timeStarted >= 3 * chaseTime / 4) // Fourth quarter, not dodgable
                {
                    //   dodgable = false;
                    if (attackIndicator != null)
                    {
                        attackIndicator.GetComponent<MeshRenderer>().material = defaultMaterial;
                        PlayerController.instance.SetCounterAvaliable(null);
                    }
                    if (lockedCharacter == currentPlayer) PlayerController.instance.SetCounterAvaliable(null);
                }
                else // First 3 quarters, attack is dodgable
                {
                    //    dodgable = true;
                    if (attackIndicator != null)
                    {
                        attackIndicator.GetComponent<MeshRenderer>().material = perfectCounterTimeMaterial;
                        PlayerController.instance.SetCounterAvaliable(this);
                    }
                    if (lockedCharacter == currentPlayer) PlayerController.instance.SetCounterAvaliable(this);
                }
                yield return null;
            }
            transform.position = targetPos;
            GetCharacterController().enabled = true;
        }

        if (attackIndicator != null)
        {
            Destroy(attackIndicator);
        }
        attackIndicator = null;

        attackStateCoroutine = StartCoroutine(SwingBat());
        yield break;
    }

    /// <summary>
    /// Handles the swing for the bat
    /// </summary>
    /// <returns> Time </returns>
    private IEnumerator SwingBat()
    {
        attackState = AttackState.Attacking;
        float timeSinceStarted = 0f;

        GameObject pivot = Instantiate(batPivot, transform);
        DefaultHitbox pivotHitbox = pivot.GetComponent<DefaultHitbox>();
        pivotHitbox.Init(this, attackDuration: batSwingDuration);
        pivot.SetActive(false);

        GameObject batHitbox = Instantiate(batHitboxPrefab, transform);
        DefaultHitbox batHitboxHitbox = batHitbox.GetComponent<DefaultHitbox>();
        batHitboxHitbox.Init(this, dmg: batSwingDamage, status: batSwingEffects, attackDuration: batSwingDuration);
        pivotHitbox.AttachHitbox(batHitboxHitbox);

        Vector3 endForward = Quaternion.AngleAxis(-batSwingAngle, Vector3.up) * transform.forward;
        Vector3 startFoward = transform.forward;

        pivot.SetActive(true);

        while (timeSinceStarted < batSwingDuration)
        {
            pivot.transform.forward = Vector3.Lerp(startFoward, endForward, timeSinceStarted / batSwingDuration);
            timeSinceStarted += Time.deltaTime;
            yield return null;
        }

        Destroy(pivot);


        yield return new WaitForSeconds(1); // Temporary cooldown time
        if (!playerControlling)
        {
            aiState = AIMovementState.Retreating;
            attackState = AttackState.Neutral;
            pathState = PathState.Unset;
        }
        else StartCoroutine(EnableMovement());

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

    /// <summary>
    /// TEMPORARY override function until animator is set up
    /// </summary>
    /// <returns> True if usable, false otherwise </returns>
    public override bool CheckPrimaryUsable()
    {
        if (!CheckPrimaryCooldown()) return false;
        if (attackingPrimary || attackingSecondary || stunned) return false;

        return true;
    }

    /// <summary>
    /// Winds up for the scream
    /// </summary>
    /// <returns> Time </returns>
    public IEnumerator ScreamWindup()
    {
        if (playerControlling) PlayerController.instance.SetAllowMovement(false);
        else aiState = AIMovementState.Blocked;
        attackState = AttackState.Windup;

        yield return new WaitForSeconds(screamWindupDuration);

        attackStateCoroutine = StartCoroutine(HandleScream());
    }

    /// <summary>
    /// Handles the scream attack for the ogre
    /// </summary>
    /// <returns> Time delays </returns>
    public IEnumerator HandleScream()
    {
        // Debug.Log("ROAR");
        attackState = AttackState.Attacking;
        Collider[] colliders = Physics.OverlapSphere(transform.position, screamRange, characters);
        foreach (Collider collider in colliders)
        {
            if (collider.gameObject.TryGetComponent(out Character character) && teamID != character.teamID)
            {
                // Debug.Log("Scream hit character " + character);
                screamEffects.ApplyStatusEffects(this, character, null); // No knockback so hitbox isnt needed
            }
        }

        yield return new WaitForSeconds(0.25f); // Wait until end of animation in the future

        if (playerControlling) StartCoroutine(EnableMovement());
        else aiState = AIMovementState.Chasing;

        attackStateCoroutine = null;
        attackingSecondary = false;
    }

    /// <summary>
    /// Runs the proper function based on the state of the AI
    /// </summary>
    public override void SetBehavior()
    {
        target = playerController.currentCharacter; // Always update this
        if (playerControlling || inProcess) return;
        // Debug.Log(aiState);

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

    /// <summary>
    /// Finds a path and starts searching depending on the AI state
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
    /// Patrol handling for the ogre
    /// </summary>
    public override void Patrol()
    {
        // Set path if there is none
        if (pathState == PathState.Unset)
        {
            FindPath();
        }


        if (LookForPlayer())
        {
            // Debug.Log("Spotted player");
            StartCoroutine(SpotPlayer());
            return;
        }

        if (pathState == PathState.Set)
        {
            // Debug.Log(Vector3.Distance(currentPath.GetDestinationPosition(gameObject), transform.position));
            if (currentPath.ReachedDestination(this)) // If we are within stopping range
            {
                // Debug.Log("Reached");
                pathState = PathState.Unset;
                StartCoroutine(LookAround()); // Look around
            }

            if (debugging)
            {
                UpdatePath(false);
            }
            AIMove();
            AILook();
        }
        else // If no current path, mark as available
        {
            reachedWalkpoint = false;
        }
    }

    /// <summary>
    /// Called in first frame, sets the patrol origin to Ogre position
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
        // Debug.Log("Patrol origin: " + patrolOrigin);
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
        // Debug.Log(walkPoint);
        Debug.DrawRay(transform.position, Vector3.up * 10, Color.yellow, 10);

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

        pathState = PathState.Set;
        return true;
    }

    /// <summary>
    /// Handles the Ogre's behavior when it sees a player
    /// </summary>
    /// <returns> Waits for animations/sounds </returns>
    private IEnumerator SpotPlayer()
    {
        inProcess = true;
        aiState = AIMovementState.Chasing;
        if (debugging)
        {
            DestroyPath();
        }

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
        outGoing = !outGoing;
        if (debugging)
        {
            DestroyPath();
        }

        inProcess = true;
        float timer = 0;

        while (timer < 1) // Wait 1 second for now, will change this to be a bool checking the end of looking animation
        {
            if (LookForPlayer())
            {
                StartCoroutine(SpotPlayer());
                yield break;
            }
            timer += Time.deltaTime;
            yield return null;
        }

        if (outGoing) // when done, determine if we sit or turn around
        {
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
        // Debug.Log("Start sit");
        inProcess = true;

        yield return new WaitForSeconds(Random.Range(minSittingTime, maxSittingTime));

        inProcess = false;
        // Debug.Log("End sit");
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
        // Set path if there is none
        if (pathState == PathState.Unset)
        {
            FindPath();
        }
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

        // Set path if there is none
        if (pathState == PathState.Unset)
        {
            FindPath();
        }
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
    /// Handles Ogre attacking chance and triggering
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
            // Debug.Log(primaryAttackChance.ToString() + " " + totalOdds);
            float choice = Random.Range(0, totalOdds);
            if (choice <= primaryAttackChance) // Primary attack selected
            {
                StartCoroutine(BeginPrimary());
            }
            else
            {
                StartCoroutine(BeginSecondary());
            }
            points.RemoveSurroundingEnemy(this);
            return true;
        }
        return false;
    }
    /// <summary>
    /// Override of Enemy.Die to change the level music to the outro
    /// </summary>
    public override void Die()
    {
        AudioManager.ChangeMusicParameter("End", "True");
        base.Die();
    }
}
