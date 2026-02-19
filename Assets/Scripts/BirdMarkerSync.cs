using UnityEngine;

/// <summary>
/// Add to the same triggerbox as TriggerPlaySound.
/// Spawns musical birds with their own prefab when the FMOD marker is hit.
/// </summary>
public class BirdMarkerSync : MonoBehaviour
{
    [Header("Spawner Reference")]
    public BirdSpawner birdSpawner;

    [Header("Spawn Settings")]
    public int birdsToSpawn = 1;
    public float spawnDistanceVariationPerBird = 5f;

    [Header("Musical Bird Prefab")]
    [Tooltip("Bird prefab with the chirp StudioEventEmitter. Leave empty to use BirdSpawner's default.")]
    public GameObject musicalBirdPrefab;

    void OnEnable() => TriggerPlaySound.OnBirdMarker += HandleMarker;
    void OnDisable() => TriggerPlaySound.OnBirdMarker -= HandleMarker;

    private void HandleMarker()
    {
        if (birdSpawner == null)
        {
            Debug.LogError("BirdMarkerSync: No BirdSpawner assigned!");
            return;
        }

        for (int i = 0; i < birdsToSpawn; i++)
        {
            GameObject birdObj = birdSpawner.SpawnBird(spawnDistanceVariationPerBird, musicalBirdPrefab);
            if (birdObj != null)
            {
                BirdFly fly = birdObj.GetComponent<BirdFly>();
                if (fly != null)
                    fly.StartSoundAfterDelay(0f);
            }
        }

        Debug.Log($"BirdMarkerSync: Spawned {birdsToSpawn} musical birds.");
    }
}