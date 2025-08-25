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
    [SerializeField] Dictionary<string, float> batSwingStatusEffects = new Dictionary<string, float>()
    {
        {"knockback", 5 },
        {"timeStop", .15f }
    };

    [SerializeField] AttackStatusEffects batSwingEffects2;

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

    [Tooltip("Slam Status Effects")]
    [SerializeField]
    Dictionary<string, float> slamBatEffects = new Dictionary<string, float>()
    {
        {"knockbackMin", 20 },
        {"knockbackMax", 70 },
        {"knockbackRange", 8 }
    };
    [SerializeField] AttackStatusEffects slamBatEffects2;
    [SerializeField] AttackStatusEffects slamImpactEffects;

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

    void Start()
    {
        SetPlayerInfo();
        SetHealthToMax();
        SetBaseStats();
    }

    private void Update()
    {
        currentPlayer = playerController.GetCurrentCharacter();
        HandleHitStun();
        HandleJumpMovement();
        if (!playerControlling)
        {
            SetRangeChecks();
            SetBehavior();
            agent.speed = movementSpeed;
        }
        else
        {
            ChargeBatSwing();
        }
    }

    public override void PrimaryAttack()
    {
        base.PrimaryAttack();

        isCharging = true;
        currentBatSwingKnockback = minimumBatSwingKnockback;
        currentBatSwingDamage = minimumBatSwingDamage;
        currentBatSwingAngle = minimumBatSwingAngle;

        timeSwingStarted = Time.time;
        PlayerController.instance.SetAllowMovement(false);
        attackingPrimary = true;

        if (!playerControlling || releasePrimaryImm) ReleasePrimary();
        releasePrimaryImm = false;
    }

    public override void SecondaryAttack()
    {
        base.SecondaryAttack();

        attackingSecondary = true;
        timeLastSecondary = Time.time;
        PlayerController.instance.SetAllowMovement(false);

        groundHeight = transform.position.y;
        jumping = true;
        jumpVelocity = ogreJumpSpeed;
    }

    public override void ReleasePrimary()
    {
        base.ReleasePrimary();
        if (!isCharging) return;

        isCharging = false;
        timeLastPrimary = Time.time;

        minAngle = Quaternion.Euler(0, currentBatSwingAngle / 2, 0) * Quaternion.LookRotation(transform.forward);
        maxAngle = Quaternion.Euler(0, -currentBatSwingAngle / 2, 0) * Quaternion.LookRotation(transform.forward);

        GameObject pivot = Instantiate(batPivot, transform);
        pivot.GetComponent<DefaultHitbox>().Init(this);
        pivot.SetActive(false);

        GameObject batHitbox = Instantiate(batHitboxPrefab, transform);
        batHitbox.GetComponent<DefaultHitbox>().Init(this, dmg: currentBatSwingDamage, status: batSwingEffects2);
        batHitbox.GetComponent<DefaultHitbox>().SetAttackName("batSwing");
        pivot.GetComponent<DefaultHitbox>().AttachHitbox(batHitbox.GetComponent<DefaultHitbox>());

        StartCoroutine(SwingBat(pivot));
    }

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
        SetPrimaryAnimStatus(false);
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
                slamBatHitbox.GetComponent<DefaultHitbox>().Init(this, dmg: ogreJumpBatDamage, slamDMG: ogreJumpSlamDamage, status: slamBatEffects2);
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
}
