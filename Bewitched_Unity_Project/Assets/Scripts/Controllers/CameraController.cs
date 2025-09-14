using Cinemachine;
using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static Codice.Client.Commands.WkTree.WorkspaceTreeNode;

public class CameraController : MonoBehaviour
{
    public CinemachineFreeLook freeLookCam;
    public CinemachineVirtualCamera virtualCam;
    [Tooltip("The FMOD studio listener that is attached to the camera")]
    private StudioListener listener;

    public static bool aiming = false;

    [SerializeField, Tooltip("Crosshair image component")]
    private Image crossHair;

    private AimCam aimCam;
    public Character currentCharacter;

    private void Awake()
    {
        aiming = false;
        aimCam = virtualCam.GetComponent<AimCam>();
        freeLookCam.Priority = 2;
        virtualCam.Priority = 1;
        aimCam.SetYaw(freeLookCam.m_XAxis.Value);

        // FMOD set up
        if (!listener) listener = GetComponent<StudioListener>();
        if (!listener.attenuationObject) listener.attenuationObject = currentCharacter.gameObject;
    }

    private void UpdateCam()
    {
        if (aiming)
        {
            freeLookCam.Priority = 1;
            virtualCam.Priority = 2;
        }
        else
        {
            freeLookCam.Priority = 2;
            virtualCam.Priority = 1;
        }
    }

    /// <summary>
    /// Handles camera rotation based on player input.
    /// Updates the player's yaw (y-axis rotation) using mouse/gamepad look input.
    /// </summary>
    /// <param name="context">The input context containing look delta values.</param>
    public void Look(InputAction.CallbackContext context)
    {
        aimCam.Look(context);
    }

    public void Aim(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            aimCam.SetYaw(freeLookCam.m_XAxis.Value);
            crossHair.gameObject.SetActive(true);
            aiming = true;
        }
        else if (context.canceled)
        {
            crossHair.gameObject.SetActive(false);
            aiming = false;
        }
        UpdateCam();
    }

    private void Update()
    {
        if (aiming)
        {
            freeLookCam.transform.rotation = currentCharacter.gameObject.transform.rotation;
            freeLookCam.transform.position = currentCharacter.gameObject.transform.position;
        }
    }

    private void OnDisable()
    {
        PossessionAbility.CharacterControlChangeEvent -= SwitchCharacter;
    }

    private void OnEnable()
    {
        PossessionAbility.CharacterControlChangeEvent += SwitchCharacter;
    }

    /// <summary>
    /// Switches the camera to follow a new character.
    /// Updates FMOD listener, Cinemachine follow/look targets, and shoulder offset.
    /// </summary>
    /// <param name="character">The new character to follow.</param>
    private void SwitchCharacter(Character character)
    {
        // listener.attenuationObject = character.gameObject;

        virtualCam.Priority = 0;
        freeLookCam.Priority = 0;

        currentCharacter = character;
        if (!listener.attenuationObject) listener.attenuationObject = currentCharacter.gameObject;

        // Virtual camera follows new character
        virtualCam = character.GetVirtualCam();

        // free follow cam
        freeLookCam = character.GetFreeLookCam();

        aimCam = virtualCam.GetComponent<AimCam>();

        UpdateCam();
    }

}
