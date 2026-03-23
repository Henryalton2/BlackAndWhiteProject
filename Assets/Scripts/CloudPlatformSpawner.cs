using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class CloudPlatformSpawner : MonoBehaviour
{
    [Header("Cloud Settings")]
    public GameObject cloudPrefab;
    public float cloudLifetime = 5f;
    public float spawnOffsetY = -1.5f;

    [Header("Cloud Limit")]
    public int maxClouds = 1;

    [Header("Ground Detection")]
    [Tooltip("Drag the Terrain or ground object here")]
    public Collider terrainCollider;
    public float groundCheckDistance = 0.2f;

    [Header("Bounce Settings")]
    public float verticalBoost = 5f;

    private int cloudsLeft;
    private PlayerMovement playerMovement;
    private List<GameObject> activeClouds = new List<GameObject>();

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        cloudsLeft = maxClouds;
    }

    bool IsTouchingTerrain()
    {
        if (terrainCollider == null) return false;
        Vector3 rayStart = transform.position + Vector3.up * 0.1f;
        Ray ray = new Ray(rayStart, Vector3.down);
        RaycastHit hit;
        return terrainCollider.Raycast(ray, out hit, groundCheckDistance);
    }

    void Update()
    {
        // Only reset cloud count when touching actual terrain, not clouds
        if (IsTouchingTerrain())
        {
            if (cloudsLeft != maxClouds)
            {
                cloudsLeft = maxClouds;
                Debug.Log("[CloudSpawner] Reset cloudsLeft — touching terrain.");
            }
        }

        // Only spawn when airborne (not grounded on anything)
        if (!playerMovement.IsGrounded && Input.GetButtonDown("Jump") && cloudsLeft > 0)
            SpawnCloud();
    }

    void SpawnCloud()
    {
        Vector3 spawnPos = transform.position + Vector3.up * spawnOffsetY;
        GameObject cloud = Instantiate(cloudPrefab, spawnPos, Quaternion.identity);
        activeClouds.Add(cloud);
        cloudsLeft--;
        Debug.Log("[CloudSpawner] Spawned cloud. Clouds left: " + cloudsLeft);
        StartCoroutine(DestroyCloudAfterTime(cloud));

        if (playerMovement != null)
            playerMovement.AddBoost(new Vector3(0f, verticalBoost, 0f));
    }

    IEnumerator DestroyCloudAfterTime(GameObject cloud)
    {
        yield return new WaitForSeconds(cloudLifetime);
        if (cloud != null)
        {
            activeClouds.Remove(cloud);
            Destroy(cloud);
        }
    }

    public void IncreaseMaxClouds(int amount)
    {
        maxClouds += amount;
        cloudsLeft += amount;
    }
}