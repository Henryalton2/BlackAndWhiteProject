using UnityEngine;
using System.Collections.Generic;

public class CloudSystem : MonoBehaviour
{
    [Header("Cloud Prefab")]
    public GameObject cloudPrefab;

    [Header("Spawn Area")]
    public Vector3 spawnArea = new Vector3(100, 40, 100);

    [Header("Movement")]
    public Vector3 windDirection = Vector3.right;
    public float baseSpeed = 5f;
    public Vector2 speedVariation = new Vector2(0.8f, 1.2f);

    [Header("Spacing")]
    public float spacing = 15f;
    public float spacingOffset = 3f;

    [Header("Edge Fade")]
    [Tooltip("Distance from edge where clouds begin fading")]
    public float edgeFadeDistance = 15f;

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
        UpdateEdgeFade();
    }

    void SpawnClouds()
    {
        clouds.Clear();
        cloudSpeeds.Clear();
        renderers.Clear();

        int countX = Mathf.FloorToInt(spawnArea.x / spacing);
        int countZ = Mathf.FloorToInt(spawnArea.z / spacing);

        Vector3 center = transform.position;
        Vector3 half = spawnArea * 0.5f;

        for (int x = 0; x < countX; x++)
        {
            for (int z = 0; z < countZ; z++)
            {
                Vector3 pos = center + new Vector3(
                    -half.x + spacing * 0.5f + x * spacing + Random.Range(-spacingOffset, spacingOffset),
                    Random.Range(0f, spawnArea.y),
                    -half.z + spacing * 0.5f + z * spacing + Random.Range(-spacingOffset, spacingOffset)
                );

                GameObject cloud = Instantiate(cloudPrefab, pos, Quaternion.identity, transform);
                clouds.Add(cloud);

                SpriteRenderer sr = cloud.GetComponent<SpriteRenderer>();
                renderers.Add(sr);

                float heightT = pos.y / spawnArea.y;
                float speed = baseSpeed * Mathf.Lerp(speedVariation.x, speedVariation.y, heightT);
                cloudSpeeds.Add(speed);
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

        for (int i = 0; i < clouds.Count; i++)
        {
            if (!clouds[i]) continue;

            Vector3 local = clouds[i].transform.position - center;

            if (local.x > half.x) local.x -= spawnArea.x;
            else if (local.x < -half.x) local.x += spawnArea.x;

            if (local.z > half.z) local.z -= spawnArea.z;
            else if (local.z < -half.z) local.z += spawnArea.z;

            clouds[i].transform.position = center + local;
        }
    }

    void UpdateEdgeFade()
    {
        Vector3 center = transform.position;
        Vector3 half = spawnArea * 0.5f;

        for (int i = 0; i < clouds.Count; i++)
        {
            if (!renderers[i]) continue;

            Vector3 local = clouds[i].transform.position - center;

            float distToEdgeX = half.x - Mathf.Abs(local.x);
            float distToEdgeZ = half.z - Mathf.Abs(local.z);

            float distToEdge = Mathf.Min(distToEdgeX, distToEdgeZ);

            float alpha = Mathf.Clamp01(distToEdge / edgeFadeDistance);

            Color c = renderers[i].color;
            c.a = alpha;
            renderers[i].color = c;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 1, 0.15f);
        Gizmos.DrawCube(transform.position + Vector3.up * spawnArea.y / 2f, spawnArea);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position + Vector3.up * spawnArea.y / 2f, spawnArea);
    }
}
