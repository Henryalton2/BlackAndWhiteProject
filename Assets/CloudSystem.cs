using UnityEngine;
using System.Collections.Generic;

public class CloudSystem : MonoBehaviour
{
    [Header("Cloud Settings")]
    public GameObject cloudPrefab;          // Assign your cloud prefab
    public float baseSpeed = 5f;            // Base speed for movement
    public float spacing = 20f;             // Rough spacing
    [Range(0f, 1f)]
    public float density = 0.5f;            // Cloud density
    public Vector2 speedVariation = new Vector2(0.8f, 1.2f); // Min/max multiplier for speed

    [Header("Spawn Area")]
    public Vector3 spawnArea = new Vector3(100, 50, 100);    // Width, height, depth
    public Vector3 windDirection = new Vector3(1, 0, 0);     // Base direction clouds move

    private List<GameObject> clouds = new List<GameObject>();
    private float[] cloudSpeeds;  // Speed per cloud

    void Start()
    {
        if (cloudPrefab == null)
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
    }

    void SpawnClouds()
    {
        int maxClouds = Mathf.RoundToInt((spawnArea.x / spacing) * (spawnArea.z / spacing) * density);
        maxClouds = Mathf.Max(maxClouds, 1);

        clouds.Clear();
        cloudSpeeds = new float[maxClouds];

        for (int i = 0; i < maxClouds; i++)
        {
            Vector3 pos = new Vector3(
                transform.position.x - spawnArea.x / 2 + Random.Range(0f, spawnArea.x),
                transform.position.y + Random.Range(0f, spawnArea.y),
                transform.position.z - spawnArea.z / 2 + Random.Range(0f, spawnArea.z)
            );

            GameObject cloud = Instantiate(cloudPrefab, pos, Quaternion.identity, transform);
            clouds.Add(cloud);

            // Speed depends on height: higher clouds move slightly faster/slower
            float heightNormalized = (pos.y - transform.position.y) / spawnArea.y;
            cloudSpeeds[i] = baseSpeed * Mathf.Lerp(speedVariation.x, speedVariation.y, heightNormalized);
        }
    }

    void MoveClouds()
    {
        Vector3 dir = windDirection.normalized;
        for (int i = 0; i < clouds.Count; i++)
        {
            if (clouds[i] != null)
            {
                clouds[i].transform.position += dir * cloudSpeeds[i] * Time.deltaTime;
            }
        }
    }

    void WrapClouds()
    {
        for (int i = 0; i < clouds.Count; i++)
        {
            if (clouds[i] == null) continue;

            Vector3 pos = clouds[i].transform.position;

            if (pos.x > transform.position.x + spawnArea.x / 2)
                pos.x = transform.position.x - spawnArea.x / 2;
            else if (pos.x < transform.position.x - spawnArea.x / 2)
                pos.x = transform.position.x + spawnArea.x / 2;

            if (pos.z > transform.position.z + spawnArea.z / 2)
                pos.z = transform.position.z - spawnArea.z / 2;
            else if (pos.z < transform.position.z - spawnArea.z / 2)
                pos.z = transform.position.z + spawnArea.z / 2;

            clouds[i].transform.position = pos;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 1, 0.2f);
        Gizmos.DrawCube(transform.position + new Vector3(0, spawnArea.y / 2, 0), spawnArea);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position + new Vector3(0, spawnArea.y / 2, 0), spawnArea);
    }
}
