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

    [Tooltip("If in windup for primary")]
    bool inPrimaryWindup = false;
    [Tooltip("Ogre animator script that controls the ogre animations")]
    private OgreAnimator animator;

    void Start()
    {
        animator = GetComponentInChildren<OgreAnimator>();
        SetPlayerInfo();
        health.SetHealthToMax();
        SetBaseStats();
        SetPatrolOrigin();
        isEventEnemy = TryGetComponent<EventEnemy>(out var e);
        sizeRadius = GetComponent<CharacterController>().radius;

    }

    private void FixedUpdate()
    {
        Vector3 currentRotation = transform.eulerAngles;
        currentRotation.x = 0;
        currentRotation.z = 0;
        transform.eulerAngles = currentRotation;

        if (dead || lobotimzed) return;

        ManageSurrounding();
        currentPlayer = playerController.GetCurrentCharacter();
        SetAIState();
        SetBehavior();
        CreateLocalInvalidArea();

        SetDebugString();
        //if (!playerControlling) Debug.Log(debugAIInfo);


        if (playerControlling)
        {
            lockedCharacter = PlayerController.instance.GetLockedTarget();
        }
        else
        {
            lockedCharacter = currentPlayer;
        }

        if (!playerControlling || (lockedCharacter != null && Vector3.Distance(lockedCharacter.transform.position, this.gameObject.transform.position) > moveToTargetDistance))
        {
            animator.SetPrimaryMovementNeeded(true);
        }
        else
        {
            animator.SetPrimaryMovementNeeded(false);
        }
    }

    /// <summary>
    /// Starts the primary attack for the ogre
    /// </summary>
    public override void PrimaryAttack()
    {
        hitCharacter = false;
        SetMovementValues(false);

        if (inPrimaryWindup) return;

        if (lockedCharacter)
        {
            lockedCharacter.SetAttacker(this);
            if (lockedCharacter.TryGetComponent(out Enemy enemy))
            {
                enemy.SetTargeted(true);
            }
        }

        attackingPrimary = true;

        if (playerControlling)
        {
            if (!playerControlling || (lockedCharacter != null && Vector3.Distance(lockedCharacter.transform.position, this.gameObject.transform.position) > moveToTargetDistance))
            {
                inPrimaryWindup = true;
                attackStateCoroutine = StartCoroutine(BatWindup());
            }
            else
            {
                attackStateCoroutine = StartCoroutine(SwingBat());
            }
        }
        else
        {
            inPrimaryWindup = true;
            attackStateCoroutine = StartCoroutine(BatWindup());
        }
    }

    /// <summary>
    /// Starts the primary attack
    /// Chooses between windup and regular hit
    /// </summary>
    public override IEnumerator BeginPrimary()
    {
        if (gameObject != null)
        {
            if (playerControlling)
            {
                if (currentPrimaryComboStep == -1)
                {

                    health.SubHealth(primaryAttackCost);

                    currentPrimaryComboStep += 1;
                    if (currentPrimaryComboStep >= primaryComboSteps)
                    {
                        currentPrimaryComboStep = 0;
                    }

                    timeLastPrimary = Time.time;

                    characterAnimator.SwitchState("PrimaryAttack", currentPrimaryComboStep);
                    yield return StartCoroutine(characterAnimator.WaitForDelay("PrimaryAttack", currentPrimaryComboStep));
                    PrimaryAttack();
                }
            }
            else
            {
                if (!attackingPrimary)
                {
                    attackingPrimary = true;
                    currentPrimaryComboStep = 0;
                    timeLastPrimary = Time.time;
                    characterAnimator.SwitchState("PrimaryAttack", 0);
                    yield return StartCoroutine(characterAnimator.WaitForDelay("PrimaryAttack", 0));
                    PrimaryAttack();
                }
            }
        }
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
        while (timeStarted < 1.125 / animator.GetPrimaryWindupMult())
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
            float dis = Vector3.Distance(lockedCharacter.transform.position, this.gameObject.transform.position);

            Vector3 targetPos = lockedCharacter.transform.position - (lockedCharacter.transform.position - transform.position).normalized * offsetForTargetPosition;
            targetPos.y = transform.position.y;
            GetCharacterController().enabled = false;
            transform.DOMove(targetPos, chaseTime * dis);
            transform.DOLookAt(targetPos, chaseTime * dis);

            float timeStarted = Time.time;
            while (Time.time - timeStarted < chaseTime * dis)
            {
                if (Time.time - timeStarted >= 3 * chaseTime * dis / 4) // Fourth quarter, not dodgable
                {
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

        animator.SetSwing();
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
        pivotHitbox.Init(this, attackDuration: 0.542f / animator.GetPrimaryComboMult(currentPrimaryComboStep == -1 ? 0 : currentPrimaryComboStep));
        pivot.SetActive(false);

        GameObject batHitbox = Instantiate(batHitboxPrefab, pivot.transform);
        DefaultHitbox batHitboxHitbox = batHitbox.GetComponent<DefaultHitbox>();
        batHitboxHitbox.Init(this, dmg: batSwingDamage, status: batSwingEffects, attackDuration: 0.542f / animator.GetPrimaryComboMult(currentPrimaryComboStep == -1 ? 0 : currentPrimaryComboStep));
        pivotHitbox.AttachHitbox(batHitboxHitbox);

        Vector3 endForward = Vector3.zero;
        Vector3 startForward = Vector3.zero;

        if (currentPrimaryComboStep == 0)
        {
            endForward = Quaternion.AngleAxis(-batSwingAngle / 8, Vector3.up) * transform.forward;
            startForward = Quaternion.AngleAxis(7 * batSwingAngle / 8, Vector3.up) * transform.forward;
        }
        else if (currentPrimaryComboStep == 1) 
        {
            endForward = Quaternion.AngleAxis(batSwingAngle / 8, Vector3.up) * transform.forward;
            startForward = Quaternion.AngleAxis(-7 * batSwingAngle / 8, Vector3.up) * transform.forward;
        }

        pivot.SetActive(true);

        while (timeSinceStarted < 0.542f / animator.GetPrimaryComboMult(currentPrimaryComboStep == -1 ? 0 : currentPrimaryComboStep))
        {
            SetMovementValues(false);
            pivot.transform.forward = Vector3.Lerp(startForward, endForward, timeSinceStarted / 0.542f / animator.GetPrimaryComboMult(currentPrimaryComboStep == -1 ? 0 : currentPrimaryComboStep));
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
        SurroundingPoints.instance.RemoveAttackingEnemy(this);
        attackStateCoroutine = null;
        aiState = AIMovementState.Chasing;
        animator.EndPrimary();
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
        SetMovementValues(false);
        attackState = AttackState.Windup;

        float timeStarted = Time.time;
        while (Time.time - timeStarted < 0.417f / animator.GetSecondaryWindupMult())
        {
            SetMovementValues(false);
            yield return null;
        }

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

        attackState = AttackState.Attacking;
        Collider[] colliders = Physics.OverlapSphere(transform.position, screamRange, characters);
        foreach (Collider collider in colliders)
        {
            if (collider.gameObject.TryGetComponent(out Character character) && character != this)
            {
                screamEffects.ApplyStatusEffects(this, character, null);
            }
        }

        yield return new WaitForSeconds(0.25f); // Wait until end of animation in the future

        SetMovementValues(true);
        attackState = AttackState.Neutral;
        SetMovementValues(true);

        attackStateCoroutine = null;
        attackingSecondary = false;
        SurroundingPoints.instance.RemoveAttackingEnemy(this);
        animator.SetSecondaryAttackEnded();
    }

    /// <summary>
    /// Runs the proper function based on the state of the AI
    /// </summary>
    public override void SetBehavior()
    {
        target = playerController.currentCharacter; // Always update this
        if (playerControlling || inProcess) return;
        // Debug.Log(aiState);

        if (aiState == AIMovementState.Patrolling && !isEventEnemy)
        {
            if (!idleAudio.isValid())
            {
                AudioManager.TryPlayInstance("OgreIdle", out idleAudio, true, gameObject);
                idleAudio.setParameterByNameWithLabel("Event", isEventEnemy ? "True" : "False");
            }
            Patrol();
        }
        else if (aiState == AIMovementState.Chasing || (aiState == AIMovementState.Patrolling && isEventEnemy))
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
    public override IEnumerator FindPath()
    {
        if (aiState == AIMovementState.Patrolling)
        {
            if (pathState == PathState.Unset)
            {
                pathState = PathState.Searching;
                yield return StartCoroutine(SetPatrollingPoint());
            }

        }
        else if (aiState == AIMovementState.Chasing)
        {
            yield return StartCoroutine(SurroundingPoints.instance.FindPathToPlayer(this, false));
        }
        else if (aiState == AIMovementState.Surrounding) // Handles the same as chasing, just in closer range
        {
            yield return StartCoroutine(SurroundingPoints.instance.FindPathToPlayer(this, true));
        }
        else if (aiState == AIMovementState.Retreating) // Handles the same as chasing, just in closer range
        {
            yield return StartCoroutine(SurroundingPoints.instance.FindPathToPlayer(this, true));
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
            StartCoroutine( FindPath() );
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
    public IEnumerator SetPatrollingPoint()
    {
        // Debug.Log("Patrol origin: " + patrolOrigin);
        if (!outGoing)
        {
            float randomX = Random.Range(-patrolRange, patrolRange);
            float randomZ = Random.Range(-patrolRange, patrolRange);

            walkPoint = new Vector3(patrolOrigin.x + randomX, patrolOrigin.y, patrolOrigin.z + randomZ);
            walkPoint = GraphBuilder.instance.FindClosestNode(walkPoint, this).GetPosition(gameObject);
        }
        else
        {
            walkPoint = GraphBuilder.instance.FindClosestNode(patrolOrigin, this).GetPosition(gameObject);
        }
        // Debug.Log(walkPoint);
        Debug.DrawRay(transform.position, Vector3.up * 10, Color.yellow, 10);

        yield return StartCoroutine(GraphBuilder.instance.AStarSearch(this, transform.position, walkPoint));
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

        // Set path if there is none
        if (pathState == PathState.Unset)
        {
            StartCoroutine(FindPath());
        }

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
            StartCoroutine(FindPath());
        }

        if (pathState == PathState.Set || (pathState == PathState.Searching && currentPath != null))
        {
            AIMove();
            if (debugging)
            {
                UpdatePath();
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
            StartCoroutine(FindPath());
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
    /// <returns> Cost of attack done </returns>
    public override int AttackFromSurrounding(SurroundingPoints points)
    {
        if (dead || lobotimzed) return 0;
        float totalOdds = 0;
        float remaining = points.GetAvailableAttackPoints();

        if (CheckPrimaryUsable() && primaryAICost <= remaining)
        {
            totalOdds += primaryAttackChance;
        }
        if (CheckSecondaryUsable() && secondaryAICost <= remaining) // In the future use this if being attacked by player
        {
            totalOdds += secondaryAttackChance;
        }

        if (totalOdds > 0)
        {
            // Debug.Log(primaryAttackChance.ToString() + " " + totalOdds);
            float choice = Random.Range(0, totalOdds);
            int cost;
            if (choice <= primaryAttackChance) // Primary attack selected
            {
                StartCoroutine(BeginPrimary());
                cost = primaryAICost;
            }
            else
            {
                StartCoroutine(BeginSecondary());
                cost = secondaryAICost;
            }
            points.AddAttackingEnemy(this, cost);
            return cost;
        }
        return 0;
    }
    /// <summary>
    /// Override to handle event enemy
    /// </summary>
    /// <param name="damage"></param>
    public override void DoHitSoundEffect(float damage)
    {
        if (deathEventReference.IsNull) return;
        //Stopping any playing sound effects on death.
        if (idleAudio.isValid())
        {
            idleAudio.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        }
        //Play Goblin's Death sound effect
        if (!deathEventReference.IsNull)
        {
            EventInstance ev = RuntimeManager.CreateInstance(deathEventReference);
            ev.setParameterByNameWithLabel("Possessed", playerControlling.ToString());
            ev.setParameterByNameWithLabel("Event", isEventEnemy.ToString());
            RuntimeManager.AttachInstanceToGameObject(ev, gameObject);
            ev.start();
            ev.release();
        }
    }
    
    /// <summary>
    /// Override of DoDeathSoundEffect to handle event enemy audio 
    /// </summary>
    protected override void DoDeathSoundEffect()
    {
        if (deathEventReference.IsNull) return;
        //Stopping any playing sound effects on death.
        if (idleAudio.isValid())
        {
            idleAudio.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        }
        //Play Goblin's Death sound effect
        if (!deathEventReference.IsNull)
        {
            EventInstance ev = RuntimeManager.CreateInstance(deathEventReference);
            ev.setParameterByNameWithLabel("Possessed", playerControlling.ToString());
            ev.setParameterByNameWithLabel("Event", isEventEnemy.ToString());
            RuntimeManager.AttachInstanceToGameObject(ev, gameObject);
            ev.start();
            ev.release();
        }
    }
}
