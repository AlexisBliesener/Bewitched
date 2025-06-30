using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using FMODUnity;
using FMOD.Studio;
using UnityEngine.ProBuilder;
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
    [Tooltip("Wall Layermask")]
    [SerializeField] LayerMask environment;
    [SerializeField] GameObject knockBackCone;

    private void Start()
    {
        SetBaseStats();
    }

    void Awake()
    {
        if(knockBackCone){
            knockBackCone.SetActive(true);
            knockBackCone.GetComponent<KnockbackCone>().playerTrans = transform;
            knockBackCone.GetComponent<KnockbackCone>().knockbackAmount = knockbackAmount;
            knockBackCone.SetActive(false);
        }
        else throw new System.Exception("Hag Knockback Cone Not Assigned!");
    }

    public override void PrimaryAttack()
    {

        StartCoroutine(KnockBackCone());

        timeLastPrimary = Time.time;
    }

    public override void SecondaryAttack()
    {
        Blink();

        timeLastSecondary = Time.time;
    }

    public override void Die()
    {
        StopAllCoroutines();
        SceneManager.LoadScene(0);
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

    public void Blink()
    {
        PlayerController.instance.SetAllowMovement(false); // Prevent movement during blink
        CameraController.instance.SetTeleporting();        // Stop camera snap

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
