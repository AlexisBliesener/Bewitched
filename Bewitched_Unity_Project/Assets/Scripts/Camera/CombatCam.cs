using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatCam : MonoBehaviour
{
    private CinemachineFreeLook combatCam;

    private bool inHitBy = false;
    private bool inOnAttack = false;
    private bool isOverriding = false;

    private float timeOverrideEnded = 0;


    private void Awake()
    {
        combatCam = GetComponent<CinemachineFreeLook>();
    }

    public IEnumerator PlayerHitBy(GameObject hitBy)
    {
        if (!CameraController.instance.GetLooking())
        {
            StopAllRotates();
            inHitBy = true;
            yield return StartCoroutine( RotateToEnemy(hitBy, 0.1f) );
            inHitBy = false;
        }
    }

    public void StopAllRotates()
    {
        StopAllCoroutines();
        inOnAttack = false;
        inHitBy = false;
    }

    private void FixedUpdate()
    {
        if (CameraController.instance.GetLooking())
        {
            StopAllRotates();
            isOverriding = true;
        }
        else
        { 
            if(isOverriding)
            {
                isOverriding = false;
                timeOverrideEnded = Time.time;
            }
            StartCoroutine(RotateToBiggestThreat(CameraController.instance.GetThreatWeight(), CameraController.instance.GetDistWeight(), CameraController.instance.GetMaxDistance(), RoomSystem.Instance.GetCurrentRoomEnemies(), SurroundingPoints.instance.GetAttackingEnemies()));
        }
            
    }

    public IEnumerator OnAttack(Vector3 forwardDir, float approachTime)
    {
        if (!inHitBy && !CameraController.instance.GetLooking())
        {
            StopAllRotates();
            inOnAttack = true;

            forwardDir.y = 0;

            float degrees = Vector3.SignedAngle(Vector3.forward, forwardDir, Vector3.up);

            if (degrees < 0)
            {
                degrees += 360;
            }

            yield return StartCoroutine(RotateCamera(degrees, approachTime));
            inOnAttack = false;
        }
    }

    public IEnumerator RotateToEnemy(GameObject enemy, float duration)
    {
        Vector3 toEnemy = enemy.transform.position - PlayerController.instance.currentCharacter.transform.position;

        toEnemy.y = 0;

        float degrees = Vector3.SignedAngle(Vector3.forward, toEnemy, Vector3.up);

        if (degrees < 0) degrees += 360;

        yield return StartCoroutine(RotateCamera(degrees, duration));
    }

    public IEnumerator RotateToBiggestThreat(int threatWeight, int distWeight, float maxDistance, List<GameObject> enemies, Dictionary<Character, int> attackingEnemies)
    {
        if (!inHitBy && !inOnAttack && !CameraController.instance.GetLooking() && Time.time - timeOverrideEnded > CameraController.instance.GetTimeWaitToPriorityRotate())
        {
            StopAllRotates();
            if (enemies != null)
            {
                Enemy topPriority = null;
                float priority = Mathf.NegativeInfinity;
                foreach (GameObject enemy in enemies)
                {
                    if (enemy == null) continue;
                    Enemy enemyComponent = enemy.GetComponent<Enemy>();
                    if (enemyComponent == PlayerController.instance.currentCharacter) continue;
                    float distance = Vector3.Distance(PlayerController.instance.currentCharacter.transform.position, enemy.transform.position);
                    float threat = enemyComponent.priority;
                    if (attackingEnemies.ContainsKey(enemyComponent))
                    {
                        threat += attackingEnemies[enemyComponent];
                    }

                    float currentPriority = (threat * threatWeight) + (maxDistance - distance) * distWeight;
                    if (topPriority == null || currentPriority > priority)
                    {
                        topPriority = enemyComponent;
                        priority = currentPriority;
                    }
                }

                if (topPriority != null)
                {
                    yield return StartCoroutine( RotateToEnemy(topPriority.gameObject, 0.01f) );
                }
            }
        }
    }

    private IEnumerator RotateCamera(float degrees, float time)
    {
        float startingValue = combatCam.m_XAxis.Value;
        for (int i = 1; i < 51; i++)
        {
            combatCam.m_XAxis.Value = Mathf.LerpAngle(startingValue, degrees, i / 50f);
            yield return new WaitForSeconds(time / 50f);
        }
    }
}
