using DG.Tweening;
using FMOD.Studio;
using FMODUnity;
using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;


public class Goblin : Enemy
{
    [Header("References/Prefabs"), ShowIf("dev")]
    [Tooltip("Knife Prefab")]
    [SerializeField] GameObject knifePrefab;
    [Tooltip("Dash Hitbox"), ShowIf("dev")]
    [SerializeField] GameObject dashHitbox;
    [Tooltip("Dash Effects"), ShowIf("dev")]
    [SerializeField] AttackStatusEffects dashEffects;
    [Tooltip("Spin Hitbox"), ShowIf("dev")]
    [SerializeField] GameObject spinHitbox;
    [Tooltip("Spin Effects"), ShowIf("dev")]
    [SerializeField] AttackStatusEffects spinEffects;
    [Tooltip("Knife Effects"), ShowIf("dev")]
    [SerializeField] AttackStatusEffects[] knifeEffects;

    [Header("Knife Settings for Goblin")]
    [Tooltip("Knife duration")]
    [SerializeField] float knifeDuration = 0.25f;
    [Tooltip("Thrust Speed")]
    [SerializeField] float[] thrustSpeed = { 10 };
    [Tooltip("Knife Damage")]
    [SerializeField] float[] knifeDamage = { 20 };

    [Header("Dash Settings for Goblin")]
    [Tooltip("Dash Speed"), Range(0, 100)]
    [SerializeField] float dashSpeed = 50;
    [Tooltip("Dash Duration"), Range(0, 10)]
    [SerializeField] float dashDuration = 0.5f;
    [Tooltip("Dash Damage"), Range(0, 200)]
    [SerializeField] float dashDamage = 30;
    [Tooltip("Offset of the hitbox forward"), Range(0, 10)]
    [SerializeField] private float offSetForward = 0.5f;

    [Header("Spin Settings for Goblin")]
    [Tooltip("Spin Damage"), Range(0, 200)]
    [SerializeField] float spinDamage = 30;
    [Tooltip("Distance to dodge in first part of spin"), Range(0, 10)]
    [SerializeField] float spinDodgeDistance = 2;
    [Tooltip("Input time after dodging for the spin dodge"), Range(0, 10)]
    [SerializeField] float spinDodgeInputTime = .75f;
    [Tooltip("Distance for first spin jump"), Range(0, 10)]
    [SerializeField] float spinDistance = 8;
    [Tooltip("Distance dropoff per bounce"), Range(0, 10)]
    [SerializeField] float spinDistanceDropoff = 2.5f;
    [Tooltip("Spin Duration"), Range(0, 50)]
    [SerializeField] float spinDuration = 10;
    [Tooltip("Spin Speed"), Range(0, 100)]
    [SerializeField] float spinSpeed = 15;
    [Tooltip("Spin Rotational Speed"), Range(0, 360)]
    [SerializeField] float spinRotationalSpeed = 120;
    [Tooltip("Standard Acceleration Period"), Range(0, 10)]
    [SerializeField] float standardAccelerationPeriod = 0.5f;
    [Tooltip("Low Health Acceleration Period"), Range(0, 10)]
    [SerializeField] float lowHealthAccelerationPeriod = 0.25f;
    [Tooltip("Low Health Angle Variation Range"), Range(0, 360)]
    [SerializeField] float lowHealthAngleRange = 80; // maximum 40 degree change
    [Tooltip("Maximum drift speed"), Range(0, 10)]
    [SerializeField] float maxDriftSpeed = 4;
    [Tooltip("The max angular distance a deflect will auto-target the player on wall/character spin collisions"), Range(0, 360)]
    [SerializeField] float maxSpinDeflectAngle = 30;
    [Tooltip("Minimum time delay before spin from AI")]
    [SerializeField] float minSpinDelay = 0.3f;
    [Tooltip("Maximum time delay before spin from AI")]
    [SerializeField] float maxSpinDelay = 0.7f;

    [Header("Goblin AI Settings")]
    [Tooltip("Minimum Patrol Distance"), Range(0, 100)]
    [SerializeField] float minPatrolDistance = 3;
    [Tooltip("Maximum Patrol Distance"), Range(0, 100)]
    [SerializeField] float maxPatrolDistance = 5;
    [Tooltip("Range the Goblin can communicate with other Goblins"), Range(0, 100)]
    [SerializeField] float communicationRange = 8;

    [Tooltip("Goblin animator script that controls the goblin animations")]
    private GoblinAnimator goblinAnimator;
    [Tooltip("The position the goblin will try to move to on attack")]
    private Vector3 targetPos = Vector3.negativeInfinity;
    [Tooltip("Is this goblin is currently in the windup animation")]
    private bool inPrimaryWindup = false;



    private int numDeflections = 0;

    private void Start()
    {
        goblinAnimator = GetComponentInChildren<GoblinAnimator>();
        primaryComboSteps = 3;
        SetPlayerInfo();
        health.SetHealthToMax();
        SetBaseStats();
        SetDebuggingValues();
        SetPatrolOrigin();
        sizeRadius = GetComponent<CharacterController>().radius;
    }

    protected override void FixedUpdate()
    {
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

        SetDebugString();

        SetAIState();

        SetBehavior();

        base.FixedUpdate();
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
                if (!inPrimaryWindup && (currentPrimaryComboStep == -1 || Time.time - timeLastPrimary >= primaryComboMinTime[currentPrimaryComboStep] / goblinAnimator.GetPrimaryComboMult(currentPrimaryComboStep)))
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
                currentPrimaryComboStep = -1;
                timeLastPrimary = Time.time;
                characterAnimator.SwitchState("PrimaryAttack", 0);
                yield return StartCoroutine(characterAnimator.WaitForDelay("PrimaryAttack", 0));
                PrimaryAttack();
            }
        }
    }

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

        Character tempLockedChar = lockedCharacter;

        attackingPrimary = true;

        if (playerControlling)
        {
            if (tempLockedChar != null && Vector3.Distance(tempLockedChar.transform.position, this.gameObject.transform.position) > moveToTargetDistance)
            {
                inPrimaryWindup = true;
                attackStateCoroutine = StartCoroutine(KnifeWindup(tempLockedChar));
            }
            else
            {
                attackStateCoroutine = StartCoroutine(HandleStab(tempLockedChar));
            }
        }
        else
        {
            inPrimaryWindup = true;
            attackStateCoroutine = StartCoroutine(KnifeWindup(tempLockedChar));
        }
    }

    /// <summary>
    /// Starts the windup for the knife
    /// </summary>
    /// <returns> Time </returns>
    public IEnumerator KnifeWindup(Character tempLockedCharacter)
    {
        inCounter = false;
        attackState = AttackState.Windup;
        // save the current position to use the y value later
        targetPos = transform.position;
        float windupStart = Time.time;

        while (Time.time  - windupStart < 0.708 / goblinAnimator.GetPrimaryWindupMult())
        {
            SetMovementValues(false);
            if (tempLockedCharacter)
            {
                Vector3 direc = tempLockedCharacter.transform.position - transform.position;
                direc.y = 0;
                Quaternion rotationVal = Quaternion.LookRotation(direc.normalized);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, rotationVal, rotationalVelocity);
            }

            yield return null;
        }

        if (playerControlling) // Since the player should only be controlling here if possessed at this point, reset target if player controlled
        {
            tempLockedCharacter = PlayerController.instance.GetLockedTarget();
        }

        attackStateCoroutine = StartCoroutine(KnifeApproach(tempLockedCharacter));
    }

    /// <summary>
    /// Approach function for stabbing
    /// </summary>
    /// <returns> Time </returns>
    public IEnumerator KnifeApproach(Character tempLockedCharacter)
    {
        attackState = AttackState.Approaching;
        if (tempLockedCharacter)
        {
            float dis = Vector3.Distance(tempLockedCharacter.transform.position, this.gameObject.transform.position);
            Vector3 direction = (tempLockedCharacter.transform.position - transform.position).normalized;
            float oldY = targetPos.y;
            targetPos = tempLockedCharacter.transform.position - direction * (GetCharacterController().radius + tempLockedCharacter.GetCharacterController().radius + offSetForward);
            RaycastHit hit;
            // Raycast to check for environment collision
            if (Physics.Raycast(transform.position, direction, out hit, dis, environment | characters))
            {
                // Move just before environment/character hit point
                dis = hit.distance;
                targetPos = hit.point - direction * (sizeRadius + offSetForward);
            }
            targetPos.y = oldY;
            transform.DOMove(targetPos, chaseTime * dis);
            transform.DOLookAt(targetPos, chaseTime * dis);

            float timeStarted = Time.time;
            timeLastPrimary = Time.time + chaseTime * dis * counterWindowLength;
            bool triggerSet = false;

            if (playerControlling)
            {
                if (tempLockedCharacter != null)
                {
                    CameraController.instance.OnAttack(tempLockedCharacter.transform.position - this.gameObject.transform.position, chaseTime * dis);
                }
                else
                {
                    CameraController.instance.OnAttack(this.gameObject.transform.forward, chaseTime * dis);
                }
            }

            inPrimaryWindup = false;
            while (Time.time - timeStarted < chaseTime * dis)
            {
                if (tempLockedCharacter == null || Vector3.Distance(transform.position, tempLockedCharacter.transform.position) < sizeRadius + offSetForward)
                {
                    DOTween.Kill(gameObject); // Kill tweens if we are too close
                    goblinAnimator.ExitLeap();
                }
                else if(tempLockedCharacter == null)
                {
                    goblinAnimator.ExitLeap();
                }

                if (Time.time - timeStarted >= counterWindowLength * chaseTime * dis) //  not dodgable
                {
                    if (!triggerSet)
                    {
                        goblinAnimator.ExitLeap();
                        triggerSet = true;
                    }

                    if (counterIndicatorVFX != null)
                    {
                        DestroyCounterIndicator();
                        if (PlayerController.instance.GetCounterAvailable() == this) PlayerController.instance.SetCounterAvaliable(null);
                    }
                }
                else // attack is dodgable
                {
                    if (counterIndicatorVFX == null)
                    {
                        counterIndicatorVFX = Instantiate(counterIndicatorVFXPrefab, transform);
                        counterIndicatorVFX.transform.localPosition = new Vector3(0, 2.5f, 0);
                        PlayerController.instance.SetCounterAvaliable(this);
                    }
                }
                SetMovementValues(false);
                GetCharacterController().enabled = false;
                yield return null;
            }

            if(!triggerSet)
            {
                goblinAnimator.ExitLeap();
            }
            transform.position = targetPos;
            GetCharacterController().enabled = true;
        }

        if (counterIndicatorVFX != null)
        {
            DestroyCounterIndicator();
        }

        attackState = AttackState.Attacking;

        Vector3 offsetPosition = transform.position + transform.forward * offSetForward;
        GameObject knifeHitbox = Instantiate(knifePrefab, offsetPosition, transform.rotation);
        knifeHitbox.GetComponent<DefaultHitbox>().Init(this, dmg: knifeDamage[currentPrimaryComboStep == -1 ? 0 : currentPrimaryComboStep], forwardVelocity: thrustSpeed[currentPrimaryComboStep == -1 ? 0 : currentPrimaryComboStep], status: knifeEffects[currentPrimaryComboStep == -1 ? 0 : currentPrimaryComboStep], attackDuration: knifeDuration);

        targetPos = Vector3.negativeInfinity;

        float hitboxStartTime = Time.time;
        while (Time.time - hitboxStartTime < 0.25f / goblinAnimator.GetPrimaryComboMult(currentPrimaryComboStep == -1 ? 0 : currentPrimaryComboStep))
        {
            SetMovementValues(false);
            yield return null;
        }

        if (!playerControlling)
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
            goblinAnimator.EndPrimary();
        }

        SetMovementValues(true);

        attackState = AttackState.Neutral;
        pathState = PathState.Unset;
        aiState = AIMovementState.Retreating;
        

        if (tempLockedCharacter)
        {
            tempLockedCharacter.SetAttacker(null);
            if (tempLockedCharacter.TryGetComponent(out Enemy enemy))
            {
                enemy.SetTargeted(false);
            }
        }

        lockedCharacter = null;
        attackingPrimary = false;
        SurroundingPoints.instance.RemoveAttackingEnemy(this);

        yield break;
    }

    /// <summary>
    /// Coroutine handling the AI state changes, AI delay, and locking movement for the player when stabbing
    /// </summary>
    /// <returns> Time breaks </returns>
    public IEnumerator HandleStab(Character tempLockedCharacter)
    {
        goblinAnimator.SetPrimaryMovementNeeded(false);
        attackState = AttackState.Attacking;

        Vector3 offsetPosition = transform.position + transform.forward * offSetForward;
        GameObject knifeHitbox = Instantiate(knifePrefab, offsetPosition, transform.rotation);
        knifeHitbox.GetComponent<DefaultHitbox>().Init(this, dmg: knifeDamage[currentPrimaryComboStep == -1 ? 0 : currentPrimaryComboStep], forwardVelocity: thrustSpeed[currentPrimaryComboStep == -1 ? 0 : currentPrimaryComboStep], status: knifeEffects[currentPrimaryComboStep == -1 ? 0 : currentPrimaryComboStep], attackDuration: knifeDuration);

        if (playerControlling)
        {
           CameraController.instance.OnAttack(this.gameObject.transform.forward, 0.01f);
        }

        float hitboxStartTime = Time.time;
        while (Time.time - hitboxStartTime < 0.25f / goblinAnimator.GetPrimaryComboMult(currentPrimaryComboStep == -1 ? 0 : currentPrimaryComboStep))
        {
            SetMovementValues(false);
            yield return null;
        }

        if (!playerControlling)
        {
            if (!hitCharacter) // If missed, vulnerable for half a second
            {
                yield return new WaitForSeconds(0.5f);
            }
        }

        SetMovementValues(true);

        attackState = AttackState.Neutral;
        pathState = PathState.Unset;
        aiState = AIMovementState.Retreating;

        if (tempLockedCharacter)
        {
            tempLockedCharacter.SetAttacker(null);
            if (tempLockedCharacter.TryGetComponent(out Enemy enemy))
            {
                enemy.SetTargeted(false);
            }
        }

        if(!playerControlling)
        {
            goblinAnimator.EndPrimary();
        }

        tempLockedCharacter = null;
        attackingPrimary = false;
        SurroundingPoints.instance.RemoveAttackingEnemy(this);
    }

    /// <summary>
    /// Starts the secondary attack
    /// </summary>
    public override void SecondaryAttack()
    {
        hitCharacter = false;
        if (playerControlling)
        {
            lockedCharacter = PlayerController.instance.GetLockedTarget();
        }
        else
        {
            lockedCharacter = currentPlayer;
        }

        Character tempLockedCharacter = lockedCharacter;

        if (tempLockedCharacter)
        {
            tempLockedCharacter.SetAttacker(this);
            if (tempLockedCharacter.TryGetComponent(out Enemy enemy))
            {
                enemy.SetTargeted(true);
            }
        }

        SetMovementValues(false);

        attackStateCoroutine = StartCoroutine(SpinWindup(tempLockedCharacter));
    }

    /// <summary>
    /// Handles the windup for the spin
    /// </summary>
    /// <returns> Time </returns>
    public IEnumerator SpinWindup(Character tempLockedCharacter)
    {
        inCounter = false;
        attackingSecondary = true;
        attackState = AttackState.Windup;

        float timeStarted = Time.time;

        if (!playerControlling)
        {
            counterIndicatorVFX = Instantiate(counterIndicatorVFXPrefab, transform);
            counterIndicatorVFX.transform.localPosition = new Vector3(0, 2.5f, 0);
        }

        if (tempLockedCharacter)
        {
            tempLockedCharacter.SetAttacker(this);
            if (tempLockedCharacter.TryGetComponent(out Enemy enemy))
            {
                enemy.SetTargeted(true);
            }
        }

        // For now wait 0.5 seconds, in future wait for animation trigger
        while (Time.time - timeStarted < 0.125f / goblinAnimator.GetSecondaryWindupMult())
        {
            SetMovementValues(false);
            if (tempLockedCharacter)
            {
                Vector3 direc = tempLockedCharacter.transform.position - transform.position;
                direc.y = 0;
                Quaternion rotationVal = Quaternion.LookRotation(direc.normalized);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, rotationVal, rotationalVelocity);
            }

            if (counterIndicatorVFX != null) DestroyCounterIndicator(); // Destroy attack indicator if possessed
            yield return null;
        }
        numDeflections = 0;
        attackStateCoroutine = StartCoroutine(HandleSpin(spinDistance, spinRotationalSpeed, tempLockedCharacter));
    }

    /// <summary>
    /// Handles the spinning attack itself
    /// </summary>
    /// <param name="distance"> Distance this spin can travel </param>
    /// <param name="desiredRotation"> Rotation to reach for goblin spin </param>
    /// <param name="newDirection"> Velocity to move at, zero by default if unset </param>
    /// <returns> Time </returns>
    public IEnumerator HandleSpin(float distance, float desiredRotation, Character tempLockedCharacter, Vector3 direction = default)
    {
        attackState = AttackState.Attacking;
        if (distance < 0.5f)
        {
            velocity = Vector3.zero; // Clamping velocity
            rotationalVelocity = 0;

            attackingSecondary = false;
            timeLastSecondary = Time.time;

            SetMovementValues(true);

            attackStateCoroutine = null;
            goblinAnimator.SetSecondaryAttackEnded();
            yield break;
        }

        SetMovementValues(false);

        float rotationalSpeed = 0;
        Vector3 desiredVelocity;

        bool slowTime = false;

        GameObject hitbox = Instantiate(spinHitbox, transform);
        hitbox.GetComponent<DeflectingHitbox>().Init(this, dmg: spinDamage, status: spinEffects, attackDuration: spinDuration);

        if (direction == Vector3.zero) // If first use, set desiredVelocity alone and have AI pause
        {
            float delayTimeStarted = Time.time;
            while (!playerControlling && Time.time - delayTimeStarted < attackDelayAI)
            {
                SetMovementValues(false);
                yield return null;
            }

            if (playerControlling) // Sets the target correctly at the moment before the spin
            {
                Enemy target = PlayerController.instance.GetLockedTarget();
                tempLockedCharacter = target;
            }

            if (tempLockedCharacter)
            {
                desiredVelocity = (tempLockedCharacter.transform.position - transform.position).normalized;
            }
            else
            {
                desiredVelocity = transform.forward.normalized;
            }
        }
        else // If deflecting slow time for a period to give reaction time
        {
            slowTime = true;
            desiredVelocity = direction;
        }

        desiredVelocity.y = 0;
        desiredVelocity = desiredVelocity.normalized * spinSpeed;

        if (playerControlling)
        {
            CameraController.instance.OnAttack(desiredVelocity, CameraController.instance.GetGoblinSecondaryRotateTime());
        }

        Vector3 drift;

        float accelerationTime;
        // Handle low health AI behavior here in the future, (apply random rotational offset depending on health)
        if (!IsLowHealth() || playerControlling)
        {
            accelerationTime = standardAccelerationPeriod;
            drift = Vector3.zero;
        }
        else
        {
            accelerationTime = lowHealthAccelerationPeriod;
            drift = Quaternion.AngleAxis(Random.Range(-lowHealthAngleRange / 2, lowHealthAngleRange / 2), Vector3.up) * velocityToMove.normalized * maxDriftSpeed;
        }

        float timeStarted;
        if (slowTime)
        {
            timeStarted = Time.time;
        }
        else
        {
            timeStarted = 0;
        }

        float timeSinceBegan = 0;

        float distanceTravelled = 0;
        bool cameraRotationStopped = false;
        while (distanceTravelled < distance)
        {
            if (slowTime && Time.time - timeStarted < 0.05f)
            {
                Time.timeScale = 0.5f;
                yield return null;
            }
            else if (Time.timeScale == 0.5f)
            {
                Time.timeScale = 1;
            }

            timeSinceBegan += Time.deltaTime;
            SetMovementValues(false); // Helps if player possesses enemy mid-attack

            if (velocity.magnitude < spinSpeed) // If still accelerating
            {
                velocity = Vector3.Lerp(velocity, desiredVelocity, timeSinceBegan / accelerationTime);
                rotationalVelocity = Mathf.Lerp(rotationalVelocity, desiredRotation, timeSinceBegan / accelerationTime);
            }

            float currentMagnitude = velocity.magnitude; // Set magnitude before adding drift

            velocity = velocity += drift * Time.deltaTime; // Add drift to velocity

            velocity = velocity.normalized * currentMagnitude; // Set to same magnitude as before, drift does not allow for faster speeds, only misdirection

            if (Mathf.Abs(rotationalSpeed) > Mathf.Abs(desiredRotation)) // Correct rotational speed
            {
                rotationalVelocity = spinRotationalSpeed;
            }

            // Check for impact before moving and adjust as needed
            RaycastHit hit;
            if (Physics.Raycast(transform.position, velocity.normalized, out hit, velocity.magnitude * Time.deltaTime, environment))
            {
                if(!cameraRotationStopped)
                {
                    cameraRotationStopped = true;
                    CameraController.instance.StopRotations();
                }
                float distRatio = Vector3.Distance(transform.position, hit.point) / (velocity.magnitude * Time.deltaTime);
                GetCharacterController().Move(velocity * Time.deltaTime * distRatio);
            }
            else if (Physics.Raycast(transform.position, velocity.normalized, out hit, velocity.magnitude * Time.deltaTime, characters))
            {
                if (!cameraRotationStopped)
                {
                    cameraRotationStopped = true;
                    CameraController.instance.StopRotations();
                }
                float distRatio = Vector3.Distance(transform.position, hit.point) / (velocity.magnitude * Time.deltaTime);
                GetCharacterController().Move(velocity * Time.deltaTime * distRatio);
            }
            else
            {
                GetCharacterController().Move(velocity * Time.deltaTime);
            }
            transform.Rotate(Vector3.up, rotationalVelocity * Time.deltaTime);

            distanceTravelled += velocity.magnitude * Time.deltaTime;

            yield return null;
        }

        // If reached this point (no deflects) slow down, destroy hitbox halfway through, and end
        float timeSinceSlowBegan = 0;
        Destroy(hitbox);

        while (timeSinceSlowBegan < 0.5f)
        {
            velocity = Vector3.Lerp(velocity, Vector3.zero, timeSinceSlowBegan / 0.5f);
            rotationalVelocity = Mathf.Lerp(rotationalVelocity, 0, timeSinceSlowBegan / 0.5f);

            GetCharacterController().Move(velocity * Time.deltaTime);
            transform.Rotate(Vector3.up, rotationalVelocity * Time.deltaTime);

            timeSinceSlowBegan += Time.deltaTime;

            yield return null;
        }

        if (tempLockedCharacter)
        {
            tempLockedCharacter.SetAttacker(null);
            if (tempLockedCharacter.TryGetComponent(out Enemy enemy))
            {
                enemy.SetTargeted(false);
            }
            transform.Rotate(Vector3.up, rotationalSpeed * Time.deltaTime);

            yield return null;
        }

        velocity = Vector3.zero; // Clamping velocity
        rotationalVelocity = 0;

        // end spin portion of the secondary attack animation, move into stagger portion
        goblinAnimator.SetSecondaryAttackEnded();

        while (goblinAnimator.GetCurrentState() == "SecondaryAttack") // While still in the secondary animation state
        {
            SetMovementValues(false);
            yield return null;
        }

        attackingSecondary = false;
        timeLastSecondary = Time.time;

        SetMovementValues(true);
        attackState = AttackState.Neutral;
        attackStateCoroutine = null;
        SurroundingPoints.instance.RemoveAttackingEnemy(this);
    }


    /// <summary>
    /// Function that follows the flow chart to set behavior for the goblin
    /// </summary>
    public override void SetBehavior()
    {
        target = playerController.currentCharacter; // Always update this
        if (playerControlling || inProcess) return;

        if (aiState == AIMovementState.Patrolling) // If patrolling
        {
            // Set path if there is none
            if (pathState == PathState.Unset)
            {
                StartCoroutine( FindPath() );
            }
            Patrol();
        }
        else if (aiState == AIMovementState.Chasing)
        {
            // Set path if there is none
            if (pathState == PathState.Unset)
            {
                StartCoroutine( FindPath() );
            }
            Chase();
        }
        else if (aiState == AIMovementState.Surrounding)
        {
            // Set path if there is none
            if (pathState == PathState.Unset)
            {
                StartCoroutine(FindPath());
            }
            Surround();
        }
        else if (aiState == AIMovementState.Retreating)
        {
            if (pathState == PathState.Unset)
            {
                StartCoroutine(FindPath());
            }
            Retreat();
        }
    }

    /// <summary>
    /// Handles finding a path in the graph based on the state
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
            yield return StartCoroutine(SurroundingPoints.instance.FindPathToRetreat(this));
        }
    }

    /// <summary>
    /// Override function handling patrol functionality for the Goblin
    /// This patrol method sets a point before the first frame and the goblin will patrol
    /// randomly within a circle of that point
    /// </summary>
    public override void Patrol()
    {
        if (!idleAudio.isValid()) AudioManager.TryPlayInstance("GoblinIdle", out idleAudio, true, gameObject);

        AIMove();
        AILook();

        if (pathState == PathState.Set)
        {
            if (currentPath.ReachedDestination(this)) // If we are within stopping range
            {
                pathState = PathState.Unset;
                StartCoroutine(LookAround()); // Look around
            }

            if (debugging)
            {
                UpdatePath();
            }
        }
        else // If no current path, mark as available
        {
            reachedWalkpoint = false;
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
    public IEnumerator SetPatrollingPoint()
    {
        float randomX = Random.Range(-patrolRange, patrolRange);
        float randomZ = Random.Range(-patrolRange, patrolRange);

        walkPoint = new Vector3(patrolOrigin.x + randomX, patrolOrigin.y, patrolOrigin.z + randomZ);
        Node node = GraphBuilder.instance.FindClosestNode(walkPoint, this);
        if (node == null) yield break;
        walkPoint = node.GetPosition(gameObject);

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

        if (aiState == AIMovementState.Patrolling) // If a valid patrol point
        {
            float distance = currentPath.GetDistance();

            if ((distance >= minPatrolDistance && distance <= maxPatrolDistance) || Vector3.Distance(transform.position, patrolOrigin) >= patrolRange)
            {
                if (debugging)
                {
                    StartPath();
                }

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
                TransitionToState(AIMovementState.Chasing);
                inProcess = false;
                yield break;
            }
            timer += Time.deltaTime;
            yield return null;
        }

        inProcess = false;
        if (debugging)
        {
            StartPath();
        }
    }

    /// <summary>
    /// Chase function for the Goblin - should set paths that focus on surrounding the player
    /// </summary>
    public override void Chase()
    {
        StopIdleAudio();
        lookAtPlayer = false;

        if (pathState == PathState.Set || currentPath != null)
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
        StopIdleAudio();
        lookAtPlayer = true;

        if (pathState == PathState.Set || currentPath != null)
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
    /// Retreat from close distance, get back to surrounding
    /// </summary>
    public void Retreat()
    {
        lookAtPlayer = true;
        if (pathState == PathState.Set || currentPath != null)
        {
            // Debug.Log("Moving: " + gameObject);
            AIMove();
            if (debugging)
            {
                UpdatePath();
            }
        }

        pathState = PathState.Unset;

        AILook();

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
    /// Gets the priority of a goblin to be added for attacking
    /// </summary>
    /// <returns> Enemy priority </returns>
    public override int GetAttackingPriority()
    {
        int val = base.GetAttackingPriority();
        if (IsLowHealth()) val += 2;

        return val;
    }

    /// <summary>
    /// Deflects the velocity depending on the playercontrolling status, target direction, and collision type
    /// </summary>
    /// <param name="other"> Other collider in collision </param>
    public void DeflectVelocity(Collider other, DeflectingHitbox caller)
    {
        numDeflections++;
        Vector3 deflectDirection;

        Vector3 closestPoint = other.ClosestPoint(transform.position);
        Vector3 contactDirection = (transform.position - closestPoint).normalized;

        deflectDirection = Vector3.Reflect(velocity.normalized, contactDirection);

        int rotationMultiplier = 1;
        if (numDeflections % 2 != 0)
        {
            rotationMultiplier = -1;
        }
        if (attackStateCoroutine != null) // If coroutine has ended, end this
        {
            StopCoroutine(attackStateCoroutine);
            attackStateCoroutine = StartCoroutine(HandleSpin(spinDistance - spinDistanceDropoff * numDeflections, spinRotationalSpeed * rotationMultiplier, lockedCharacter, deflectDirection));
            rotationalVelocity = -rotationalVelocity / 2; // Reverse rotational speed and halve it
            Destroy(caller.gameObject);
        }
        velocity = Vector3.zero; // Zero out velocity to instantly change directions
        Destroy(caller.gameObject);
    }

    /// <summary>
    /// Handles Goblin attacking chance and triggering
    /// </summary>
    /// <param name="points"> The points calling this function </param>
    /// <returns> Cost of attack done </returns>
    public override int AttackFromSurrounding(SurroundingPoints points)
    {
        if (dead || lobotimzed) return 0;
        float totalOdds = 0;
        List<Goblin> goblins = points.GetEnemiesSameType(this); // List of other goblins that are able to attack
        float remaining = points.GetAvailableAttackPoints();
        bool primaryAvailable = false;

        if (CheckPrimaryUsable() && primaryAICost <= remaining)
        {
            totalOdds += primaryAttackChance;
            primaryAvailable = true;
        }
        if (CheckSecondaryUsable() && secondaryAICost <= remaining)
        {
            if (goblins.Count >= 1) // Only do this if other goblins are around
            {
                totalOdds += secondaryAttackChance;
            }
        }

        if (totalOdds > 0) // Attack happens in here
        {
            int cost;
            float choice = Random.Range(0, totalOdds);
            if (choice <= primaryAttackChance && primaryAvailable) // Primary attack selected
            {
                StartCoroutine(BeginPrimary());
                cost = primaryAICost;
            }
            else
            {
                StartCoroutine(BeginSecondary());
                // Plan other goblin attack here and add to cost ahead of time
                int goblinAttackCount = (int)Random.Range(0, Mathf.Max((remaining - secondaryAICost) / secondaryAICost, goblins.Count)); // Gets a random number of available goblins
                Debug.Log("Attacking with: " + goblinAttackCount + " others");
                for (int i = 0; i < goblinAttackCount; i++)
                {
                    StartCoroutine(goblins[i].SpinWithDelay());
                    points.AddAttackingEnemy(goblins[i], secondaryAICost);
                }
                cost = secondaryAICost;
            }
            points.AddAttackingEnemy(this, cost);
            return cost;
        }
        else
        {
            return 0;
        }
    }

    /// <summary>
    /// Set possessed to be true/false
    /// </summary>
    /// <param name="val"> Value to set </param>
    public override void SetControlled(bool val)
    {
        base.SetControlled(val);
    }

    /// <summary>
    /// Begins the spin attack after a short delay
    /// </summary>
    /// <returns> Time </returns>
    public IEnumerator SpinWithDelay()
    {
        float waitDelay = Random.Range(minSpinDelay, maxSpinDelay);
        float timeStarted = Time.time;
        attackState = AttackState.Windup;
        while (Time.time - timeStarted < waitDelay)
        {
            yield return null;
        }
        StartCoroutine(BeginSecondary());
    }
}