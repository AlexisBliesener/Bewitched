using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;

public class CameraController : MonoBehaviour
{
    public Character characterToFollow;
    private StudioListener listener;

    public static CameraController instance { get; private set; }

    [Header("Distance Settings")]
    [SerializeField]
    [Tooltip("Z Distance From Player")]
    private float zOffset;

    [SerializeField]
    [Tooltip("Height Above Player")]
    private float height;

    [SerializeField]
    [Tooltip("Camera Switch Speed")]
    private float cameraTransitionSpeed = 3;

    private bool switching = false;

    private float transitionTime = 0f;

    private bool teleporting = false;

    private void Start()
    {
        instance = this;
    }

    void Awake()
    {
        if(!listener) listener = GetComponent<StudioListener>();
        if(!listener.attenuationObject) listener.attenuationObject = characterToFollow.gameObject;
        PlayerController.CharacterControlChangeEvent+=SwitchCharacter;
    }

    void OnDisable()
    {
        PlayerController.CharacterControlChangeEvent-=SwitchCharacter;
    }

    // Update is called once per frame
    void Update()
    {
        if (!switching && !teleporting)
        {
            transform.position = new Vector3(characterToFollow.transform.position.x, height, characterToFollow.transform.position.z - zOffset);
            transform.LookAt(characterToFollow.transform);
        }
        else
        {
            Vector3 endpoint = new Vector3(characterToFollow.transform.position.x, height, characterToFollow.transform.position.z - zOffset);
            transitionTime += Time.deltaTime * cameraTransitionSpeed;
            transform.position = Vector3.Lerp(transform.position, endpoint, transitionTime);

            if ((transform.position - new Vector3(characterToFollow.transform.position.x, height, characterToFollow.transform.position.z - zOffset)).magnitude < 0.05)
            {
                switching = false;
                teleporting = false;
                transitionTime = 0f;
            }
        }
    }
    void SwitchCharacter(Character character)
    {
        switching = true;
        characterToFollow = character;
        listener.attenuationObject=character.gameObject;
    }

    public void SetTeleporting()
    {
        teleporting = true;
    }
}
