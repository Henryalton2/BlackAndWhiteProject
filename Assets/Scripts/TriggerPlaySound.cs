using UnityEngine;
using FMODUnity;

public class TriggerPlaySound : MonoBehaviour
{
    [Header("FMOD Event to play")]
    public EventReference fmodEvent; // drag your FMOD event here

    private bool hasPlayed = false; // ensures it only plays once

    void OnTriggerEnter(Collider other)
    {
        // Only trigger on player
        if (!hasPlayed && other.CompareTag("Player"))
        {
            hasPlayed = true;

            // Play the FMOD event at the player's position
            RuntimeManager.PlayOneShot(fmodEvent, other.transform.position);

            Debug.Log("Played FMOD event: " + fmodEvent);
        }
    }
}
