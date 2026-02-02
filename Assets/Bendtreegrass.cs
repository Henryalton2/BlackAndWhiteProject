using UnityEngine;
using System.Collections.Generic;

public class BillboardBend : MonoBehaviour
{
    private Vector3 originalPosition;
    private Transform cameraTransform;

    [Header("Fireplace Smoke Settings")]
    public bool isFireplace = false;
    public GameObject smokePrefab;
    public float smokeRiseSpeed = 1f;
    public float smokeGrowSpeed = 0.5f;
    public float smokeLifetime = 5f;
    public float spawnInterval = 1f;
    public Vector2 smokeSpawnOffset = new Vector2(0.2f, 0.2f);
    public float cloudSwayMultiplier = 0.1f; // Sway strength

    private float nextSpawnTime = 0f;

    private CloudSystem cloudSystem; // reference to the cloud system

    private class SmokePuffData
    {
        public GameObject go;
        public Vector3 originalPosition;
        public SpriteRenderer spriteRenderer;
        public float age = 0f;
    }

    private List<SmokePuffData> smokePuffs = new List<SmokePuffData>();

    void Start()
    {
        originalPosition = transform.position;
        cameraTransform = Camera.main.transform;

        // Find the first active CloudSystem in the scene
        cloudSystem = FindObjectOfType<CloudSystem>();
        if (cloudSystem == null)
        {
            Debug.LogWarning("No CloudSystem found in the scene. Smoke will not sway with clouds.");
        }
    }

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        // --- World Bend for parent object ---
        float bendAmount = WorldBendTrigger.CurrentBend;
        Vector3 camPos = cameraTransform.position;
        float dx = transform.position.x - camPos.x;
        float dz = transform.position.z - camPos.z;
        float distSq = dx * dx + dz * dz;

        Vector3 newPos = transform.position;
        newPos.y = originalPosition.y - distSq * bendAmount;
        transform.position = newPos;

        // --- Spawn smoke ---
        if (isFireplace && smokePrefab != null && Time.time >= nextSpawnTime)
        {
            SpawnSmoke();
            nextSpawnTime = Time.time + spawnInterval;
        }

        UpdateSmokePuffs(distSq, bendAmount);
    }

    private void SpawnSmoke()
    {
        Vector3 spawnPos = transform.position;
        spawnPos.x += Random.Range(-smokeSpawnOffset.x, smokeSpawnOffset.x);
        spawnPos.z += Random.Range(-smokeSpawnOffset.y, smokeSpawnOffset.y);

        GameObject smoke = Instantiate(smokePrefab, spawnPos, Quaternion.identity, transform);
        SpriteRenderer sr = smoke.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            Debug.LogWarning("Smoke prefab requires a SpriteRenderer.");
            return;
        }

        SmokePuffData puff = new SmokePuffData
        {
            go = smoke,
            originalPosition = spawnPos,
            spriteRenderer = sr,
            age = 0f
        };

        smokePuffs.Add(puff);
    }

    private void UpdateSmokePuffs(float distSq, float bendAmount)
    {
        Vector3 windDir = cloudSystem != null ? cloudSystem.windDirection.normalized : Vector3.zero;

        for (int i = smokePuffs.Count - 1; i >= 0; i--)
        {
            var puff = smokePuffs[i];
            puff.age += Time.deltaTime;

            // Remove if lifetime exceeded
            if (puff.age >= smokeLifetime)
            {
                Destroy(puff.go);
                smokePuffs.RemoveAt(i);
                continue;
            }

            // --- Position: world bend + rise + cloud sway ---
            Vector3 pos = puff.originalPosition;

            // Apply world bend
            pos.y -= distSq * bendAmount;

            // Add smoke rise
            pos.y += smokeRiseSpeed * puff.age;

            // Add cloud sway proportional to height
            pos += windDir * cloudSwayMultiplier * puff.age;

            puff.go.transform.position = pos;

            // --- Uniform scale ---
            float scale = 1f + smokeGrowSpeed * puff.age;
            puff.go.transform.localScale = Vector3.one * scale;

            // --- Smooth fade ---
            Color c = puff.spriteRenderer.color;
            c.a = Mathf.Lerp(1f, 0f, puff.age / smokeLifetime);
            puff.spriteRenderer.color = c;
        }
    }
}
