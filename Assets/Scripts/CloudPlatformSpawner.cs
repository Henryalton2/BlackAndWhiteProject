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
    public Collider groundCollider;
    public float groundCheckDistance = 0.2f; // distance to check below player

    private int cloudsLeft; // counter for remaining clouds
    private CharacterController controller;
    private List<GameObject> activeClouds = new List<GameObject>();

    void Start()
    {
        controller = GetComponent<CharacterController>();
        cloudsLeft = maxClouds;
    }

    void Update()
    {
        if (IsOnGround())
        {
            cloudsLeft = maxClouds; // reset counter when touching ground
        }

        // Only spawn cloud if airborne, jump pressed, and clouds left
        if (!IsOnGround() && Input.GetButtonDown("Jump") && cloudsLeft > 0)
        {
            SpawnCloud();
        }
    }

    bool IsOnGround()
    {
        if (groundCollider == null) return false;

        // Cast a short ray down from the bottom of the character controller
        Vector3 rayStart = transform.position + Vector3.up * 0.1f; // slightly above feet
        Ray ray = new Ray(rayStart, Vector3.down);
        RaycastHit hit;

        if (groundCollider.Raycast(ray, out hit, groundCheckDistance + 0.1f))
        {
            return true;
        }

        return false;
    }

    void SpawnCloud()
    {
        Vector3 spawnPos = transform.position + Vector3.up * spawnOffsetY;
        GameObject cloud = Instantiate(cloudPrefab, spawnPos, Quaternion.identity);
        activeClouds.Add(cloud);

        cloudsLeft--; // reduce remaining clouds

        StartCoroutine(DestroyCloudAfterTime(cloud));
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

    // Optional: call this to permanently increase clouds the player can spawn
    public void IncreaseMaxClouds(int amount)
    {
        maxClouds += amount;
        cloudsLeft += amount;
    }
}
