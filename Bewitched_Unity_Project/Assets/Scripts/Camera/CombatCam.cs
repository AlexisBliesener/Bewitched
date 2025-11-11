using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls dynamic camera behavior during combat using CinemachineFreeLook.
/// Handles automatic rotations towards attackers, attack directions, and priority enemies.
/// </summary>
public class CombatCam : MonoBehaviour
{
    [Tooltip("The combat cam that this combat cam script is controlling")]
    private CinemachineFreeLook combatCam;
    [Tooltip("If the camera is currently moving to center on the enemy that hit the player")]
    private bool inHitBy = false;
    [Tooltip("If the camera is currently moving to center on the players attack direction")]
    private bool inOnAttack = false;
    [Tooltip("If the camera is currenty being overriden by right stick input")]
    private bool isOverriding = false;
    [Tooltip("The time the override of the camera control ended")]
    private float timeOverrideEnded = 0;


    private void Awake()
    {
        combatCam = GetComponent<CinemachineFreeLook>();
    }

    /// <summary>
    /// Rotates the camera to focus on the enemy that just hit the player.
    /// </summary>
    /// <param name="hitBy">The enemy GameObject that hit the player.</param>
    public IEnumerator PlayerHitBy(GameObject hitBy)
    {
        if (!CameraController.instance.GetLooking())
        {
            StopAllRotates();
            inHitBy = true;
            yield return StartCoroutine( RotateToEnemy(hitBy, CameraController.instance.GetHitRotationTime()) );
            inHitBy = false;
        }
    }

    /// <summary>
    /// Stops all ongoing camera rotation coroutines and resets rotation state flags.
    /// </summary>
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

    /// <summary>
    /// Rotates the camera to align with the player's current attack direction.
    /// </summary>
    /// <param name="forwardDir">The forward direction of the player's attack.</param>
    /// <param name="approachTime">Time over which the rotation should occur.</param>
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

            yield return StartCoroutine(RotateCamera(degrees, CameraController.instance.GetAttackingRotationTime(), approachTime));
            inOnAttack = false;
        }
    }

    /// <summary>
    /// Rotates the camera to face a specific enemy GameObject.
    /// </summary>
    /// <param name="enemy">The target enemy to face.</param>
    /// <param name="duration">How long the rotation should take.</param>
    public IEnumerator RotateToEnemy(GameObject enemy, float duration)
    {
        Vector3 toEnemy = enemy.transform.position - PlayerController.instance.currentCharacter.transform.position;

        toEnemy.y = 0;

        float degrees = Vector3.SignedAngle(Vector3.forward, toEnemy, Vector3.up);

        if (degrees < 0) degrees += 360;

        float dist = toEnemy.magnitude;

        dist -= 1;

        dist = Mathf.Clamp(dist, 0f, 10f);

        yield return StartCoroutine(RotateCamera(degrees, 1f - dist / 10f, duration));
    }

    /// <summary>
    /// Rotates the camera toward the enemy with the highest calculated threat priority.
    /// Takes into account enemy threat level, distance, and attack engagement.
    /// </summary>
    /// <param name="threatWeight">Weight multiplier for enemy threat values.</param>
    /// <param name="distWeight">Weight multiplier for enemy distance values.</param>
    /// <param name="maxDistance">Maximum distance considered for weighting.</param>
    /// <param name="enemies">List of enemies in the current room.</param>
    /// <param name="attackingEnemies">Dictionary of attacking enemies and their bonus priority.</param>
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

                    Vector3 direction = (enemy.transform.position - PlayerController.instance.currentCharacter.transform.position).normalized;
                    if (Physics.Raycast(PlayerController.instance.currentCharacter.transform.position + Vector3.up * 1.5f, direction, out RaycastHit hit, distance, LayerMask.GetMask("Environment", "Character" )))
                    {
                        if (hit.collider.gameObject != enemy)
                            continue;
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
                    yield return StartCoroutine( RotateToEnemy(topPriority.gameObject, CameraController.instance.GetGeneralPriorityRotationTime()) );
                }
            }
        }
    }

    /// <summary>
    /// Smoothly rotates the CinemachineFreeLook camera over time to the target horizontal and vertical angles.
    /// </summary>
    /// <param name="degrees">The target horizontal rotation angle in degrees.</param>
    /// <param name="normalizedVal">Normalized vertical axis value (0–1 range).</param>
    /// <param name="time">Total duration of the rotation in seconds.</param>
    private IEnumerator RotateCamera(float degrees, float normalizedVal, float time)
    {
        float startingValueX = combatCam.m_XAxis.Value;
        float startingValueY = combatCam.m_YAxis.Value;

        for (int i = 1; i < 51; i++)
        {
            combatCam.m_XAxis.Value = Mathf.LerpAngle(startingValueX, degrees, i / 50f);
            combatCam.m_YAxis.Value = Mathf.Lerp(startingValueY, normalizedVal, i / 50f);
            yield return new WaitForSeconds(time / 50f);
        }
    }
}
