using UnityEngine;
using FMODUnity;

[RequireComponent(typeof(Collider))]
public class TreeWindTrigger : MonoBehaviour
{
    [Tooltip("FMOD Event Emitter attached to this tree")]
    public StudioEventEmitter emitter;

    [Tooltip("Maximum random delay before starting the sound (seconds)")]
    public float maxRandomDelay = 0.6f;

    private void Reset()
    {
        // Auto-assign the emitter if on same GameObject
        if (emitter == null)
        {
            emitter = GetComponent<StudioEventEmitter>();
            if (emitter == null)
                Debug.LogWarning("TreeWindTrigger: No StudioEventEmitter found on this GameObject!", this);
        }

        // Ensure Collider is set as trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        
        if (!other.CompareTag("Player"))
            return;


        if (emitter != null && !emitter.IsPlaying())
        {
            // Random delay to offset multiple trees playing at once
            float delay = Random.Range(0f, maxRandomDelay);
            Invoke(nameof(PlayEmitter), delay);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (emitter != null)
        {
            emitter.Stop();
        }
    }

    private void PlayEmitter()
    {
        if (emitter != null && !emitter.IsPlaying())
        {
            emitter.Play();
        }
    }
}
