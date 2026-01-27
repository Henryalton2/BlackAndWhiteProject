using UnityEngine;

public class BirdSpawner : MonoBehaviour
{
    [Header("References")]
    public GameObject birdPrefab;
    public Camera mainCamera;

    [Header("Spawn Settings")]
    [Tooltip("Base distance in front of the camera/player to spawn the bird")]
    [Range(10f, 100f)]
    public float spawnDistance = 40f;

    [Tooltip("Random variation added to spawnDistance (+/-)")]
    public float spawnDistanceVariation = 5f;

    public float minHeight = 2f;
    public float maxHeight = 5f;

    [Header("Flight Settings")]
    public float minSpeed = 4f;
    public float maxSpeed = 8f;

    [Header("Offscreen Settings")]
    [Tooltip("Extra units to move bird offscreen to the left")]
    public float offscreenExtra = 1f;

    [Header("Flap Variation")]
    [Tooltip("Random animation start offset (0–1)")]
    public float maxFlapOffset = 1f;

    [Tooltip("Random speed variation for flapping")]
    public float flapSpeedVariation = 0.2f;

    void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    void Update()
    {
        // Optional debug key to spawn birds manually
        if (Input.GetKeyDown(KeyCode.B))
        {
            SpawnBird();
        }
    }

    /// <summary>
    /// Spawns a single bird with optional spawn distance variation
    /// </summary>
    public GameObject SpawnBird(float distanceVariation = 0f)
    {
        if (birdPrefab == null || mainCamera == null)
        {
            Debug.LogError("BirdSpawner missing references! Assign Bird Prefab and Main Camera.");
            return null;
        }

        Vector3 camPos = mainCamera.transform.position;

        // Forward distance with variation
        float distanceOffset = Random.Range(-spawnDistanceVariation, spawnDistanceVariation) + Random.Range(-distanceVariation, distanceVariation);
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
        BirdFly fly = birdObj.GetComponent<BirdFly>();
        if (fly == null)
            fly = birdObj.AddComponent<BirdFly>();

        float speed = Random.Range(minSpeed, maxSpeed);
        fly.baseSpeed = speed;
        fly.Init(flyDir, 1f, 0);

        // FLAP DESYNC
        Animator anim = birdObj.GetComponent<Animator>();
        if (anim != null)
        {
            float randomOffset = Random.Range(0f, maxFlapOffset);
            anim.Play(0, 0, randomOffset);

            float speedVariation = Random.Range(1f - flapSpeedVariation, 1f + flapSpeedVariation);
            anim.speed = speedVariation;
        }

        Debug.Log($"Spawned bird at {spawnPos}, flying right at speed {speed}, spawnDistance={actualSpawnDistance}");
        return birdObj;
    }
}
