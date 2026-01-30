using UnityEngine;
using System.Collections.Generic;

public class FireSmokeSpawner : MonoBehaviour
{
    [Header("Smoke Prefab")]
    public GameObject smokePrefab;

    [Header("Smoke Settings")]
    public float riseSpeed = 0.4f;          // normal vertical rise
    public float windMultiplier = 0.1f;     // horizontal wind effect
    public float turbulenceStrength = 0.15f;// horizontal jitter
    public float lifetime = 3f;             // smoke lifetime
    public float scaleGrowSpeed = 0.2f;     // smoke scale growth
    public Vector2 spawnOffset = new Vector2(0.15f, 0.05f);
    public float spawnInterval = 0.8f;      // time between smoke poofs

    [Header("Rise Fade Settings")]
    public float riseFadeDuration = 1.0f;   // time to resume rising after bend

    [Header("Bend Settings")]
    public float bendScale = 10f;           // how strongly smoke bends during recovery

    [Header("Pooling Settings")]
    public int poolSize = 20;

    private CloudSystem cloudSystem;
    private float spawnTimer;

    // Object pool
    private readonly Queue<GameObject> smokePool = new();
    private readonly List<GameObject> activeSmoke = new();
    private readonly List<float> smokeTimers = new();
    private readonly List<float> noiseSeeds = new();
    private readonly List<float> riseFadeTimers = new();

    private float lastBendAmount = 0f;

    void Start()
    {
        cloudSystem = FindObjectOfType<CloudSystem>();

        // Pre-fill pool
        for (int i = 0; i < poolSize; i++)
        {
            GameObject smoke = Instantiate(smokePrefab, transform.position, Quaternion.identity);
            smoke.SetActive(false);
            smokePool.Enqueue(smoke);
        }
    }

    void Update()
    {
        spawnTimer += Time.deltaTime;

        // Spawn new smoke poofs
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            SpawnSmoke();
        }

        // Detect bend recovery
        float currentBend = WorldBendTrigger.CurrentBend;
        bool isRecovering = currentBend < lastBendAmount && currentBend > 0f;
        lastBendAmount = currentBend;

        // Update each smoke poof
        for (int i = activeSmoke.Count - 1; i >= 0; i--)
        {
            GameObject smoke = activeSmoke[i];
            if (!smoke) { RemoveSmoke(i); continue; }

            smokeTimers[i] += Time.deltaTime;
            float t = smokeTimers[i];

            // Smooth rise fade after recovery
            if (!isRecovering)
            {
                riseFadeTimers[i] += Time.deltaTime / riseFadeDuration;
                riseFadeTimers[i] = Mathf.Clamp01(riseFadeTimers[i]);
            }

            // Calculate vertical movement
            Vector3 movement;

            if (isRecovering)
            {
                // Bend smoke along Y axis instead of rising
                float bendY = -currentBend * bendScale;
                movement = new Vector3(0f, bendY, 0f);
            }
            else
            {
                // Normal vertical rise
                movement = Vector3.up * riseSpeed * riseFadeTimers[i];
            }

            // Add horizontal wind + turbulence
            if (cloudSystem)
            {
                Vector3 windDir = cloudSystem.windDirection.normalized;
                float windStrength = cloudSystem.baseSpeed * windMultiplier;
                float noise = Mathf.PerlinNoise(noiseSeeds[i], Time.time * 0.4f) - 0.5f;
                Vector3 turbulence = windDir * noise * turbulenceStrength;
                movement += windDir * windStrength + turbulence;
            }

            smoke.transform.position += movement * Time.deltaTime;

            // Scale up
            smoke.transform.localScale += Vector3.one * scaleGrowSpeed * Time.deltaTime;

            // Fade out
            SpriteRenderer sr = smoke.GetComponent<SpriteRenderer>();
            if (sr)
            {
                float alpha = Mathf.Lerp(1f, 0f, t / lifetime);
                Color c = sr.color;
                c.a = alpha;
                sr.color = c;
            }

            // Return to pool when lifetime exceeded
            if (t >= lifetime)
                RemoveSmoke(i);
        }
    }

    private void SpawnSmoke()
    {
        GameObject smoke;
        if (smokePool.Count > 0)
        {
            smoke = smokePool.Dequeue();
            smoke.SetActive(true);
        }
        else
        {
            smoke = Instantiate(smokePrefab);
        }

        Vector3 offset = new Vector3(
            Random.Range(-spawnOffset.x, spawnOffset.x),
            Random.Range(0f, spawnOffset.y),
            0f
        );

        smoke.transform.position = transform.position + offset;
        smoke.transform.localScale = Vector3.one;

        activeSmoke.Add(smoke);
        smokeTimers.Add(0f);
        noiseSeeds.Add(Random.Range(0f, 1000f));
        riseFadeTimers.Add(1f); // start fully rising
    }

    private void RemoveSmoke(int index)
    {
        GameObject smoke = activeSmoke[index];
        if (smoke)
        {
            smoke.SetActive(false);
            smokePool.Enqueue(smoke);
        }

        activeSmoke.RemoveAt(index);
        smokeTimers.RemoveAt(index);
        noiseSeeds.RemoveAt(index);
        riseFadeTimers.RemoveAt(index);
    }
}
