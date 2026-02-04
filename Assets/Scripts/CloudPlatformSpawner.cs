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
    public int maxActiveClouds = 1;
    public float spawnCooldown = 1.5f;

    private bool canSpawn = true;
    private List<GameObject> activeClouds = new List<GameObject>();
    private CharacterController controller;

    // 🔽 NEW: Airborne tracking
    private bool hasLeftGround = false;
    private float airTimer = 0f;
    [SerializeField] private float minAirTime = 0.1f; // prevents edge-case spawns

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (controller.isGrounded)
        {
            hasLeftGround = false;
            airTimer = 0f;
            return;
        }

        // Player is airborne
        airTimer += Time.deltaTime;
        hasLeftGround = true;

        // 🔽 Only allow cloud after being in air briefly
        if (hasLeftGround && airTimer >= minAirTime && Input.GetButtonDown("Jump"))
        {
            TrySpawnCloud();
        }
    }

    void TrySpawnCloud()
    {
        if (!canSpawn) return;

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

    public void IncreaseMaxClouds(int amount)
    {
        maxActiveClouds += amount;
    }
}
