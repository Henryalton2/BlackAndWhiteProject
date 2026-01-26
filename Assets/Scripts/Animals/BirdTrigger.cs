using UnityEngine;

public class BirdTrigger : MonoBehaviour
{
    [Header("Spawner Reference")]
    public BirdSpawner birdSpawner;      // Drag your BirdSpawner here

    [Header("Trigger Settings")]
    public int birdsToSpawn = 1;         // How many birds spawn when player enters
    public bool singleUse = true;        // Should trigger only once?

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        // If already triggered and single-use, do nothing
        if (triggered && singleUse) return;

        // Only respond to the player
        if (other.CompareTag("Player"))
        {
            triggered = true;

            if (birdSpawner == null)
            {
                Debug.LogError("BirdTrigger: No BirdSpawner assigned!");
                return;
            }

            // Spawn the specified number of birds
            for (int i = 0; i < birdsToSpawn; i++)
            {
                birdSpawner.SpawnBird();
            }

            Debug.Log($"BirdTrigger: Spawned {birdsToSpawn} birds for {other.name}");
        }
    }
}
