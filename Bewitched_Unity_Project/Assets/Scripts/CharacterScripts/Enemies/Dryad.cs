using DG.Tweening;
using FMOD.Studio;
using FMODUnity;
using NaughtyAttributes;
using System.Collections;
using UnityEngine;
using UnityEngine.TextCore.Text;
public class Dryad : Enemy
{
    [Header("Dryad Prefabs/Effects")]
    [Tooltip("Drayd dart Prefab"), ShowIf(nameof(dev))]
    [SerializeField] private GameObject dartPrefab;

    [Tooltip("If for some reason the dart didn't hit the wall, it will destroy itself after this time")]
    [SerializeField] private float dartDuration = 10f;

    [Tooltip("Dart forward speed ")]
    [SerializeField] private float dartSpeed = 18f;

    [Tooltip("Dart damage ")]
    [SerializeField] private float dartDamage = 6f;

    [Tooltip("Dart status effect"), ShowIf(nameof(dev))]
    [SerializeField] private AttackStatusEffects dartEffect;
    [Header("Dryad Secondary (Spore Cloud)")]
    [Tooltip("Spore cloud hitbox prefab "), ShowIf(nameof(dev))]
    [SerializeField] private GameObject sporeHitboxPrefab;

    [Tooltip("Damage dealt by spore")]
    [SerializeField] private float sporeDamage = 0.5f;

    [Tooltip("Status effects for spores"), ShowIf(nameof(dev))]
    [SerializeField] private AttackStatusEffects sporeEffect;

    [Tooltip("Windup time before spores activate (for animation...)")]
    [SerializeField] private float sporeWindupTime = 0.25f;
    [SerializeField, Tooltip("Offset for the spore cloud spawn point"), ShowIf(nameof(dev))]
    private Vector3 offSetSpore = new Vector3(0, 0.5f, 0);
    [SerializeField, Tooltip("Offset for the dart spawn point"), ShowIf(nameof(dev))]
    private Vector3 offSetDart = new Vector3(0, 0.5f, 0);
    [SerializeField, Tooltip("Minimum surrounding radius to trigger spore cloud attack")]
    private float minSurroundingRadiusSporeCloud = 0;
    [SerializeField, Tooltip("Maximum surrounding radius to trigger spore cloud attack")]
    private float maxSurroundingRadiusSporeCloud = 8;

    [Tooltip("Time of the rotation to face the target when throwing the dart"), ShowIf(nameof(dev))]
    private float throwFacingTime = 0.25f;

    [Tooltip("Time to wait after locking in on the target so the player can move out of the way")]
    private float lockTargetPositionTime = 0.5f;

    [Tooltip("Low health aggro active, this will make the dryad get closer to the player to the point of the moveToTargetDistance ")]
    private float lowHealthAggroActive = 17.5f; // half of the dryad health
    [Tooltip("Dryad animator script that controls the dryad animations")]
    protected DryadAnimator dryadAnimator;

    void Start()
    {
        dryadAnimator = GetComponentInChildren<DryadAnimator>();
        SetPlayerInfo();
        health.SetHealthToMax();
        SetBaseStats();
        SetPatrolOrigin();
        sizeRadius = GetComponent<CharacterController>().radius;
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        Vector3 currentRotation = transform.eulerAngles;
        currentRotation.x = 0;
        currentRotation.z = 0;
        transform.eulerAngles = currentRotation;
        CreateLocalInvalidArea();
        ManageSurrounding();

        if (playerControlling)
        {
            lockedCharacter = PlayerController.instance.GetLockedTarget();
        }
        else
        {
            lockedCharacter = currentPlayer;
        }

        if (dead || lobotimzed) return;

        currentPlayer = playerController.GetCurrentCharacter();
        SetAIState();
        SetBehavior();

        SetDebugString();
    }

    /// <summary>
    /// Starts the primary attack for the dryad
    /// </summary>
    public override void PrimaryAttack()
    {
        hitCharacter = false;
        SetMovementValues(false);

        if (lockedCharacter)
        {
            lockedCharacter.SetAttacker(this);
            if (lockedCharacter.TryGetComponent(out Enemy enemy))
            {
                enemy.SetTargeted(true);
            }
        }

        if(playerControlling)
        {
            aiControlledOnPrimary = false;
        }
        else
        {
            aiControlledOnPrimary = true;
        }
        attackingPrimary = true;
        attackStateCoroutine = StartCoroutine(ThrowDart(lockedCharacter));
    }

    [ContextMenu("Call me 1")]
    public void CallMe()
    {
        PrimaryAttack();
    }
    [ContextMenu("Call me 2")]
    public void CallMe2()
    {
        SecondaryAttack();
    }

    /// <summary>
    /// Starts the primary attack
    /// </summary>
    /// 
    public override IEnumerator BeginPrimary()
    {
        if (gameObject != null && CheckPrimaryUsable())
        {
            if (playerControlling)
            {
                if((currentPrimaryComboStep == -1 || Time.time - timeLastPrimary >= primaryComboMinTime[currentPrimaryComboStep] / dryadAnimator.GetPrimaryComboMult(currentPrimaryComboStep)))
                {
                    health.SubHealth(primaryAttackCost, this);

                    currentPrimaryComboStep += 1;
                    if (currentPrimaryComboStep >= primaryComboSteps)
                    {
                        currentPrimaryComboStep = 0;
                    }

                    timeLastPrimary = Time.time;

                    dryadAnimator.SwitchState("PrimaryAttack", currentPrimaryComboStep);
                    yield return StartCoroutine(dryadAnimator.WaitForDelay("PrimaryAttack", currentPrimaryComboStep));
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
                    dryadAnimator.SwitchState("PrimaryAttack", 0);
                    yield return StartCoroutine(dryadAnimator.WaitForDelay("PrimaryAttack", 0));
                    PrimaryAttack();
                }
            }
        }
        yield return null;
    }

    /// <summary>
    /// Checks if the secondary attack is usable
    /// The secondary attack is only usable if the player is within the SurroundingRadiusSporeCloud
    /// </summary>
    /// <returns> True if usable, false otherwise </returns>
    public override bool CheckSecondaryUsable()
    {
        if (!CheckSecondaryCooldown()) return false;
        if (attackingPrimary || attackingSecondary || stunned) return false;
        // The secondary attack is only usable if the player is within the surrounding range
        float dist = Vector3.Distance(transform.position, currentPlayer.transform.position);
        float max = currentPlayer.sizeRadius + sizeRadius + maxSurroundingRadiusSporeCloud;
        float min = currentPlayer.sizeRadius + sizeRadius + minSurroundingRadiusSporeCloud;

        if ((!playerControlling) && (dist > (max) || dist < (min))) return false;
        return true;
    }

    public override IEnumerator BeginSecondary()
    {
        dryadAnimator.SwitchState("SecondaryAttack");
        yield return StartCoroutine(dryadAnimator.WaitForDelay("SecondaryAttack", 0));
        if (gameObject)
        {
            if (PlayerController.instance.currentCharacter == this)
            {
                health.SubHealth(secondaryAttackCost, this);
            }
            SecondaryAttack();

        }
    }

    /// <summary>
    /// Starts the secondary attack for the dryad
    /// </summary>
    public override void SecondaryAttack()
    {
        if (!CheckSecondaryUsable()) return;

        if (playerControlling)
        {
            lockedCharacter = PlayerController.instance.GetLockedTarget();
        }
        else
        {
            lockedCharacter = currentPlayer;
        }

        if (lockedCharacter)
        {
            lockedCharacter.SetAttacker(this);
            if (lockedCharacter.TryGetComponent(out Enemy enemy))
            {
                enemy.SetTargeted(true);
            }
        }

        attackingSecondary = true;
        timeLastSecondary = Time.time;
        attackStateCoroutine = StartCoroutine(SporeCloud(lockedCharacter));
    }

    /// <summary>
    /// Handles the spore cloud attack, it will start by waiting for the windup time
    /// it will rotate to face the target and then start the attack
    /// </summary>
    /// <param name="lockedCharacter"> Character locked to the player </param>
    private IEnumerator SporeCloud(Character tempLockedCharacter)
    {
        inCounter = false;
        attackState = AttackState.Windup;
        SetMovementValues(false);
        hitCharacter = false;

        // face target in windup... 
        if (tempLockedCharacter)
        {
            Vector3 lookPos = tempLockedCharacter.transform.position;
            lookPos.y = transform.position.y;

            transform.DOKill();

            yield return transform.DOLookAt(lookPos, sporeWindupTime).WaitForCompletion();
        }
        else
        {
            // if there is no target we will just wait for the windup time
            yield return new WaitForSeconds(sporeWindupTime);
        }


        attackState = AttackState.Attacking;

        Vector3 spawnPos = transform.position + (offSetSpore);
        GameObject sporeObj = Instantiate(sporeHitboxPrefab, spawnPos, transform.rotation);
        DefaultHitbox hitBox = sporeObj.GetComponentInChildren<DefaultHitbox>();
        if (hitBox != null)
        {
            hitBox.Init(this, dmg: sporeDamage, slamDMG: 0f, forwardVelocity: 0f, rotationalVelocity: 0f, status: sporeEffect, attackDuration: 1.3f / dryadAnimator.GetSecondaryWindupMult());
        }

        float startedTime = Time.time;
        while (Time.time - startedTime < 1.3f / dryadAnimator.GetSecondaryWindupMult())
        {
            SetMovementValues(false);
            yield return null;
        }

        SetMovementValues(true);

        if (tempLockedCharacter)
        {
            tempLockedCharacter.SetAttacker(null);
            if (tempLockedCharacter.TryGetComponent(out Enemy enemy))
            {
                enemy.SetTargeted(false);
            }
        }

        attackingSecondary = false;
        attackState = AttackState.Neutral;
        pathState = PathState.Unset;
        aiState = AIMovementState.Retreating;
        attackStateCoroutine = null;
        timeLastSecondary = Time.time;
        dryadAnimator.SetSecondaryAttackEnded();

        SurroundingPoints.instance.RemoveAttackingEnemy(this);
    }

    /// <summary>
    /// Handle the dart throwing attack 
    /// </summary>
    public IEnumerator ThrowDart(Character tempLockedCharacter)
    {
        inCounter = false;
        attackState = AttackState.Windup;
        Vector3 targetPos;
        if (tempLockedCharacter)
        {
            Vector3 lookPos = tempLockedCharacter.transform.position;
            lookPos.y = transform.position.y;

            targetPos = tempLockedCharacter.transform.position;

            // Rotate to face the target when throwing the dart
            yield return transform.DOLookAt(lookPos, throwFacingTime).WaitForCompletion();
        }
        else
        {
            targetPos = transform.position;
        }
        
        // if the player is not controlling, lock in for half a second (so the player has a chance to move out of the way)
        if (!playerControlling)
        {
            counterIndicatorVFX = Instantiate(counterIndicatorVFXPrefab, transform);
            counterIndicatorVFX.transform.localPosition = new Vector3(0, 4.5f, 0);
            yield return new WaitForSeconds(lockTargetPositionTime); 

            Destroy(counterIndicatorVFX);
        }
        
        attackState = AttackState.Attacking;

        Vector3 spawnPos = transform.position + offSetDart;
        Vector3 dir = transform.forward;
        if (tempLockedCharacter)
        {
            targetPos.y += tempLockedCharacter.GetCharacterController().height * 0.5f;
            dir = (targetPos - spawnPos).normalized;
        }

        SetCostlyAttackingLine(dir, (targetPos - spawnPos).magnitude, 1);

        Quaternion rotation = Quaternion.LookRotation(dir, Vector3.forward);
        GameObject dartObj = Instantiate(dartPrefab, spawnPos, rotation);
        DartHitbox dartHitbox = dartObj.GetComponentInChildren<DartHitbox>();
        if (dartHitbox != null)
        {
            dartHitbox.InitDart(this, dir, dmg: dartDamage, slamDMG: 0f, forwardVelocity: dartSpeed, rotationalVelocity: 0f, status: dartEffect, attackDuration: dartDuration);
        }

        if (playerControlling)
        {
            CameraController.instance.OnAttack(dir, 0.15f);
        }

        if(aiControlledOnPrimary)
            SetMovementValues(true);

        if (tempLockedCharacter)
        {
            tempLockedCharacter.SetAttacker(null);
            if (tempLockedCharacter.TryGetComponent(out Enemy enemy))
            {
                enemy.SetTargeted(false);
            }
        }

        if (aiControlledOnPrimary)
        {
            if (!hitCharacter) // If missed, vulnerable for half a second
            {
                float timeStart = Time.time;
                while (Time.time - timeStart > 0.1f)
                {
                    SetMovementValues(false);
                    yield return null;
                }
            }
            dryadAnimator.EndPrimary();
        }

        attackingPrimary = false;
        aiControlledOnPrimary = false;
        attackState = AttackState.Neutral;
        pathState = PathState.Unset;
        aiState = AIMovementState.Surrounding;  
        attackStateCoroutine = null;
        lockedCharacter = null;
        SurroundingPoints.instance.RemoveAttackingEnemy(this);
    }

    /// <summary>
    /// Checks if the primary attack is usable
    /// The primary attack is only usable if the player is NOT within the SurroundingRadiusSporeCloud
    /// </summary>
    /// <returns> True if usable, false otherwise </returns>
    public override bool CheckPrimaryUsable()
    {
        if (!CheckPrimaryCooldown()) return false;
        if (attackingPrimary || attackingSecondary || stunned) return false;
        float dist = Vector3.Distance(transform.position, currentPlayer.transform.position);
        float max = currentPlayer.sizeRadius + sizeRadius + maxSurroundingRadiusSporeCloud;
        float min = currentPlayer.sizeRadius + sizeRadius + minSurroundingRadiusSporeCloud;
        if ((!playerControlling) && dist <= (max) && dist >= (min)) return false;
        return true;
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
                // AudioManager.TryPlayInstance("DryadIdle", out idleAudio, true, gameObject);
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
    /// Patrol handling for the dryad
    /// </summary>
    public override void Patrol()
    {
        // Set path if there is none
        if (pathState == PathState.Unset)
        {
            StartCoroutine(FindPath());
        }

        if (LookForPlayer())
        {
            StartCoroutine(SpotPlayer());
            return;
        }

        if (pathState == PathState.Set)
        {
            if (currentPath.ReachedDestination(this)) // If we are within stopping range
            {
                pathState = PathState.Unset;
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
    /// Called in first frame, sets the patrol origin to Dryad position
    /// </summary>
    public void SetPatrolOrigin()
    {
        patrolOrigin = transform.position;
    }

    /// <summary>
    /// Setting a patrol point for the dryad 
    /// By using the patrol origin
    /// </summary>
    public IEnumerator SetPatrollingPoint()
    {
        // Debug.Log("Patrol origin: " + patrolOrigin);

        float randomX = Random.Range(-patrolRange, patrolRange);
        float randomZ = Random.Range(-patrolRange, patrolRange);

        walkPoint = new Vector3(patrolOrigin.x + randomX, patrolOrigin.y, patrolOrigin.z + randomZ);
        Node node = GraphBuilder.instance.FindClosestNode(walkPoint, this);
        if (node == null) yield break;
        walkPoint = node.GetPosition(gameObject);

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
    /// Handles the Dryad's behavior when it sees a player
    /// </summary>
    /// <returns> Waits for animations/sounds </returns>
    private IEnumerator SpotPlayer()
    {
        TransitionToState(AIMovementState.Surrounding);
        if (debugging)
        {
            DestroyPath();
        }

        yield return null;
    }

    /// <summary>
    /// Chase function for the Dryad, it should attack the player when moving
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

        // AttackFromSurrounding(SurroundingPoints.instance); // if we wanted to attack while moving

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
            // if the health is low AND the player is farther away than moveToTargetDistance, then move it toward the player
            if (health.GetHealth() <= lowHealthAggroActive && Vector3.Distance(PlayerController.instance.currentCharacter.transform.position, this.gameObject.transform.position) > moveToTargetDistance)
            {
                AIMove();
            }
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
        TransitionToState(AIMovementState.Surrounding);
        AILook();
    }

    /// <summary>   
    /// Handles Dryad attacking 
    /// It will check if the player is in SurroundingRadiusSporeCloud and if so it will attack the secondary
    /// If the player is not in the SurroundingRadiusSporeCloud, it will attack the primary
    /// </summary>
    public override int AttackFromSurrounding(SurroundingPoints points)
    {
        if (dead || lobotimzed ) return 0;
        float remaining = points.GetAvailableAttackPoints();
        int cost = 0;
        if (CheckPrimaryUsable() && primaryAICost <= remaining)
        {
            StartCoroutine(BeginPrimary());
            cost = primaryAICost;
        }
        else if (CheckSecondaryUsable() && secondaryAICost <= remaining)
        {
            StartCoroutine(BeginSecondary());
            cost = secondaryAICost;
        }
        if (cost == 0) return 0;
        points.AddAttackingEnemy(this, cost);
        return cost;
    }

}
