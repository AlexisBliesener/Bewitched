using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public abstract class Enemy : Character
{
    [Header("Enemy AI Settings")]
    [Tooltip("Navmesh Agent on this character")]
    public NavMeshAgent agent;

    protected PlayerController playerController;

    protected Hag hag;
    protected Character currentPlayer;

    [Tooltip("Masks")]
    public LayerMask ground;
    public LayerMask environment;

    [Tooltip("Sight Range")]
    public float sightRange;

    [Tooltip("Walk Point Range")]
    public float patrolRange;

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

    protected bool walkPointSet = false;
    protected bool playerInSightRange, currentInSightRange, targetInSightRange = false;
    protected bool targetInPrimaryRange = false;

    protected bool playerControlling = false; // flag for determining actions (player or AI)

    private Vector3 walkPoint;

    private Character target;

    private Vector3 lastTargetLocation;

    private bool seenTarget = false;

    private GameObject minibar;

    protected bool isStunned = false;

    private bool inAttackDelay = false;

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
        playerInSightRange = (hag.transform.position - transform.position).magnitude < sightRange;
        float distToHag = (hag.transform.position - transform.position).magnitude;
        if (playerInSightRange)
        {
            if (Physics.Raycast(transform.position, hag.transform.position-transform.position, distToHag, environment))
            {
                playerInSightRange = false;
            }
        }

        currentInSightRange = (currentPlayer.transform.position - transform.position).magnitude < sightRange;
        float distToChar = (currentPlayer.transform.position - transform.position).magnitude;
        if (currentInSightRange)
        {
            if (Physics.Raycast(transform.position, currentPlayer.transform.position - transform.position, distToChar, environment))
            {
                currentInSightRange = false;
            }
        }

        if (playerInSightRange && currentInSightRange)
        {
            targetInSightRange = true;

            if (distToHag < distToChar)
            {
                target = hag;
            }
            else
            {
                target = currentPlayer;
            }
        }
        else if (playerInSightRange)
        {
            target = hag;
            targetInSightRange = true;
        }
        else if (currentInSightRange)
        {
            target = currentPlayer;
            targetInSightRange = true;
        }
        else
        {
            targetInSightRange = false;
            target = hag;
        }

        if ((target.transform.position - transform.position).magnitude - target.sizeRadius < primaryAttackRange)
        {
            targetInPrimaryRange = true;
        }
        else
        {
            targetInPrimaryRange = false;
        }
    }

    public void SetBehavior()
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

    public void Chase()
    {
        if ((target.transform.position - transform.position).magnitude - target.sizeRadius < 1)
        {
            agent.stoppingDistance = target.sizeRadius;
            agent.SetDestination(transform.position);
            AnimateIdle();
        }
        else
        {
            agent.stoppingDistance = target.sizeRadius;
            agent.SetDestination(target.transform.position);
            AnimateMove();
        }
    }

    public void Patrol()
    {
        if(!agent.enabled) return;
        if (!walkPointSet)
        {
            SetWalkPoint();
        }

        if (walkPointSet)
        {
            agent.SetDestination(walkPoint);
            AnimateMove();
        }

        Vector3 distance = transform.position - walkPoint;

        if (distance.magnitude < 1)
        {
            walkPointSet = false;
        }
    }

    public void SetWalkPoint()
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
                return;
            }
        }
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
}
