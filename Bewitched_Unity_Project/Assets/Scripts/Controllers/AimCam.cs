using Cinemachine;
using FMODUnity;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

public class AimCam : MonoBehaviour
{
    const string FILE_ENDING = ".json";

    [SerializeField, Tooltip("Sensitiviy multiplier of the camera x-axis movement")]
    private float xSensitivity = 0.5f;
    [SerializeField, Tooltip("Sensitivity multiplier of the camera y-axis movement")]
    private float ySensitivity = 0.5f;


    [Tooltip("The virtual camera that is following the player")]
    [SerializeField]private CinemachineVirtualCamera virtualCamera;
    [Tooltip("Character that the camera is following")]
    private Character characterToFollow;
    [Tooltip("The POV component of the virtual camera")]
    private CinemachinePOV cameraPOVComponent;
    [Tooltip("The y-axis rotation applied to the player based on mouse movement")]
    private float yaw = 0;

    private void Update()
    {
        if (virtualCamera.Priority <2) return;

        if (CameraController.aiming)
        {
            characterToFollow.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            cameraPOVComponent.m_HorizontalAxis.Value = characterToFollow.transform.rotation.y;
        }
    }
    public void SetYaw(float yaw)
    {

    }

    public void Look(InputAction.CallbackContext context)
    {
        Vector2 lookInput = context.ReadValue<Vector2>();

        cameraPOVComponent.m_VerticalAxis.m_MaxSpeed = 300 * ySensitivity;

        if (context.action.activeControl.device.description.deviceClass != "Mouse")
        {
            lookInput.x *= 20;
        }

        // scale input
        yaw += lookInput.x * xSensitivity;
    }

    private void Awake()
    {
        characterToFollow = GetComponentInParent<Character>();
        virtualCamera = GetComponent<CinemachineVirtualCamera>();
        cameraPOVComponent = virtualCamera.GetCinemachineComponent<CinemachinePOV>();
    }

}
