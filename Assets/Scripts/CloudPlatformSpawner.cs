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
    [Tooltip("Number of clouds the player can spawn before touching the ground")]
    public int maxClouds = 1;

    [Header("Ground Detection")]
    [Tooltip("Drag the Terrain or ground object here")]
    public Collider terrainCollider;
    public float groundCheckDistance = 0.2f;

    [Header("Bounce Settings")]
    public float verticalBoost = 5f;

    [Header("Cloud Layer")]
    public LayerMask cloudLayer; // Assign your Cloud prefab layer

    private int cloudsLeft;
    private CharacterController controller;
    private PlayerMovement playerMovement;
    private List<GameObject> activeClouds = new List<GameObject>();

    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerMovement = GetComponent<PlayerMovement>();
        cloudsLeft = maxClouds;
    }

    void Update()
    {
        // Reset clouds only when touching terrain
        if (IsTouchingTerrain())
        {
            if (cloudsLeft != maxClouds)
            {
                cloudsLeft = maxClouds;
                Debug.Log("[CloudSpawner] Reset cloudsLeft because touching terrain.");
            }
        }

        // Only spawn cloud if player is airborne (not touching terrain or cloud)
        if (!IsTouchingGroundOrCloud() && Input.GetButtonDown("Jump") && cloudsLeft > 0)
        {
            SpawnCloud();
        }
    }

    bool IsTouchingTerrain()
    {
        if (terrainCollider == null) return false;

        Vector3 rayStart = transform.position + Vector3.up * 0.1f;
        Ray ray = new Ray(rayStart, Vector3.down);
        RaycastHit hit;

        if (terrainCollider.Raycast(ray, out hit, groundCheckDistance))
        {
            return true;
        }

        return false;
    }

    bool IsTouchingGroundOrCloud()
    {
        Vector3 rayStart = transform.position + Vector3.up * 0.1f;
        Ray ray = new Ray(rayStart, Vector3.down);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, groundCheckDistance))
        {
            // Grounded on terrain or a cloud platform
            if (hit.collider == terrainCollider || ((1 << hit.collider.gameObject.layer) & cloudLayer) != 0)
            {
                return true;
            }
        }

        return false;
    }

    void SpawnCloud()
    {
        Vector3 spawnPos = transform.position + Vector3.up * spawnOffsetY;
        GameObject cloud = Instantiate(cloudPrefab, spawnPos, Quaternion.identity);
        activeClouds.Add(cloud);

        cloudsLeft--;
        Debug.Log("[CloudSpawner] Spawned cloud. Clouds left: " + cloudsLeft);

        StartCoroutine(DestroyCloudAfterTime(cloud));

        // Apply vertical boost only
        if (playerMovement != null)
        {
            playerMovement.AddBoost(new Vector3(0f, verticalBoost, 0f));
        }
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
