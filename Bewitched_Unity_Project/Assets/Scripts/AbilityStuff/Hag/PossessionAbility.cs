using FMOD.Studio;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static PlayerController;


/// <summary>
/// Handles the player�s possession ability, allowing the Hag character to
/// possess enemies, manage cooldowns, update the UI, and switch between
/// controlled characters.
/// </summary>
public class PossessionAbility : MonoBehaviour
{
    public static PlayerControlHandler CharacterControlChangeEvent;
    const string FILE_ENDING = ".json";
    [SerializeField, Tooltip("The cooldown in seconds that the player must wait in witch state before being able to possess again")]
    float possessionCooldown = 10;
    [SerializeField, Tooltip("The max distance away from the camera that the player can possess")]
    protected float maxPossessionDistance;
    [SerializeField, Tooltip("Possession Orb Cooldown UI")]
    private CooldownDisplay possessionCooldownDisplay;
    [SerializeField, Tooltip("Rate at which life is drained"), Range(0,100)]
    private float lifeDrainPercentage = 2;
    [SerializeField, Tooltip("The currently controlled character's health bar")]
    private GameObject secondaryHealthBar;
    [SerializeField, Tooltip("Hag script on the witch gameobject")]
    protected Hag oldHag;
    [SerializeField, Tooltip("Crosshair image component")]
    private Image crossHair;

    [Header("VFX")]
    [SerializeField, Tooltip("Highlights the enemy that is currently being targeted by possession")]
    private GameObject targetVFX;
    [SerializeField, Tooltip("Prefab of firing possession VFX")]
    private GameObject firingVFX;
    [SerializeField, Tooltip("Prefab of smoke cloud around enemy that got possessed")]
    private GameObject smokeCloudVFX;

    [Tooltip("The time possession was left")]
    private float timePossessionLastLeft = Mathf.NegativeInfinity;
    [Tooltip("The time when possession of the current enemy started")]
    private float timePossessing;
    [Tooltip("The current character that is possessed")]
    protected Character currentCharacter;

    [Tooltip("States to log if the player currently is in range of a possessable enemy and can possess or not")]
    private enum PossessionStates { canPossess, canNotPossess};
    [Tooltip("The players current possession state, if they have an available and legal possession to do or not")]
    private PossessionStates possessionState = PossessionStates.canNotPossess;
    [Tooltip("The current enemy that would be possessed if the ability is fired")]
    private Character currentPossessableEnemy = null;
    //The possession sound effect that is currently playing if any
    private EventInstance possessionSoundEffect;

    #region Saving/Loading

    [ContextMenu("Save to JSON")]
    public void SaveToJson()
    {
        string possessionStatsStr = JsonUtility.ToJson(this, true);

        string folderPath = Path.Combine(Application.dataPath, "JSON");
        folderPath = Path.Combine(folderPath, "PossessionAbility");
        SeeFilePath();
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string filePath = Path.Combine(folderPath, "PossessionAbility" + FILE_ENDING);
        File.WriteAllText(filePath, possessionStatsStr);


#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif


    }

    [ContextMenu("See File Path")]
    public void SeeFilePath()
    {
        string folderPath = Path.Combine(Application.persistentDataPath, "JSON");
        folderPath = Path.Combine(folderPath, "PossessionAbility");
        Debug.Log("Path To JSON File:");
        Debug.Log(folderPath);
    }

    [ContextMenu("Load From JSON")]
    public void LoadFromJson()
    {

        string folderPath = Path.Combine(Application.dataPath, "JSON");
        folderPath = Path.Combine(folderPath, "PossessionAbility");
        string filePath = Path.Combine(folderPath, "PossessionAbility" + FILE_ENDING);

        string jsonStr = File.ReadAllText(filePath);

        string[] jsons = jsonStr.Split("|");

        JsonUtility.FromJsonOverwrite(jsons[0], this);

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif

    }

    #endregion

    /// <summary>
    /// Initializes possession state and subscribes to character switching events.
    /// </summary>
    private void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
        currentCharacter = oldHag;
        CharacterControlChangeEvent += SwitchCharacter;
    }

    /// <summary>
    /// Unsubscribes from character switching events when disabled.
    /// </summary>
    void OnDisable()
    {
        CharacterControlChangeEvent -= SwitchCharacter;
    }

    /// <summary>
    /// Updates possession abilities, UI, and keeps the Hag's position aligned when possessing an enemy.
    /// </summary>
    private void Update()
    {
        UpdateCooldowns();
        UpdateState();
        UpdateCrossHair();
        UpdateTargetVFX();

        // Move hag to current characters position
        if (currentCharacter != oldHag)
        {
            oldHag.transform.position = currentCharacter.transform.position;
            oldHag.transform.rotation = currentCharacter.transform.rotation;
        }
    }

    /// <summary>
    /// Handles input for starting or ending possession.
    /// </summary>
    /// <param name="context">The input action callback context.</param>
    public void Possess(InputAction.CallbackContext context)
    {
        if (currentCharacter == oldHag)
        {
            if (context.started)
            {
                if (Time.time - timePossessionLastLeft >= possessionCooldown)
                {
                    timePossessionLastLeft = Time.time;
                    oldHag.AnimatePossess();
                    StartCoroutine(FirePossession());
                }
            }
            else
            {
                return;
            }
        }
        else
        {
            if (context.started)
            {
                if(!GrandFinale.instance.GetActive())
                {
                    // respawn old Hag
                    currentCharacter.SetControlled(false);
                    CharacterControlChangeEvent?.Invoke(oldHag);
                }
                else
                {
                   GrandFinale.instance.Explode(timePossessing, false);
                }

                timePossessionLastLeft = Time.time;
            }
            else
            {
                return;
            }
        }
    }

    /// <summary>
    /// Possesses an enemy if currently avaliable at the time of firing
    /// </summary>
    private IEnumerator FirePossession()
    {
        Character target = possessionState == PossessionStates.canPossess ? currentPossessableEnemy : null;
        if (!AudioManager.TryPlayInstance("Possession", out possessionSoundEffect, true, null))
        {
            Debug.LogError("Failed to play possession sound effect. Is it assigned in the ref sheet?");
        }
        yield return new WaitForSeconds(0.5f);
        if (target)
        {
            CharacterControlChangeEvent?.Invoke(currentPossessableEnemy);
            currentPossessableEnemy.SetControlled(true);

            // Possession smoke VFX
            if(smokeCloudVFX != null)
            {
                Instantiate(smokeCloudVFX, new Vector3(currentPossessableEnemy.transform.position.x,
                    currentPossessableEnemy.transform.position.y + currentPossessableEnemy.GetComponent<CharacterController>().height / 2,
                    currentPossessableEnemy.transform.position.z), currentPossessableEnemy.transform.rotation);
            }
            else
            {
                Debug.LogWarning("Smoke Cloud VFX is not assigned!");
            }

            if (possessionSoundEffect.isValid()) possessionSoundEffect.setParameterByName("Stage", 1);
            else Debug.LogError("Possession Sound Effect is not playing! Can't set param!");
        }
        else
        {
            //Possession miss currently not implemented
            if (possessionSoundEffect.isValid()) possessionSoundEffect.setParameterByName("Stage", 2);
            else Debug.LogError("Possession Sound Effect is not playing! Can't set param!");
        }

        // Possession fire VFX
        if(firingVFX != null)
        {
            Instantiate(firingVFX, oldHag.transform.position + new Vector3(oldHag.transform.forward.x, 1f, oldHag.transform.forward.z), oldHag.transform.rotation);
        }
        else
        {
            Debug.LogWarning("Firing Possession VFX is not assigned!");
        }
    }
    
    /// <summary>
    /// Updates the aim target VFX
    /// Places it on the enemy being targeted or disabled it if there is no enemy being targeted
    /// </summary>
    private void UpdateTargetVFX()
    {
        if(targetVFX != null)
        {
            if (possessionState == PossessionStates.canPossess)
            {
                targetVFX.SetActive(true);
                targetVFX.transform.position = new Vector3(currentPossessableEnemy.transform.position.x,
                    currentPossessableEnemy.transform.position.y + currentPossessableEnemy.GetComponent<CharacterController>().height / 2,
                    currentPossessableEnemy.transform.position.z); ;
            }
            else
            {
                targetVFX.SetActive(false);
            }
        }
        else
        {
            Debug.LogWarning("Target Possession VFX not assigned!");
        }
    }

    /// <summary>
    /// Updates the color of the cross hair baised on if the player can currently possess
    /// </summary>
    private void UpdateCrossHair()
    {
        if (crossHair == null)
        {
            Debug.LogWarning("Crosshair image is not assigned!");
            return;
        }

        if (possessionState == PossessionStates.canNotPossess)
        {
            crossHair.color = Color.white;
        }
        else
        {
            crossHair.color = Color.red;
        }
    }

    /// <summary>
    /// Updates the state of possession ability to tell if the player can currently possess or not
    /// </summary>
    private void UpdateState()
    {
        // Can only possess if the player is currently eleth
        if(currentCharacter != oldHag)
        {
            possessionState = PossessionStates.canNotPossess;
            return;
        }

        // Can only possess if the cooldown is over
        if(possessionCooldown - (Time.time - timePossessionLastLeft) > 0)
        {
            possessionState = PossessionStates.canNotPossess;
            return;
        }

        // Detect enemy for possession
        if(CameraController.GetIsAiming())
        {
            Ray possessionRay = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
            RaycastHit hitInfo;
            if (Physics.Raycast(possessionRay, out hitInfo, maxPossessionDistance))
            {
                if (hitInfo.collider.gameObject.CompareTag("Enemy"))
                {
                    currentPossessableEnemy = hitInfo.collider.gameObject.GetComponent<Character>();
                    possessionState = PossessionStates.canPossess;
                }
                else
                {
                    possessionState = PossessionStates.canNotPossess;
                }
            }
            else
            {
                possessionState = PossessionStates.canNotPossess;
            }
        }
        else
        {
            Ray possessionRay = new Ray(currentCharacter.transform.position - Vector3.up, currentCharacter.transform.forward);
            RaycastHit hitInfo;
            if (Physics.Raycast(possessionRay, out hitInfo, maxPossessionDistance))
            {
                if (hitInfo.collider.gameObject.CompareTag("Enemy"))
                {
                    currentPossessableEnemy = hitInfo.collider.gameObject.GetComponent<Character>();
                    possessionState = PossessionStates.canPossess;
                }
                else
                {
                    possessionState = PossessionStates.canNotPossess;
                }
            }
            else
            {
                possessionState = PossessionStates.canNotPossess;
            }
        }

    }

    /// <summary>
    /// Updates the possession cooldown UI display.
    /// </summary>
    private void UpdateCooldowns()
    {
        if (possessionCooldownDisplay == null)
        {
            Debug.LogWarning("Cooldown display is not assigned!");
            return;
        }

        if (currentCharacter == oldHag)
        {
            possessionCooldownDisplay.SetAbleToUse(true);
        }
        else
        {
            possessionCooldownDisplay.SetAbleToUse(false);
        }

        possessionCooldownDisplay.SetCooldownCover(possessionCooldown - (Time.time - timePossessionLastLeft));
    }

    /// <summary>
    /// Switches control between the Hag and a possessed character, updating health and UI accordingly.
    /// </summary>
    /// <param name="newCharacter">The new character to switch control to.</param>
    public void SwitchCharacter(Character newCharacter)
    {
        currentCharacter.DeactivateSurroundingPoints();
        currentCharacter.GetComponent<HealthController>().EnableUpdateModel(false);

        PlayerController.instance.SeteCharacterController(newCharacter.GetComponent<CharacterController>());
        HealthController hagHealth = oldHag.GetComponent<HealthController>();
        HealthController newHealth = newCharacter.GetComponent<HealthController>();
        if (newCharacter == oldHag)
        {
            if (hagHealth != null)
            {
                hagHealth.SetDecay(0f); // Hag does not decay
                hagHealth.EnableUpdateModel(true);
            }

            if (secondaryHealthBar != null)
            {
                oldHag.EnableEleth();
                secondaryHealthBar.SetActive(false);
            }
            currentCharacter.SetTeamID(2);
            PlayerController.instance.SetAllowMovement(true);
            AudioManager.TryPlayOneShot("LeaveBody");

            // Possession smoke VFX
            if (smokeCloudVFX != null)
            {
                Instantiate(smokeCloudVFX, new Vector3(oldHag.transform.position.x,
                    oldHag.transform.position.y + oldHag.GetComponent<CharacterController>().height / 2,
                    oldHag.transform.position.z), oldHag.transform.rotation);
            }
            else
            {
                Debug.LogWarning("Smoke Cloud VFX is not assigned!");
            }
        }
        else
        {
            // Possess an enemy
            if (hagHealth != null)
            {
                hagHealth.SetDecay(0f);
            }
            if (newHealth != null)
            {
                newHealth.SetDecay(lifeDrainPercentage);
                newHealth.EnableUpdateModel(true);
                if (secondaryHealthBar != null)
                {
                    secondaryHealthBar.GetComponent<HealthBar>().Subscribe(newHealth);
                    oldHag.DisableEleth();
                    secondaryHealthBar.SetActive(true);
                }
            }
            newCharacter.SetTeamID(1);
            timePossessing = Time.time;
        }
        timePossessionLastLeft = Time.time;
        currentCharacter = newCharacter;
        currentCharacter.ActivateSurroundingPoints();
        PlayerController.instance.currentCharacter = newCharacter;
    }
}
