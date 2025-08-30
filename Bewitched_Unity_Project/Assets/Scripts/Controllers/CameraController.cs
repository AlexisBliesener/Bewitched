using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static Codice.Client.Commands.WkTree.WorkspaceTreeNode;
using Cinemachine;

public class CameraController : MonoBehaviour
{
    [SerializeField]
    private Character characterToFollow;
    private StudioListener listener;

    [SerializeField]
    private float xSensitivity = 0.5f;

    private float yaw = 0;

    private CinemachineVirtualCamera virtualCamera;

    [SerializeField]
    private LayerMask environmentMask;

    private float camSide = 1;

    public static CameraController instance { get; private set; }

    private void Awake()
    {
        instance = this;
        virtualCamera = GetComponent<CinemachineVirtualCamera>();
    }

    [SerializeField] private float switchSpeed = 0.5f; // smoothness
    private float targetCamSide = 1f;                // start on right

    void Update()
    {
        Ray ray = new Ray(characterToFollow.transform.position , transform.right);
        bool hitRight = Physics.Raycast(ray, 4f, environmentMask);

        // Decide target side
        if (hitRight)
            targetCamSide = -1f;   // force to left side
        else
            targetCamSide = 1f;  // force to right side (or default)

        // Smoothly move toward target side
        var tpf = virtualCamera.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
        tpf.CameraSide = Mathf.Lerp(tpf.CameraSide, targetCamSide, Time.deltaTime * switchSpeed);
    }

    public void Look(InputAction.CallbackContext context)
    {
        Vector2 lookInput = context.ReadValue<Vector2>();

        // scale input
        yaw += lookInput.x * xSensitivity;

        // apply rotations
        characterToFollow.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

    }



    //[Header("Distance Settings")]
    //[SerializeField]
    //[Tooltip("Z Distance From Player")]
    //private float zOffset;

    //[SerializeField]
    //[Tooltip("Height Above Player")]
    //private float height;

    //[SerializeField]
    //[Tooltip("Camera Switch Speed")]
    //private float cameraTransitionSpeed = 3;

    //private bool switching = false;

    //private float transitionTime = 0f;

    private bool teleporting = false;


    //void Awake()
    //{
    //    if(!listener) listener = GetComponent<StudioListener>();
    //    if(!listener.attenuationObject) listener.attenuationObject = characterToFollow.gameObject;
    //    PlayerController.CharacterControlChangeEvent+=SwitchCharacter;
    //}

    //void OnDisable()
    //{
    //    PlayerController.CharacterControlChangeEvent-=SwitchCharacter;
    //}

    //// Update is called once per frame
    //void Update()
    //{
    //    if (!switching && !teleporting)
    //    {
    //        transform.position = new Vector3(characterToFollow.transform.position.x, height, characterToFollow.transform.position.z - zOffset);
    //        transform.LookAt(characterToFollow.transform);
    //    }
    //    else
    //    {
    //        Vector3 endpoint = new Vector3(characterToFollow.transform.position.x, height, characterToFollow.transform.position.z - zOffset);
    //        transitionTime += Time.deltaTime * cameraTransitionSpeed;
    //        transform.position = Vector3.Lerp(transform.position, endpoint, transitionTime);

    //        if ((transform.position - new Vector3(characterToFollow.transform.position.x, height, characterToFollow.transform.position.z - zOffset)).magnitude < 0.05)
    //        {
    //            switching = false;
    //            teleporting = false;
    //            transitionTime = 0f;
    //        }
    //    }
    //}
    //void SwitchCharacter(Character character)
    //{
    //    switching = true;
    //    characterToFollow = character;
    //    listener.attenuationObject=character.gameObject;
    //}

    public void SetTeleporting()
    {
        teleporting = true;
    }
}
