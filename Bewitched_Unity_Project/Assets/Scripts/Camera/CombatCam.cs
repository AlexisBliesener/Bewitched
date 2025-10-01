using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class CombatCam : MonoBehaviour
{
    private CinemachineFreeLook combatCam;

    private float vel = 0f;

    private void Awake()
    {
        combatCam = GetComponent<CinemachineFreeLook>();
    }

    public void TargetSet(GameObject enemy)
    {
        //Vector3 combatCamForward = new Vector3(this.transform.forward.x, 0, this.transform.forward.z);
        //Vector3 toEnemy = enemy.transform.position - this.transform.position;
        //toEnemy = new Vector3(toEnemy.x, 0, toEnemy.z);
        //toEnemy = toEnemy.normalized;
        //combatCamForward = combatCamForward.normalized;

        //float dotProduct = Vector3.Dot(combatCamForward, toEnemy);
        //float angle = Mathf.Acos(dotProduct);
        //angle = Mathf.Rad2Deg * angle;
        //angle += 180;

        //Debug.Log("ANGLE " + angle);

        //StartCoroutine(SmoothFrame(angle));

        StartCoroutine(Recenter());
    }

    private IEnumerator SmoothFrame(float angle)
    {
        for(int i = 0; i < 50; i ++)
        {
            combatCam.m_XAxis.Value = Mathf.SmoothDampAngle(combatCam.m_XAxis.Value, angle, ref vel, 0.5f);
            yield return new WaitForSeconds(0.01f);
        }
    }

    private IEnumerator Recenter()
    {
        combatCam.m_RecenterToTargetHeading.m_enabled = true;
        yield return new WaitForSeconds(combatCam.m_RecenterToTargetHeading.m_RecenteringTime);
        combatCam.m_RecenterToTargetHeading.m_enabled = false;
    }
}
