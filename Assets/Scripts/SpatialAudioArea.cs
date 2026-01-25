using UnityEngine;
using FMODUnity;
using FMOD.Studio;

[RequireComponent(typeof(BoxCollider))]
public class SpatialAudioArea : MonoBehaviour
{
    [Header("FMOD Event")]
    public EventReference areaEvent;

    private EventInstance instance;
    private BoxCollider boxCollider;

    void Start()
    {
        boxCollider = GetComponent<BoxCollider>();
        boxCollider.isTrigger = true;

        // Create the 3D audio instance
        instance = RuntimeManager.CreateInstance(areaEvent);
        instance.set3DAttributes(RuntimeUtils.To3DAttributes(gameObject));
        instance.start();
    }

    void Update()
    {
        // Update 3D position if the GameObject moves
        if (instance.isValid())
        {
            instance.set3DAttributes(RuntimeUtils.To3DAttributes(gameObject));
        }
    }

    private void OnDestroy()
    {
        if (instance.isValid())
        {
            instance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT); // Fully qualified STOP_MODE
            instance.release();
        }
    }

    // Optional: detect player entering/exiting to trigger effects
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // You could scale trees or trigger visual reactions here
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Reset tree visuals if needed
        }
    }
}
