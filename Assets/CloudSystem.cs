using UnityEngine;
using System.Collections.Generic;

public class CloudSystem : MonoBehaviour
{
    [Header("Cloud Settings")]
    public GameObject cloudPrefab;

    [Range(1, 300)]
    public int cloudCount = 40;

    public float baseSpeed = 5f;
    public Vector2 speedVariation = new Vector2(0.8f, 1.2f);

    [Header("Spacing Controls")]
    [Tooltip("Higher values = clouds are further apart")]
    public float spacing = 10f;

    [Tooltip("Random positional offset to break grid alignment")]
    public float spacingOffset = 3f;

    [Header("Fade Settings")]
    [Tooltip("Distance from box edge where clouds fade out")]
    public float edgeFadeDistance = 15f;

    [Header("Spawn Area")]
    public Vector3 spawnArea = new Vector3(100, 50, 100);
    public Vector3 windDirection = Vector3.right;

    private readonly List<GameObject> clouds = new();
    private readonly List<float> cloudSpeeds = new();
    private readonly List<SpriteRenderer> renderers = new();

    void Start()
    {
        if (!cloudPrefab)
        {
            Debug.LogError("Cloud prefab not assigned!");
            return;
        }

        SpawnClouds();
    }

    void Update()
    {
        MoveClouds();
        WrapClouds();
        UpdateAlphaFade();
    }

    void SpawnClouds()
    {
        clouds.Clear();
        cloudSpeeds.Clear();
        renderers.Clear();

        int gridX = Mathf.CeilToInt(spawnArea.x / spacing);
        int gridZ = Mathf.CeilToInt(spawnArea.z / spacing);

        int spawned = 0;

        for (int x = 0; x < gridX && spawned < cloudCount; x++)
        {
            for (int z = 0; z < gridZ && spawned < cloudCount; z++)
            {
                Vector3 pos = transform.position + new Vector3(
                    -spawnArea.x / 2 + x * spacing + Random.Range(-spacingOffset, spacingOffset),
                    Random.Range(0f, spawnArea.y),
                    -spawnArea.z / 2 + z * spacing + Random.Range(-spacingOffset, spacingOffset)
                );

                GameObject cloud = Instantiate(cloudPrefab, pos, Quaternion.identity, transform);
                clouds.Add(cloud);

                SpriteRenderer sr = cloud.GetComponent<SpriteRenderer>();
                renderers.Add(sr);

                float heightT = Mathf.InverseLerp(
                    transform.position.y,
                    transform.position.y + spawnArea.y,
                    pos.y
                );

                float speed = baseSpeed * Mathf.Lerp(speedVariation.x, speedVariation.y, heightT);
                cloudSpeeds.Add(speed);

                spawned++;
            }
        }
    }

    void MoveClouds()
    {
        Vector3 dir = windDirection.normalized;

        for (int i = 0; i < clouds.Count; i++)
        {
            if (!clouds[i]) continue;
            clouds[i].transform.position += dir * cloudSpeeds[i] * Time.deltaTime;
        }
    }

    void WrapClouds()
    {
        Vector3 center = transform.position;
        Vector3 half = spawnArea * 0.5f;
        Vector3 dir = windDirection.normalized;

        for (int i = 0; i < clouds.Count; i++)
        {
            if (!clouds[i]) continue;

            Vector3 local = clouds[i].transform.position - center;

            if (dir.x != 0)
            {
                if (local.x > half.x) local.x -= spawnArea.x;
                else if (local.x < -half.x) local.x += spawnArea.x;
            }

            if (dir.z != 0)
            {
                if (local.z > half.z) local.z -= spawnArea.z;
                else if (local.z < -half.z) local.z += spawnArea.z;
            }

            clouds[i].transform.position = center + local;
        }
    }

    void UpdateAlphaFade()
    {
        Vector3 center = transform.position;
        Vector3 half = spawnArea * 0.5f;

        for (int i = 0; i < clouds.Count; i++)
        {
            if (!renderers[i]) continue;

            Vector3 local = clouds[i].transform.position - center;

            float fadeX = Mathf.InverseLerp(half.x, half.x - edgeFadeDistance, Mathf.Abs(local.x));
            float fadeZ = Mathf.InverseLerp(half.z, half.z - edgeFadeDistance, Mathf.Abs(local.z));

            float alpha = Mathf.Clamp01(Mathf.Min(fadeX, fadeZ));

            Color c = renderers[i].color;
            c.a = alpha;
            renderers[i].color = c;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 1, 0.2f);
        Gizmos.DrawCube(transform.position + Vector3.up * spawnArea.y / 2f, spawnArea);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position + Vector3.up * spawnArea.y / 2f, spawnArea);
    }
}
