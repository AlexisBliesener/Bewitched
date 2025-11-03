using System.Collections;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using UnityEngine.SceneManagement;

public class Hag : Character
{
    //This is just a basic character class for now just for testing stuff with the hag.

    [Header("Hag Settings")]
    [Tooltip("Knockback Radius")]
    [SerializeField] float knockbackAngle = 50;
    [Tooltip("Knockback Range")]
    [SerializeField] float knockbackRange = 1;
    [Tooltip("Knockback Amount")]
    [SerializeField] float knockbackAmount = 2;
    [Tooltip("Blink Distance")]
    [SerializeField] float blinkDistance = 10;
    [SerializeField] GameObject knockBackCone;
    [SerializeField, Tooltip("Objects to disable when eleth is possessing an enemy")]
    private GameObject[] objectsToDisable;

    [Tooltip("The animator controller for eleth character")]
    private ElethAnimator elethAnimator;
    //The character controller component that controls the hag. For accessing hitbox.
    CharacterController controller;

    [Tooltip("Death UI Pop-up Screen")]
    public GameObject deathUI;



    private void Start()
    {
        elethAnimator = GetComponent<ElethAnimator>();
        SetBaseStats();
    }

    private void FixedUpdate()
    {
        CreateLocalInvalidArea();
    }

    protected override void Awake()
    {
        base.Awake();
        if (knockBackCone)
        {
            knockBackCone.SetActive(true);
            knockBackCone.GetComponent<KnockbackCone>().playerTrans = transform;
            knockBackCone.GetComponent<KnockbackCone>().knockbackAmount = knockbackAmount;
            knockBackCone.SetActive(false);
        }
        else throw new System.Exception("Hag Knockback Cone Not Assigned!");
        if (!TryGetComponent(out controller))
        {
            Debug.LogError("Eleth doesn't have a CharacterController component!");
        }
    }

    private void FixedUpdate()
    {
        CreateLocalInvalidArea();
    }

    public override IEnumerator BeginPrimary()
    {
       yield return null;
    }

    public override IEnumerator BeginSecondary()
    {
        yield return null;
    }

    public override void PrimaryAttack()
    {

        //StartCoroutine(KnockBackCone());

        //timeLastPrimary = Time.time;
    }

    public override void SecondaryAttack()
    {
        //Blink();

        //timeLastSecondary = Time.time;
    }

    protected override void OnDamaged(float amount)
    {
        base.OnDamaged(amount);
        //Play the Witch's hit sound effect when she gets damaged.
        AudioManager.TryGetReference("WitchHit", out EventReference evRef);
        EventInstance inst = RuntimeManager.CreateInstance(evRef);
        inst.setParameterByName("Damage", amount / health.GetMaxHealth());
        inst.start();
        inst.release();
    }

    /// <summary>
    /// When the player is done possessing calling this disables Eleth
    /// </summary>
    public void DisableEleth()
    {
        foreach (GameObject go in objectsToDisable)
        {
            go.SetActive(false);
        }
        controller.detectCollisions = false;
    }

    /// <summary>
    /// When the player is done possessing calling this enables Eleth
    /// </summary>
    public void EnableEleth()
    {
        foreach (GameObject go in objectsToDisable)
        {
            go.SetActive(true);
        }
        controller.detectCollisions = true;
    }

    /// <summary>
    /// Called when Eleth dies
    /// Fires death animation and music
    /// Stops movement
    /// </summary>
    protected override void OnDeath(GameObject enemyGameObject)
    {
        AnimateDeath();
        //This is temporary until we implement the big "You Died" UI Banner thing.
        //This is just so Andrew actually hears this sound effect.

        //Stops all non-UI events. WitchDeath event also mutes all other sound effects
        RuntimeManager.GetBus("bus:/Music/LevelMusic").stopAllEvents(FMOD.Studio.STOP_MODE.IMMEDIATE);
        AudioManager.TryPlayOneShot("WitchDeath");
        AudioManager.TryPlaySnapshot("GameOver");
        //Disable player controller
        PlayerController.instance.gameObject.SetActive(false);
        //Wait until the sound effect is over before returning to the main menu
        Invoke("Die", 12f);
        if (hitStunActual != null) Destroy(hitStunActual);
        if (counterIndicatorVFX != null) Destroy(counterIndicatorVFX);
    }

    public override void Die()
    {
        StopAllCoroutines();
        deathUI.SetActive(true);
        //SceneManager.LoadScene(0); // go back to main menu
    }

    private void Update()
    {
        HandleHitStun();
    }

    public IEnumerator KnockBackCone()
    {
        Vector3 forwardDir = transform.forward;
        /*
        for (int i = 0; i < 20; i++)
        {
            float angle = -knockbackAngle / 2f + (18 * i);
            Quaternion rotation = Quaternion.Euler(0, angle, 0);
            Vector3 direction = rotation * forwardDir;

            RaycastHit hit;
            if (Physics.Raycast(transform.position + transform.forward.normalized, direction, out hit, knockbackRange, characters))
            {
                if (hit.collider.GetComponent<Enemy>())
                {
                    Enemy enemy = hit.collider.GetComponent<Enemy>();
                    
                    enemy.GetComponent<KnockbackControl>().AddImpact((enemy.transform.position - transform.position).normalized, knockbackAmount);
                }
            }
        }
        */
        AudioManager.TryPlayOneShot("WitchPush");
        knockBackCone.SetActive(true);
        for(int i = 0; i < 5; i++){
            yield return new WaitForFixedUpdate();
        }
        knockBackCone.SetActive(false);

    }

    /// <summary>
    /// Called when the possession ability is used
    /// Sets Eleth to possession animation state
    /// </summary>
    public void AnimatePossess()
    {
        elethAnimator.SwitchState("Possession");
    }

    public void Blink()
    {
        PlayerController.instance.SetAllowMovement(false); // Prevent movement during blink


        RaycastHit hit;
        if (!Physics.Raycast(transform.position, transform.forward, out hit, blinkDistance, environment))
        {
            transform.position = transform.position + transform.forward.normalized * blinkDistance;
        }
        else
        {
            transform.position = hit.point - transform.forward.normalized * 0.5f;
        }

        // Set a small delay before allowing movement again
        StartCoroutine(EnableMovement());
        AudioManager.TryPlayOneShot("Blink");
    }
}

