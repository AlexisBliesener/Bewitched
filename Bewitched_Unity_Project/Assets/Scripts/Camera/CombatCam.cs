using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class CombatCam : MonoBehaviour
{
    private CinemachineFreeLook combatCam;

    public bool inHitBy = false;
    public bool inOnAttack = false;

    private void Awake()
    {
        combatCam = GetComponent<CinemachineFreeLook>();
    }

    public IEnumerator PlayerHitBy(GameObject hitBy)
    {
        StopAllRotates();
        inHitBy = true;

        Vector3 toEnemy = hitBy.transform.position - PlayerController.instance.currentCharacter.transform.position;

        toEnemy.y = 0;

        float degrees = Vector3.SignedAngle(Vector3.forward, toEnemy, Vector3.up);

        if (degrees < 0) degrees += 360;

        yield return StartCoroutine(RotateCamera(degrees, 0.1f));

        inHitBy = false;
    }

    private void StopAllRotates()
    {
        StopAllCoroutines();
        inOnAttack = false;
        inHitBy = false;
    }

    private void FixedUpdate()
    {
        StartCoroutine(RotateToBiggestThreat(CameraController.instance.GetThreatWeight(), CameraController.instance.GetDistWeight(), CameraController.instance.GetMaxDistance(), RoomSystem.Instance.GetCurrentRoomEnemies(), SurroundingPoints.instance.GetAttackingEnemies()));
    }

    public IEnumerator OnAttack(Vector3 forwardDir, float approachTime)
    {
        if(!inHitBy)
        {
            StopAllRotates();
            inOnAttack = true;

            forwardDir.y = 0;

            float degrees = Vector3.SignedAngle(Vector3.forward, forwardDir, Vector3.up);

            if (degrees < 0) degrees += 360;

            yield return StartCoroutine(RotateCamera(degrees, approachTime));
            inOnAttack = false;
        }
    }

    public IEnumerator RotateToBiggestThreat(int threatWeight, int distWeight, float maxDistance, List<GameObject> enemies, Dictionary<Character, int> attackingEnemies)
    {
        if (!inHitBy && !inOnAttack )
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
                    if(enemyComponent == PlayerController.instance.currentCharacter) continue;
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
                    Vector3 toEnemy = topPriority.transform.position - PlayerController.instance.currentCharacter.transform.position;

                    toEnemy.y = 0;

                    float degrees = Vector3.SignedAngle(Vector3.forward, toEnemy, Vector3.up);

                    if (degrees < 0) degrees += 360;

                    yield return StartCoroutine(RotateCamera(degrees, 0.01f));
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
