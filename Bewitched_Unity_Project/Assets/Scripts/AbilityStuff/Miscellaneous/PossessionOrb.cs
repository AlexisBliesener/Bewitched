using System.Collections;
using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public class PossessionOrb : MonoBehaviour
{
    //Reference to the character class who shot this orb
    public Character player;
    float spawnTime;
    //The FMODEvent playing that's associated with this object
    EventInstance orbEvent;

    public void Init(Vector3 velocity, Character caller){
        player = caller;
        GetComponent<Rigidbody>().velocity = velocity;
        spawnTime = Time.time;
        if(AudioManager.TryGetReference("PossessionOrb",out EventReference eventRef)){
            orbEvent = RuntimeManager.CreateInstance(eventRef);
            RuntimeManager.AttachInstanceToGameObject(orbEvent,gameObject);
            orbEvent.start();
            orbEvent.release();
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        if(other.TryGetComponent(out Character character) && character!=player){
            PlayerController.CharacterControlChangeEvent?.Invoke(character);
            character.SetControlled(true);
            //Ends the audio event
            orbEvent.setParameterByName("Hit",1f);
            Destroy(gameObject);
        }
        else if(other.gameObject.layer == 8) {
            orbEvent.setParameterByName("Hit",2f);
            Destroy(gameObject);
        }


    }
    
    //For testing
    void Update()
    {
        if(Time.time-spawnTime > 4){ 
            orbEvent.setParameterByName("Hit",2f);
            Destroy(gameObject);
        }
    }
}
