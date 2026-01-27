using UnityEngine;

public class BirdTrigger : MonoBehaviour
{
    [Header("Spawner Reference")]
    public BirdSpawner birdSpawner;

    [Header("Trigger Settings")]
    public int birdsToSpawn = 1;
    public bool singleUse = true;

    [Tooltip("Maximum additional distance variation per bird")]
    public float spawnDistanceVariationPerBird = 5f;

    [Tooltip("Seconds before the bird makes its first chirp")]
    [Range(0f, 20f)]
    public float chirpDelay = 10f;

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered && singleUse) return;

        if (other.CompareTag("Player"))
        {
            triggered = true;

            if (birdSpawner == null)
            {
                Debug.LogError("BirdTrigger: No BirdSpawner assigned!");
                return;
            }

            for (int i = 0; i < birdsToSpawn; i++)
            {
                GameObject birdObj = birdSpawner.SpawnBird(spawnDistanceVariationPerBird);
                if (birdObj != null)
                {
                    BirdFly fly = birdObj.GetComponent<BirdFly>();
                    if (fly != null)
                    {
                        fly.StartSoundAfterDelay(chirpDelay);
                    }
                }
            }

            Debug.Log($"BirdTrigger: Spawned {birdsToSpawn} birds for {other.name}");
        }
    }
}
