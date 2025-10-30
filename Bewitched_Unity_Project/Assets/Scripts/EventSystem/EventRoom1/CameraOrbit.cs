using UnityEngine;
using Cinemachine;
// This script will orbit the camera around the cinemachine path
public class CameraOrbit : MonoBehaviour
{
    [SerializeField, Tooltip("The camera to orbit")]
    private CinemachineVirtualCamera virtualCam;
    [SerializeField, Tooltip("The speed of the orbit")]
    private float speed = 1.5f;
    [SerializeField, Tooltip("The dolly component of the camera")]
    private CinemachineTrackedDolly dolly;
    [SerializeField, Tooltip("The path component of the camera")]
    private CinemachinePathBase path;
    /// <summary>
    /// this will intialize the camera orbit when the script is started
    /// </summary>
    void Start()
    {
        if (virtualCam)
        {
            dolly = virtualCam.GetCinemachineComponent<CinemachineTrackedDolly>();
            path = dolly.m_Path;

            if (dolly != null && path != null)
            {
                // Start at the very beginning of the path
                dolly.m_PathPosition = path.MinPos;
            }
        }
    }
    /// <summary>
    /// this will update the camera orbit
    /// it will move the camera along the path
    /// </summary>
    void Update()
    {
        if (dolly != null && path != null)
        {
            dolly.m_PathPosition += speed * Time.deltaTime;

            if (dolly.m_PathPosition > path.MaxPos)
                dolly.m_PathPosition -= path.MaxPos;
        }
    }
}
