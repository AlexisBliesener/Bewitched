using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

[RequireComponent(typeof(Light))]
public class Torch : MonoBehaviour, IDoor
{

    [SerializeField, Tooltip("Are you a dev? [Don't check this if you're not a dev!!]")]
    protected private bool dev = false;
    [Header("Light Settings")]
    [Tooltip("Light component")]
    private Light torchLight;
    [SerializeField,Tooltip("Minimum intensity of the light")]
    public float minimumIntensity;
    [SerializeField,Tooltip("Maximum intensity of the light")]
    public float maximumIntensity;

    [SerializeField,Tooltip("Minimum variance of the light")]
    public float minimumVariance;
    [SerializeField,Tooltip("Maximum variance of the light")]
    public float maximumVariance;

    [SerializeField,Tooltip("Minimum cycle duration of the light")]
    public float minimumCycleDuration;
    [SerializeField,Tooltip("Maximum cycle duration of the light")]
    public float maximumCycleDuration;

    [Tooltip("Current intensity of the light, this is always changing called on update")]
    private float currentIntensity;
    [Tooltip("Target light intensity")]
    private float targetLight;
    [Tooltip("Time of the last switch")]
    private float timeLastSwitched;

    [Tooltip("Cycle duration")]
    private float cycleDuration;
    [Tooltip("Variance")]
    private float variance;
    [Tooltip("Step size for changing intensity")]
    private float stepSize;

    [Tooltip("If the light is brightening or dimming")]
    private bool brightening = true;

    [Header("Settings for the torch when the door is open")]
    [SerializeField, Tooltip("Enable changing the light color when the door is open")]
    private bool changeLightColorAfterDoorOpened = true;
    [SerializeField,Tooltip("The color of the torch when the door is open"), ShowIf(nameof(changeLightColorAfterDoorOpened))]
    private Color colorAfterDoorOpened;
    [SerializeField,Tooltip("The range of the torch when the door is open"), ShowIf(nameof(changeLightColorAfterDoorOpened))]
    private float rangeAfterDoorOpened = -1;
    [SerializeField,Tooltip("The temperature of the torch when the door is open"), ShowIf(nameof(changeLightColorAfterDoorOpened))]
    private float temperatureAfterDoorOpened;
    [SerializeField,Tooltip("Minimum intensity of the light"), ShowIf(nameof(changeLightColorAfterDoorOpened))]
    public float minimumIntensityAfterDoorOpened;
    [SerializeField,Tooltip("Maximum intensity of the light"), ShowIf(nameof(changeLightColorAfterDoorOpened))]
    public float maximumIntensityAfterDoorOpened;
    [SerializeField,Tooltip("Minimum variance of the light"), ShowIf(nameof(changeLightColorAfterDoorOpened))]
    public float minimumVarianceAfterDoorOpened;
    [SerializeField,Tooltip("Maximum variance of the light"), ShowIf(nameof(changeLightColorAfterDoorOpened))]
    public float maximumVarianceAfterDoorOpened;
    [SerializeField,Tooltip("Minimum cycle duration of the light"), ShowIf(nameof(changeLightColorAfterDoorOpened))]
    public float minimumCycleDurationAfterDoorOpened;
    [SerializeField,Tooltip("Maximum cycle duration of the light"), ShowIf(nameof(changeLightColorAfterDoorOpened))]
    public float maximumCycleDurationAfterDoorOpened;
    [Dropdown(nameof(GetRoomControllersDropdown)),SerializeField,Tooltip("The room nmae that the torch is in"), ShowIf(nameof(changeLightColorAfterDoorOpened))]
    private RoomController roomController;
    [SerializeField, Tooltip("The hieght of the room (to find the room controller)"), ShowIf(nameof(dev))]
    private float yTolerance = 10f;
    [Tooltip("The original light color before the door is opened")]
    private Color originalLightColor;
    [Tooltip("The original light range before the door is opened")]
    private float originalLightTemperature;
    [Tooltip("The original light range before the door is opened")]
    private float originalLightRange;
    private void Start()
    {
        torchLight = GetComponent<Light>();
        

        currentIntensity = Random.Range(minimumIntensity, maximumIntensity);
        torchLight.intensity = currentIntensity;

        // Store the original light settings, so we can restore it when the door is closed again 
        originalLightColor = torchLight.color;
        originalLightTemperature = torchLight.colorTemperature;
        originalLightRange = torchLight.range;

        SetLightTarget();
        // We find the room controller if it's null
        // if the room controller is not null, we don't need to find it (since this is overriden in the inspector)
        if (roomController == null && changeLightColorAfterDoorOpened)
        {
            roomController = RoomSystem.Instance.GetRoomFromCoordinates(gameObject.transform.position, yTolerance: yTolerance);
        }

        if (roomController != null)
        {
            roomController.AddDoor(this);
        }
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

    /// <summary>
    /// It sets the light target based on the current intensity and the cycle duration
    /// </summary>
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

    /// <summary>
    /// This is called when the door of the room is locked, it will change the light color and range if the changeLightColorAfterDoorOpened variable is true
    /// </summary>
    public void Lock()
    {
        if (changeLightColorAfterDoorOpened)
        {
            torchLight.color = originalLightColor;
            torchLight.range = originalLightRange;
            torchLight.colorTemperature = originalLightTemperature;
        }
    }
    /// <summary>
    /// This is called when the door of the room is unlocked, it will change the light color and range if the changeLightColorAfterDoorOpened variable is true
    /// </summary>
    public void Unlock()
    {
        if (changeLightColorAfterDoorOpened)
        {
            torchLight.color = colorAfterDoorOpened;
            torchLight.range = rangeAfterDoorOpened;
            torchLight.colorTemperature = temperatureAfterDoorOpened;
            minimumIntensity = minimumIntensityAfterDoorOpened;
            maximumIntensity = maximumIntensityAfterDoorOpened;
            minimumVariance = minimumVarianceAfterDoorOpened;
            maximumVariance = maximumVarianceAfterDoorOpened;
            minimumCycleDuration = minimumCycleDurationAfterDoorOpened;
            maximumCycleDuration = maximumCycleDurationAfterDoorOpened;
        }
    }
    /// <summary>
    /// This is a dropdown list of all the room controllers in the scene
    /// This is used to select the room controller that the torch will use in the inspector
    /// </summary>
    /// <returns>Dropdown list of all room controllers</returns>
    private DropdownList<RoomController> GetRoomControllersDropdown()
    {
        DropdownList<RoomController> allRooms = new DropdownList<RoomController>();

        string roomNameInTorch = "Not found";

        
        RoomController[] roomControllers = FindObjectsOfType<RoomController>(true);
        // Alright, I had to do two loops, one to find the room name that the torch is in, and one to find all room controllers 
        // beacuse the stupid DropdownList doesn't have a way to add a new item at specific index... 
        // but this function is called only in the editro mode so it's fine
        foreach (RoomController roomController in roomControllers)
        {
            if (roomController.IsObjectInsideRoom(this.gameObject.transform.position, yTolerance: yTolerance))
            {
                roomNameInTorch = roomController.gameObject.name;
                break;
            }
        }

        allRooms.Add($"Use the room that the torch is in ({roomNameInTorch})", null);

        foreach (RoomController roomController in roomControllers)
        {
            if (roomController != null)
            {
                allRooms.Add(roomController.gameObject.name, roomController);
            }
        }
        return allRooms;
    }

    /// <summary>
    /// This is called when the script is added to the light game object
    /// It will copy the light settings to the color and range variables in the script
    /// </summary>
    [Button("Import torch values from Light Component"), ShowIf(nameof(changeLightColorAfterDoorOpened))]
    private void Reset()
    {
        torchLight = GetComponent<Light>();
        colorAfterDoorOpened = torchLight.color;
        rangeAfterDoorOpened = torchLight.range;
        temperatureAfterDoorOpened = torchLight.colorTemperature;
        minimumIntensityAfterDoorOpened = minimumIntensity;
        maximumIntensityAfterDoorOpened = maximumIntensity;
        minimumVarianceAfterDoorOpened = minimumVariance;
        maximumVarianceAfterDoorOpened = maximumVariance;
        minimumCycleDurationAfterDoorOpened = minimumCycleDuration;
        maximumCycleDurationAfterDoorOpened = maximumCycleDuration;
    }
}
