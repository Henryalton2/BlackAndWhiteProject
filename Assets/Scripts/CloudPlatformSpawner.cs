using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloudPlatformSpawner : MonoBehaviour
{
    [Header("Cloud Settings")]
    public GameObject cloudPrefab;
    public float cloudLifetime = 5f;
    public float spawnOffsetY = -1.5f;

    [Header("Limits")]
    public int maxActiveClouds = 1; // Upgradeable
    public float spawnCooldown = 1.5f;

    private bool canSpawn = true;
    private List<GameObject> activeClouds = new List<GameObject>();
    private CharacterController controller;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        // Only spawn if player jumps while NOT grounded
        if (Input.GetButtonDown("Jump") && !controller.isGrounded)
        {
            TrySpawnCloud();
        }
    }

    void TrySpawnCloud()
    {
        if (!canSpawn) return;

        // Enforce max cloud count
        if (activeClouds.Count >= maxActiveClouds)
        {
            Destroy(activeClouds[0]);
            activeClouds.RemoveAt(0);
        }

        Vector3 spawnPos = transform.position;
        spawnPos.y += spawnOffsetY;

        GameObject cloud = Instantiate(cloudPrefab, spawnPos, Quaternion.identity);
        activeClouds.Add(cloud);

        StartCoroutine(CloudLifetimeRoutine(cloud));
        StartCoroutine(SpawnCooldownRoutine());
    }

    IEnumerator CloudLifetimeRoutine(GameObject cloud)
    {
        yield return new WaitForSeconds(cloudLifetime);

        if (activeClouds.Contains(cloud))
            activeClouds.Remove(cloud);

        Destroy(cloud);
    }

    IEnumerator SpawnCooldownRoutine()
    {
        canSpawn = false;
        yield return new WaitForSeconds(spawnCooldown);
        canSpawn = true;
    }

    // Call this when player unlocks more clouds
    public void IncreaseMaxClouds(int amount)
    {
        maxActiveClouds += amount;
    }
}
