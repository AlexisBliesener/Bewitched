using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Torch : MonoBehaviour
{
    public Light torchLight;

    public float minimumIntensity;
    public float maximumIntensity;

    public float minimumVariance;
    public float maximumVariance;

    public float minimumCycleDuration;
    public float maximumCycleDuration;

    private float currentIntensity;
    private float targetLight;
    private float timeLastSwitched;

    private float cycleDuration;
    private float variance;
    private float stepSize;

    private bool brightening = true;

    private void Start()
    {
        torchLight = GetComponent<Light>();
        

        currentIntensity = Random.Range(minimumIntensity, maximumIntensity);
        torchLight.intensity = currentIntensity;


        SetLightTarget();
    }

    private void Update()
    {
        currentIntensity += stepSize * Time.deltaTime;

        if ((brightening && currentIntensity >= targetLight) ||
        (!brightening && currentIntensity <= targetLight))
        {
            currentIntensity = targetLight;
            SetLightTarget();
        }
        torchLight.intensity = currentIntensity;
    }

    private void SetLightTarget()
    {
        cycleDuration = Random.Range(minimumCycleDuration, maximumCycleDuration);
        variance = Random.Range(minimumVariance, maximumVariance);

        if (currentIntensity > minimumIntensity && currentIntensity < maximumIntensity)
        {
            brightening = Random.Range(0, 2) == 0;
        }
        else
        {
            brightening = currentIntensity <= minimumIntensity;
        }

        if (brightening)
        {
            targetLight = currentIntensity + Mathf.Abs(variance);
            if (targetLight > maximumIntensity)
            {
                targetLight = maximumIntensity;
            }
        }
        else
        {
            targetLight = currentIntensity - Mathf.Abs(variance);
            if (targetLight < minimumIntensity)
            {
                targetLight = minimumIntensity;
            }
        }
        variance = targetLight - currentIntensity;

        timeLastSwitched = Time.time;
        stepSize = variance / cycleDuration;
    }
}
