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

    private Dictionary<string, float> lanceHandleEffects = new Dictionary<string, float>()
    {
        { "knockback", 5 }
    };

    private Dictionary<string, float> lanceTipEffects = new Dictionary<string, float>()
    {
        { "knockback", 5 }
    };

    [SerializeField] AttackStatusEffects lanceTipEffects2;
    [SerializeField] AttackStatusEffects lanceHandleEffects2;

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
    [SerializeField]
    Dictionary<string, float> shieldBashEffects = new Dictionary<string, float>()
    {
        { "knockback", 5 },
        { "timeStop", .15f}
    };

    [SerializeField] AttackStatusEffects shieldBashEffects2;

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

    // Start is called before the first frame update
    void Start()
    {
        SetPlayerInfo();
        SetHealthToMax();
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
        base.PrimaryAttack();

        GameObject lanceHandle = Instantiate(lanceHandlePrefab, transform);
        lanceHandle.GetComponent<DefaultHitbox>().Init(this, dmg: lanceHandleDamage, forwardVelocity: thrustSpeed, status: lanceHandleEffects2);

        GameObject lanceTip = Instantiate(lanceTipPrefab, transform);
        lanceTip.GetComponent<DefaultHitbox>().Init(this, dmg: lanceTipDamage, forwardVelocity: thrustSpeed, status: lanceTipEffects2);
        lanceHandle.GetComponent<DefaultHitbox>().AttachHitbox(lanceTip.GetComponent<DefaultHitbox>());

        timeLastPrimary = Time.time;
        attackingPrimary = true;
    }

    public override void SecondaryAttack()
    {
        base.SecondaryAttack();

        chargingShieldBash = true;
        currentShieldBashDamage = minimumShieldBashDamage;
        currentShieldBashKnockback = minimumShieldBashKnockback;
        currentShieldBashSpeed = minimumShieldBashSpeed;

        baseMovementSpeed = movementSpeed;
        movementSpeed = chargingMovementSpeed;
        timeStartedBash = Time.time;
        attackingSecondary = true;

        if (releaseSecondaryImm) ReleaseSecondary();
        releaseSecondaryImm = false;
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

    public override void ReleaseSecondary()
    {
        base.ReleaseSecondary();
        if (!chargingShieldBash) return;

        chargingShieldBash = false;
        timeLastSecondary = Time.time;
        playerController.SetAllowMovement(false);

        invincible = true;

        GameObject hitbox = Instantiate(shieldPrefab, transform);
        shieldBashEffects["knockback"] = currentShieldBashKnockback;
        hitbox.GetComponent<DefaultHitbox>().Init(this, dmg: currentShieldBashDamage, status: shieldBashEffects2);
        hitbox.GetComponent<DefaultHitbox>().SetAttackName("shieldBash");
        StartCoroutine(HandleBashMovement(hitbox));
    }

    private IEnumerator HandleBashMovement(GameObject hitbox)
    {
        float timeSinceStarted = 0f;

        while (timeSinceStarted < bashDuration)
        {
            if (hitbox.GetComponent<DefaultHitbox>().HasHitWall())
            {
                StartCoroutine(EnableMovement());
                invincible = false;
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
        invincible = false;
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
