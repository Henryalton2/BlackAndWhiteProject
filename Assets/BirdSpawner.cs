using UnityEngine;

public class BirdSpawner : MonoBehaviour
{
    [Header("References")]
    public GameObject birdPrefab;
    public Camera mainCamera;

    [Header("Spawn Settings")]
    [Tooltip("Distance in front of the camera/player to spawn the bird")]
    [Range(10f, 100f)]
    public float spawnDistance = 40f; // forward from camera

    [Tooltip("Random variation added to spawnDistance")]
    public float spawnDistanceVariation = 5f; // +/- variation

    public float minHeight = 2f;
    public float maxHeight = 5f;

    [Header("Flight Settings")]
    public float minSpeed = 4f;
    public float maxSpeed = 8f;

    [Header("Offscreen Settings")]
    [Tooltip("Extra units to move bird offscreen to the left")]
    public float offscreenExtra = 1f;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            SpawnBird();
        }
    }

    public void SpawnBird()
    {
        if (birdPrefab == null || mainCamera == null)
        {
            Debug.LogError("BirdSpawner missing references! Assign Bird Prefab and Main Camera.");
            return;
        }

        Vector3 camPos = mainCamera.transform.position;

        // Forward distance with variation
        float distanceOffset = Random.Range(-spawnDistanceVariation, spawnDistanceVariation);
        float actualSpawnDistance = spawnDistance + distanceOffset;
        Vector3 forwardOffset = mainCamera.transform.forward * actualSpawnDistance;

        // Calculate camera frustum width at spawn distance
        float frustumHeight = 2f * actualSpawnDistance * Mathf.Tan(mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float frustumWidth = frustumHeight * mainCamera.aspect;

        // Left offset = beyond left edge of camera view
        Vector3 leftOffset = -mainCamera.transform.right * (frustumWidth / 2f + offscreenExtra);

        // Combine offsets
        Vector3 spawnPos = camPos + forwardOffset + leftOffset;

        // Random vertical position
        spawnPos.y = Random.Range(minHeight + camPos.y, maxHeight + camPos.y);

        // Fly direction = camera right
        Vector3 flyDir = mainCamera.transform.right;

        // Spawn bird
        GameObject birdObj = Instantiate(birdPrefab, spawnPos, Quaternion.identity);

        // Add BirdFly
        BirdFly fly = birdObj.AddComponent<BirdFly>();
        float speed = Random.Range(minSpeed, maxSpeed);
        fly.baseSpeed = speed;
        fly.Init(flyDir, 1f, 0);

        Debug.Log($"Spawned bird at {spawnPos}, flying right at speed {speed}, spawnDistance={actualSpawnDistance}");
    }
}
