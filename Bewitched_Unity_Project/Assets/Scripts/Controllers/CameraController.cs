using Cinemachine;
using FMODUnity;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    const string FILE_ENDING = ".json";

    [Tooltip("Singleton of the CameraController")]
    public static CameraController instance { get; private set; }

    [SerializeField, Tooltip("Character that the camera is following")]
    private Character characterToFollow;
    [SerializeField, Tooltip("Sensitiviy multiplier of the camera x-axis movement")]
    private float xSensitivity = 0.5f;
    [SerializeField, Tooltip("Sensitivity multiplier of the camera y-axis movement")]
    private float ySensitivity = 0.5f;
    [SerializeField, Tooltip("Layermask the only holds objects that are in the environment")]
    private LayerMask environmentMask;
    [SerializeField, Tooltip("Speed multiplier applied to camera side to side switch movement")] 
    private float switchSpeed = 0.5f;
    [SerializeField, Tooltip("the main camera with the cinemachine brain")]
    private Camera mainCam;

    [Tooltip("The FMOD studio listener that is attached to the camera")]
    private StudioListener listener;
    [Tooltip("The side the camera is on releative to the player, 1 = right side, 0 = middle, -1 = left side")]
    private float camSide = 1;
    [Tooltip("The virtual camera that is following the player")]
    private CinemachineVirtualCamera virtualCamera;
    [Tooltip("The y-axis rotation applied to the player based on mouse movement")]
    private float yaw = 0;
    [Tooltip("The side of the player the camera is currently targeting to be on, 1 = right side, 0 = middle, -1 = left side")]
    private float targetCamSide = 1f;

    #region Saving/Loading

    [ContextMenu("Save to JSON")]
    public void SaveToJson()
    {
        string cameraStatsStr = JsonUtility.ToJson(this, true);

        string folderPath = Path.Combine(Application.dataPath, "JSON");
        folderPath = Path.Combine(folderPath, "CameraStats");
        SeeFilePath();
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string filePath = Path.Combine(folderPath, "camera" + FILE_ENDING);
        File.WriteAllText(filePath, cameraStatsStr);


#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }

    [ContextMenu("See File Path")]
    public void SeeFilePath()
    {
        string folderPath = Path.Combine(Application.persistentDataPath, "JSON");
        folderPath = Path.Combine(folderPath, "CameraStats");
        Debug.Log("Path To JSON File:");
        Debug.Log(folderPath);
    }

    [ContextMenu("Load From JSON")]
    public void LoadFromJson()
    {

        string folderPath = Path.Combine(Application.dataPath, "JSON");
        folderPath = Path.Combine(folderPath, "CameraStats");
        string filePath = Path.Combine(folderPath, "camera" + FILE_ENDING);

        string jsonStr = File.ReadAllText(filePath);

        string[] jsons = jsonStr.Split("|");

        JsonUtility.FromJsonOverwrite(jsons[0], this);

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }

    #endregion

    /// <summary>
    /// Handles camera rotation based on player input.
    /// Updates the player's yaw (y-axis rotation) using mouse/gamepad look input.
    /// </summary>
    /// <param name="context">The input context containing look delta values.</param>
    public void Look(InputAction.CallbackContext context)
    {
        Vector2 lookInput = context.ReadValue<Vector2>();

        if (context.action.activeControl.device.description.deviceClass != "Mouse")
        {
            lookInput.x *= 20;
        }
            // scale input
            yaw += lookInput.x * xSensitivity;

        // apply rotations
        characterToFollow.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    private void OnDisable()
    {
        PlayerController.CharacterControlChangeEvent -= SwitchCharacter;
    }

    private void OnEnable()
    {
        PlayerController.CharacterControlChangeEvent += SwitchCharacter;
    }

    private void Awake()
    {
        instance = this;
        virtualCamera = GetComponent<CinemachineVirtualCamera>();
        PlayerController.CharacterControlChangeEvent += SwitchCharacter;

        // FMOD set up
        if (!listener) listener = GetComponent<StudioListener>();
        if (!listener.attenuationObject) listener.attenuationObject = characterToFollow.gameObject;
    }

    private void Update()
    {
        SwitchCameraSide();
    }

    /// <summary>
    /// Determines if the camera should switch sides based on environment collisions.
    /// Smoothly interpolates the Cinemachine camera side between left and right.
    /// </summary>
    private void SwitchCameraSide()
    {
        Ray ray = new Ray(characterToFollow.transform.position, transform.right);
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

    /// <summary>
    /// Switches the camera to follow a new character.
    /// Updates FMOD listener, Cinemachine follow/look targets, and shoulder offset.
    /// </summary>
    /// <param name="character">The new character to follow.</param>
    private void SwitchCharacter(Character character)
    {
        characterToFollow = character;
        listener.attenuationObject = character.gameObject;

        // Virtual camera follows new character
        virtualCamera.gameObject.transform.SetParent(character.transform);
        mainCam.gameObject.transform.SetParent(character.transform);
        virtualCamera.Follow = characterToFollow.gameObject.transform;
        virtualCamera.LookAt = characterToFollow.gameObject.transform;
        virtualCamera.GetCinemachineComponent<Cinemachine3rdPersonFollow>().ShoulderOffset = characterToFollow.GetShoulderOffset();
    }
}
