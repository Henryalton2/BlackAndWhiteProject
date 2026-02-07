using UnityEngine;
using System.Collections.Generic;

public class CloudSystem : MonoBehaviour
{
    [Header("Cloud Prefabs")]
    public GameObject whiteCloudPrefab;
    public GameObject colorCloud1;
    public GameObject colorCloud2;
    public GameObject colorCloud3;

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
    public float edgeFadeDistance = 15f;

    private List<GameObject> clouds = new();
    private List<float> speeds = new();
    private bool usingWhite = true;

    // ⭐ Night mode reference (cached once)
    private NightModeToggle nightToggle;

    void Start()
    {
        nightToggle = FindObjectOfType<NightModeToggle>();
        SpawnSet(new GameObject[] { whiteCloudPrefab });
    }

    void Update()
    {
        Move();
        Wrap();
        EdgeFade();

        // Swap cloud sets
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (usingWhite)
                SpawnSet(new GameObject[] { colorCloud1, colorCloud2, colorCloud3 });
            else
                SpawnSet(new GameObject[] { whiteCloudPrefab });

            usingWhite = !usingWhite;
        }
    }

    // ================= SPAWNING =================

    void SpawnSet(GameObject[] prefabs)
    {
        ClearClouds();

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

                GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
                GameObject cloud = Instantiate(prefab, pos, Quaternion.identity, transform);
                SetupCloud(cloud);

                clouds.Add(cloud);

                float h = pos.y / spawnArea.y;
                speeds.Add(baseSpeed * Mathf.Lerp(speedVariation.x, speedVariation.y, h));
            }
        }
    }

    void SetupCloud(GameObject cloud)
    {
        SpriteRenderer sr = cloud.GetComponent<SpriteRenderer>();
        if (!sr) return;

        // 🔥 Ensure each cloud has its own material instance so invert is stable
        sr.material = new Material(sr.material);

        // Reset alpha to fully opaque
        Color c = sr.color;
        sr.color = new Color(c.r, c.g, c.b, 1f);

        // ⭐ Register cloud with NightModeToggle
        if (nightToggle != null)
            nightToggle.RegisterObject(cloud);
    }

    void ClearClouds()
    {
        foreach (var c in clouds)
        {
            if (!c) continue;

            // ⭐ Unregister before destroying
            if (nightToggle != null)
                nightToggle.UnregisterObject(c);

            Destroy(c);
        }

        clouds.Clear();
        speeds.Clear();
    }

    // ================= MOVEMENT =================

    void Move()
    {
        Vector3 dir = windDirection.normalized;
        for (int i = 0; i < clouds.Count; i++)
        {
            if (!clouds[i]) continue;
            clouds[i].transform.position += dir * speeds[i] * Time.deltaTime;
        }
    }

    void Wrap()
    {
        Vector3 center = transform.position;
        Vector3 half = spawnArea * 0.5f;

        foreach (var c in clouds)
        {
            Vector3 local = c.transform.position - center;

            if (local.x > half.x) local.x -= spawnArea.x;
            if (local.x < -half.x) local.x += spawnArea.x;
            if (local.z > half.z) local.z -= spawnArea.z;
            if (local.z < -half.z) local.z += spawnArea.z;

            c.transform.position = center + local;
        }
    }

    void EdgeFade()
    {
        Vector3 center = transform.position;
        Vector3 half = spawnArea * 0.5f;

        foreach (var c in clouds)
        {
            var sr = c.GetComponent<SpriteRenderer>();
            if (!sr) continue;

            Vector3 local = c.transform.position - center;

            float dx = half.x - Mathf.Abs(local.x);
            float dz = half.z - Mathf.Abs(local.z);
            float d = Mathf.Min(dx, dz);

            float alpha = Mathf.Clamp01(d / edgeFadeDistance);

            Color col = sr.color;
            sr.color = new Color(col.r, col.g, col.b, alpha);
        }
    }
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.5f, 0.8f, 1f, 1f); // light blue outline

        Vector3 center = transform.position + Vector3.up * (spawnArea.y * 0.5f);
        Vector3 size = spawnArea;

        Gizmos.DrawWireCube(center, size);
    }
#endif

}
