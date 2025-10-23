using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class CombatCam : MonoBehaviour
{
    private CinemachineFreeLook combatCam;

    private void Awake()
    {
        combatCam = GetComponent<CinemachineFreeLook>();
    }



    public void PlayerHitBy(GameObject hitBy)
    {
        StopAllCoroutines();

        Vector3 toEnemy = hitBy.transform.position - PlayerController.instance.currentCharacter.transform.position;

        toEnemy.y = 0;

        float degrees = Vector3.SignedAngle(Vector3.forward, toEnemy, Vector3.up);

        if (degrees < 0) degrees += 360;

        StartCoroutine(RotateCamera(degrees, 0.1f));

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
