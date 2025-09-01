using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Goblin : Enemy
{

    [Header("Goblin Settings")]
    [Tooltip("Knife Prefab")]
    [SerializeField] GameObject knifePrefab;
    [Tooltip("Thrust Speed")]
    [SerializeField] float thrustSpeed = 10;
    [Tooltip("Knife Damage")]
    [SerializeField] float knifeDamage = 20;
    [Tooltip("Knife Effects")]
    [SerializeField] AttackStatusEffects knifeEffects;
    [Tooltip("Dash Hitbox")]
    [SerializeField] GameObject dashHitbox;
    [Tooltip("Dash Speed")]
    [SerializeField] float dashSpeed = 50;
    [Tooltip("Dash Duration")]
    [SerializeField] float dashDuration = 0.5f;
    [Tooltip("Dash Damage")]
    [SerializeField] float dashDamage = 30;
    [Tooltip("Dash Effects")]
    [SerializeField] AttackStatusEffects dashEffects;

    private bool isDashing = false;

    private void Start()
    {
        SetPlayerInfo();
        health.SetHealthToMax();
        SetBaseStats();
    }

    private void Update()
    {
        currentPlayer = playerController.GetCurrentCharacter();
        if (!playerControlling)
        {
            SetRangeChecks();
            SetBehavior();
            agent.speed = movementSpeed;
        }
        HandleHitStun();
    }

    public override void PrimaryAttack()
    {
        base.PrimaryAttack();

        GameObject shank = Instantiate(knifePrefab, transform);
        shank.GetComponent<DefaultHitbox>().Init(this, dmg: knifeDamage, forwardVelocity: thrustSpeed, status: knifeEffects);

        timeLastPrimary = Time.time;
        attackingPrimary = true;
    }
    
    public override void SecondaryAttack()
    {
        base.SecondaryAttack();

        Dash();
        attackingSecondary = true;
        timeLastSecondary = Time.time;
    }

    public void Dash()
    {
        isDashing = true;
        health.SetInvincible(true);
        PlayerController.instance.SetAllowMovement(false);

        GameObject hitbox = Instantiate(dashHitbox, transform);
        hitbox.GetComponent<DefaultHitbox>().Init(this, dmg: dashDamage, attackDuration: dashDuration, status: dashEffects);

        StartCoroutine(HandleDashMovement(hitbox));
    }

    private IEnumerator HandleDashMovement(GameObject hitbox)
    {
        float timeSinceStarted = 0f;

        while (timeSinceStarted < dashDuration)
        {
            if (hitbox.GetComponent<DefaultHitbox>().HasHitWall())
            {
                StartCoroutine(EnableMovement());
                isDashing = false;
                health.SetInvincible(false);
                attackingSecondary = false;

                transform.position = transform.position - transform.forward.normalized * dashSpeed * Time.deltaTime;

                yield break;
            }

            transform.position = transform.position + transform.forward.normalized * dashSpeed * Time.deltaTime;
            timeSinceStarted += Time.deltaTime;
            yield return null;
        }

        transform.position = transform.position + transform.forward.normalized * dashSpeed * Time.deltaTime;

        Destroy(hitbox);

        StartCoroutine(EnableMovement());
        isDashing = false;
        health.SetInvincible(false);
        attackingSecondary = false;
    }
}
