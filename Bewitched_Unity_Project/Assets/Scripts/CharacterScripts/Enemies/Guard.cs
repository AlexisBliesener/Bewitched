using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Guard : Enemy
{
    [Header("Guard Settings")]
    [Tooltip("Lance Handle Prefab")]
    [SerializeField] GameObject lanceHandlePrefab;
    [Tooltip("Lance Tip Prefab")]
    [SerializeField] GameObject lanceTipPrefab;
    [Tooltip("Thrust Speed")]
    [SerializeField] float thrustSpeed = 10;
    [Tooltip("Lance Handle Damage")]
    [SerializeField] float lanceHandleDamage = 20;
    [Tooltip("Lance Tip Damage")]
    [SerializeField] float lanceTipDamage = 5;
    [Tooltip("Lance Thrust Duration")]
    [SerializeField] float lanceDuration = 0.5f;

    [SerializeField] AttackStatusEffects lanceTipEffects;
    [SerializeField] AttackStatusEffects lanceHandleEffects;

    [Tooltip("Shield Prefab")]
    [SerializeField] GameObject shieldPrefab;

    [Tooltip("Shield Bash Minimum Speed")]
    [SerializeField] float minimumShieldBashSpeed;
    [Tooltip("Shield Bash Maximum Speed")]
    [SerializeField] float maximumShieldBashSpeed;

    [Tooltip("Shield Bash Minimum Damage")]
    [SerializeField] float minimumShieldBashDamage;
    [Tooltip("Shield Bash Maximum Damage")]
    [SerializeField] float maximumShieldBashDamage;

    [Tooltip("Shield Bash Minimum Knockback")]
    [SerializeField] float minimumShieldBashKnockback;
    [Tooltip("Shield Bash Maximum Knockback")]
    [SerializeField] float maximumShieldBashKnockback;

    [Tooltip("Shield Bash Effects")]
    [SerializeField] AttackStatusEffects shieldBashEffects;

    [Tooltip("Charge Time to Max")]
    [SerializeField] float maxShieldBashChargeTime;
    [Tooltip("Shield Bash Duration")]
    [SerializeField] float bashDuration;

    [Tooltip("Movement Speed When Charging")]
    [SerializeField] float chargingMovementSpeed = 2;

    bool chargingShieldBash = false;

    float currentShieldBashSpeed;
    float currentShieldBashDamage;
    float currentShieldBashKnockback;

    float timeStartedBash;

    [Header("Guard AI Settings")]

    [Tooltip("Number of patrol points")]
    [SerializeField] int numPatrolPoints = 3;

    [Tooltip("Sphere prefab representing a patrol point")]
    [SerializeField] GameObject patrolPointPrefab;

    [Tooltip("Patrol points to move through")]
    private List<Vector3> patrolPoints = new List<Vector3>();

    [Tooltip("Editor gameobjects for visually moving points")]
    private List<GameObject> patrolObjs = new List<GameObject>();

    #region Menu Functions

    /// <summary>
    /// Creates the patrol objects either from scratch or from current positions
    /// </summary>
    [ContextMenu("Create Patrol Objects")]
    public void CreatePatrolObjects()
    {
        if (numPatrolPoints > 0)
        {
            if (patrolPoints.Count != numPatrolPoints) // If mismatching number of points create new objects and points
            {
                DeletePatrolObjects();
                patrolPoints = new List<Vector3>();
                for (int i = 0; i < numPatrolPoints; i++)
                {
                    GameObject point = Instantiate(patrolPointPrefab);
                    point.transform.position = new Vector3(transform.position.x + i * 2, transform.position.y + 1, transform.position.z);

                    // Update color of sphere too so that it is a gradient from black towards white
                    patrolObjs.Add(point);
                    patrolPoints.Add(new Vector3(transform.position.x + i * 2, transform.position.y, transform.position.z));
                }
            }
            else if (patrolObjs.Count != patrolPoints.Count) // If object and position counts are mismatched (needs updating)
            {
                DeletePatrolObjects();
                for (int i = 0; i < numPatrolPoints; i++)
                {
                    Debug.Log(patrolPoints[i]);
                    GameObject point = Instantiate(patrolPointPrefab);
                    point.transform.position = new Vector3(patrolPoints[i].x, patrolPoints[i].y + 1, patrolPoints[i].z);
                    Debug.Log(point.transform.position);
                    // Update color of sphere too so that it is a gradient from black towards white
                    patrolObjs.Add(point);
                }
            }
        }
    }

    /// <summary>
    /// Sets the patrol points based on position of objects
    /// </summary>
    [ContextMenu("Set Patrol Points")]
    public void SetPatrolPoints()
    {
        patrolPoints = new List<Vector3>();
        for (int i = 0; i < patrolObjs.Count; i++)
        {
            Vector3 point = patrolObjs[i].transform.position;
            point.y -= 1;
            patrolPoints.Add(point);
        }
    }

    /// <summary>
    /// Deletes all patrol objects in the scene
    /// </summary>
    [ContextMenu("Delete Patrol Objects")]
    public void DeletePatrolObjects()
    {
        foreach (GameObject obj in patrolObjs)
        {
            DestroyImmediate(obj);
        }
        patrolObjs = new List<GameObject>();
    }

    /// <summary>
    /// Removes all the patrol point positions
    /// </summary>
    [ContextMenu("Destroy all patrol points")]
    public void RemoveAllPoints()
    {
        DeletePatrolObjects();
        patrolPoints = new List<Vector3>();
    }

    #endregion

    // Start is called before the first frame update
    void Start()
    {
        SetPlayerInfo();
        health.SetHealthToMax();
        SetBaseStats();
    }

    // Update is called once per frame
    void Update()
    {
        currentPlayer = playerController.GetCurrentCharacter();
        HandleHitStun();
        if (!playerControlling)
        {
            SetRangeChecks();
            SetBehavior();
            agent.speed = movementSpeed;
        }
        else
        {
            ChargeShieldBash();
        }
    }

    public override void PrimaryAttack()
    {
        GameObject lanceHandle = Instantiate(lanceHandlePrefab, transform);
        lanceHandle.GetComponent<DefaultHitbox>().Init(this, dmg: lanceHandleDamage, forwardVelocity: thrustSpeed, status: lanceHandleEffects, attackDuration: lanceDuration);

        GameObject lanceTip = Instantiate(lanceTipPrefab, transform);
        lanceTip.GetComponent<DefaultHitbox>().Init(this, dmg: lanceTipDamage, status: lanceTipEffects, attackDuration: lanceDuration);
        lanceHandle.GetComponent<DefaultHitbox>().AttachHitbox(lanceTip.GetComponent<DefaultHitbox>());

        timeLastPrimary = Time.time;
        attackingPrimary = true;
    }

    public override void SecondaryAttack()
    {
        chargingShieldBash = true;
        currentShieldBashDamage = minimumShieldBashDamage;
        currentShieldBashKnockback = minimumShieldBashKnockback;
        currentShieldBashSpeed = minimumShieldBashSpeed;

        baseMovementSpeed = movementSpeed;
        movementSpeed = chargingMovementSpeed;
        timeStartedBash = Time.time;
        attackingSecondary = true;

        //if (releaseSecondaryImm) ReleaseSecondary();
        //releaseSecondaryImm = false;
    }

    public void ChargeShieldBash()
    {
        if (chargingShieldBash)
        {
            float timeVal = (Time.time - timeStartedBash) / maxShieldBashChargeTime;

            if (timeVal < 1) // If charging for more than maximum time do nothing
            {
                currentShieldBashDamage = Mathf.Lerp(minimumShieldBashDamage, maximumShieldBashDamage, timeVal);
                currentShieldBashKnockback = Mathf.Lerp(minimumShieldBashKnockback, maximumShieldBashKnockback, timeVal);
                currentShieldBashSpeed = Mathf.Lerp(minimumShieldBashSpeed, maximumShieldBashSpeed, timeVal);
            }
        }
    }

    //public override void ReleaseSecondary()
    //{
    //    base.ReleaseSecondary();
    //    if (!chargingShieldBash) return;

    //    chargingShieldBash = false;
    //    timeLastSecondary = Time.time;
    //    playerController.SetAllowMovement(false);

    //    health.SetInvincible(true);

    //    GameObject hitbox = Instantiate(shieldPrefab, transform);
    //    hitbox.GetComponent<DefaultHitbox>().Init(this, dmg: currentShieldBashDamage, status: shieldBashEffects, attackDuration: bashDuration);
    //    StartCoroutine(HandleBashMovement(hitbox));
    //}

    private IEnumerator HandleBashMovement(GameObject hitbox)
    {
        float timeSinceStarted = 0f;

        while (timeSinceStarted < bashDuration)
        {
            if (hitbox.GetComponent<DefaultHitbox>().HasHitWall())
            {
                StartCoroutine(EnableMovement());
                health.SetInvincible(false);
                movementSpeed = baseMovementSpeed;
                attackingSecondary = false;

                transform.position = transform.position - transform.forward.normalized * currentShieldBashSpeed * Time.deltaTime;

                yield break;
            }

            transform.position = transform.position + transform.forward.normalized * currentShieldBashSpeed * Time.deltaTime;
            timeSinceStarted += Time.deltaTime;
            yield return null;
        }

        transform.position = transform.position + transform.forward.normalized * currentShieldBashSpeed * Time.deltaTime;

        Destroy(hitbox);

        StartCoroutine(EnableMovement());
        health.SetInvincible(false);
        movementSpeed = baseMovementSpeed;
        attackingSecondary = false;
    }

    public override Vector3 GetCurrentSpeedVector()
    {
        return currentShieldBashSpeed * transform.forward.normalized;
    }

    public override bool CheckSecondaryUsable()
    {
        if (chargingShieldBash) return false;
        return base.CheckSecondaryUsable();
    }
}
