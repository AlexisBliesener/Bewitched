using DG.Tweening;
using FMOD.Studio;
using FMODUnity;
using NaughtyAttributes;
using System.Collections;
using UnityEngine;
public class Ogre : Enemy
{
    [Header("Ogre Prefabs/Effects"), ShowIf("dev")]
    [Tooltip("Ogre Bat Prefab")]
    [SerializeField] GameObject batHitboxPrefab;
    [Tooltip("Pivot Prefab"), ShowIf("dev")]
    [SerializeField] GameObject batPivot;

    [Tooltip("Bat Swing Status Effects"), ShowIf("dev")]
    [SerializeField] AttackStatusEffects batSwingEffects;

    [Tooltip("Ogre Slam Bat Hitbox"), ShowIf("dev")]
    [SerializeField] GameObject slamHitboxPrefab;

    [Tooltip("Slam Bat Status Effects"), ShowIf("dev")]
    [SerializeField] AttackStatusEffects slamBatEffects;

    [Tooltip("Slam Impact Status Effects"), ShowIf("dev")]
    [SerializeField] AttackStatusEffects slamImpactEffects;

    [Tooltip("Scream effects"), ShowIf("dev")]
    [SerializeField] AttackStatusEffects screamEffects;
    [Header("Ogre Settings")]

    [Tooltip("Bat Swing Damage")]
    [SerializeField, Range(0, 100)] float batSwingDamage = 30f;
    [Tooltip("Bat Swing Angle")]
    [SerializeField, Range(0, 360)] float batSwingAngle = 60f;
    [Tooltip("Bat Swing Duration")]
    [SerializeField, Range(0, 10)] float batSwingDuration = 0.5f;
    [Tooltip("Bat Windup Period")]
    [SerializeField, Range(0, 10)] float batWindupPeriod = 0.5f;
    [Header("Ogre Jump Settings")]
    [Tooltip("Ogre Jump Gravity")]
    [SerializeField, Range(0, 100)] float ogreJumpGravity = 40f;
    [Tooltip("Ogre Jump Speed")]
    [SerializeField, Range(0, 100)] float ogreJumpSpeed = 25f;
    [Tooltip("Ogre Jump Bat Damage")]
    [SerializeField, Range(0, 100)] float ogreJumpBatDamage = 50f;
    [Tooltip("Ogre Jump Slam Damage")]
    [SerializeField, Range(0, 100)] float ogreJumpSlamDamage = 20f;
    [Tooltip("Ogre Jump Minimum Knockback")]
    [SerializeField, Range(0, 100)] float ogreJumpKnockbackMinimum = 20f;
    [Tooltip("Ogre Jump Maximum Knockback")]
    [SerializeField, Range(0, 100)] float ogreJumpKnockbackMaximum = 70f;
    [Tooltip("Ogre Slam Knockback Range")]
    [SerializeField, Range(0, 100)] float ogreJumpSlamImpactRange = 8f;
    [Header("Ogre Scream Settings")]
    [Tooltip("Scream radius")]
    [SerializeField] float screamRange = 5;
    [Tooltip("Scream windup time")]
    [SerializeField] float screamWindupDuration = 0.5f;
    [Header("Ogre Sitting Settings")]
    [Tooltip("Minimum time for ogre to sit")]
    [SerializeField, Range(0, 10)] float minSittingTime = 3f;
    [Tooltip("Maximum time for ogre to sit")]
    [SerializeField, Range(0, 10)] float maxSittingTime = 7f;
    [SerializeField, Tooltip("Offset for the attack indicator"), ShowIf("dev")]
    private Vector3 offsetAttackIndicator = new Vector3(0, 2.5f, 0);
    [SerializeField, Tooltip("Offset for the pivot for the bat"), ShowIf("dev")]
    private Vector3 offsetPivotBat = new Vector3(0, 0, 0);
    [SerializeField, Tooltip("Offset for the target position when the oge locked on the player"), ShowIf("dev")]
    private float offsetForTargetPosition = 1.5f;

    [Tooltip("Bool determining if ogre is going to patrol point")]
    bool outGoing = false;
    //Is this an event enemy?
    bool isEventEnemy = false;

    void Start()
    {
        SetPlayerInfo();
        health.SetHealthToMax();
        SetBaseStats();
        SetPatrolOrigin();
        isEventEnemy = TryGetComponent<EventEnemy>(out var e);
        sizeRadius = GetComponent<CharacterController>().radius;
    }

    private void FixedUpdate()
    {
        if (dead || lobotimzed) return;
        ManageSurrounding();
        currentPlayer = playerController.GetCurrentCharacter();
        // SetAIState();
        SetBehavior();
        CreateLocalInvalidArea();
    }
    /// <summary>
    /// Starts the primary attack for the ogre
    /// </summary>
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
    /// <summary>
    /// Starts the secondary attack for the ogre
    /// </summary>
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
            Vector3 targetPos = lockedCharacter.transform.position - (lockedCharacter.transform.position - transform.position).normalized * offsetForTargetPosition;
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
                    if (counterIndicatorVFX != null)
                    {
                        if (counterIndicatorVFX != null)
                        {
                            DestroyCounterIndicator();
                        }
                        counterIndicatorVFX = null;
                        PlayerController.instance.SetCounterAvaliable(null);
                    }
                    if (lockedCharacter == currentPlayer) PlayerController.instance.SetCounterAvaliable(null);
                }
                else // First 3 quarters, attack is dodgable
                {
                    //    dodgable = true;
                    if (counterIndicatorVFX == null)
                    {
                        counterIndicatorVFX = Instantiate(counterIndicatorVFXPrefab, transform);
                        counterIndicatorVFX.transform.localPosition = offsetAttackIndicator;
                        PlayerController.instance.SetCounterAvaliable(this);
                    }
                    if (lockedCharacter == currentPlayer) PlayerController.instance.SetCounterAvaliable(this);
                }
                yield return null;
            }
            transform.position = targetPos;
            GetCharacterController().enabled = true;
        }

        attackStateCoroutine = StartCoroutine(SwingBat());
        yield break;
    }

    /// <summary>
    /// Handles the swing for the bat
    /// </summary>
    /// <returns> Time </returns>
    private IEnumerator SwingBat()
    {
        if (batHitboxPrefab == null || batPivot == null)
        {
            Debug.LogWarning("batHitboxPrefab or batPivot prefabs are not assigned!");
            yield break;
        }
        attackState = AttackState.Attacking;
        float timeSinceStarted = 0f;
        GameObject pivot = Instantiate(batPivot, transform.position + offsetPivotBat, transform.rotation, transform);
        DefaultHitbox pivotHitbox = pivot.GetComponent<DefaultHitbox>();
        pivotHitbox.Init(this, attackDuration: batSwingDuration);
        pivot.SetActive(false);

        GameObject batHitbox = Instantiate(batHitboxPrefab, pivot.transform);
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
        SetMovementValues(true);
        attackState = AttackState.Neutral;

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
        attackStateCoroutine = null;
        timeLastPrimary = Time.time;
        aiState = AIMovementState.Chasing;
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
        if (screamEffects == null)
        {
            Debug.LogWarning("Scream effects are not assigned!");
            yield break;
        }
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

        SetMovementValues(true);
        attackState = AttackState.Neutral;
        if (playerControlling)
        {
            StartCoroutine(EnableMovement());
        }
        else
        {
            aiState = AIMovementState.Chasing;
            attackState = AttackState.Neutral;
        }

        attackStateCoroutine = null;
        attackingSecondary = false;
    }
    //Override of OnDamaged to handle the OgreHit sound effect
    protected override void OnDamaged(float f)
    {
        base.OnDamaged(f);
        if(AudioManager.TryGetReference("OgreHit", out EventReference evRef))
        {
            EventInstance ev = RuntimeManager.CreateInstance(evRef);
            RuntimeManager.AttachInstanceToGameObject(ev, gameObject);
            ev.setParameterByName("Damage", f / health.GetMaxHealth());
            ev.setParameterByNameWithLabel("Possessed", playerControlling ? "True" : "False");
            ev.setParameterByNameWithLabel("Event", isEventEnemy ? "True" : "False");
            ev.start();
            ev.release();
        }
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
            if (!idleAudio.isValid())
            {
                AudioManager.TryPlayInstance("OgreIdle", out idleAudio, true, gameObject);
                idleAudio.setParameterByNameWithLabel("Event", isEventEnemy ? "True" : "False");
            }
            Patrol();
        }
        else if (aiState == AIMovementState.Chasing)
        {
            StopIdleAudio();
            Chase();
        }
        else if (aiState == AIMovementState.Surrounding)
        {
            StopIdleAudio();
            Surround();
        }
        else if (aiState == AIMovementState.Retreating)
        {
            StopIdleAudio();
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
            StartCoroutine(SurroundingPoints.instance.FindPathToPlayer(this, false));
        }
        else if (aiState == AIMovementState.Surrounding) // Handles the same as chasing, just in closer range
        {
            StartCoroutine(SurroundingPoints.instance.FindPathToPlayer(this, true));
        }
        else if (aiState == AIMovementState.Retreating) // Handles the same as chasing, just in closer range
        {
            StartCoroutine(SurroundingPoints.instance.FindPathToPlayer(this, true));
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
                UpdatePath();
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

        StartCoroutine(GraphBuilder.instance.AStarSearch(this, transform.position, walkPoint));
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
                // Since TransitionToState checks for inProcess, we need to set it to false here, to transition to the next state
                inProcess = false;
                TransitionToState(AIMovementState.Chasing);
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
            StartPath();
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
                UpdatePath();
            }
        }
        AILook();
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

        if (pathState == PathState.Set || (pathState == PathState.Searching && currentPath != null))
        {
            if (Vector3.Distance(transform.position, currentPlayer.transform.position) > chaseToSurroundingRadius)
            {
                AIMove();
                if (debugging)
                {
                    UpdatePath();
                }
            }
        }

        lookAtPlayer = true;
        AILook();
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
                UpdatePath();
            }
        }
        AILook();
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
            return true;
        }
        return false;
    }
    /// <summary>
    /// Override of Enemy.Die to handle the ogre's death sound effect.
    /// </summary>
    public override void Die()
    {
        //Stopping any playing sound effects on death.
        if (idleAudio.isValid())
        {
            idleAudio.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        }
        //Play Ogre's Death sound effect
        if (AudioManager.TryPlayInstance("OgreDeath", out EventInstance ev, true, gameObject))
        {
            ev.setParameterByNameWithLabel("Possessed", playerControlling ? "True" : "False");
            ev.setParameterByNameWithLabel("Event", isEventEnemy ? "True" : "False");
        }
        base.Die();
    }
}
