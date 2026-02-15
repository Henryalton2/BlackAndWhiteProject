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
    public float groundCheckDistance = 0.2f;

    [Header("Bounce Settings")]
    public float verticalBoost = 5f;
    public float horizontalBoost = 3f;

    private int cloudsLeft;
    private CharacterController controller;
    private List<GameObject> activeClouds = new List<GameObject>();
    private PlayerMovement playerMovement;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerMovement = GetComponent<PlayerMovement>();
        cloudsLeft = maxClouds;
    }

    void Update()
    {
        if (IsOnGround())
        {
            cloudsLeft = maxClouds; // reset counter when touching ground
        }

        // Spawn cloud only if airborne, jump pressed, and clouds left
        if (!IsOnGround() && Input.GetButtonDown("Jump") && cloudsLeft > 0)
        {
            SpawnCloudAndBounce();
        }
    }

    bool IsOnGround()
    {
        if (groundCollider == null) return false;

        Vector3 rayStart = transform.position + Vector3.up * 0.1f;
        Ray ray = new Ray(rayStart, Vector3.down);
        RaycastHit hit;

        if (groundCollider.Raycast(ray, out hit, groundCheckDistance))
        {
            return true;
        }

        return false;
    }

    void SpawnCloudAndBounce()
    {
        // Spawn cloud under player
        Vector3 spawnPos = transform.position + Vector3.up * spawnOffsetY;
        GameObject cloud = Instantiate(cloudPrefab, spawnPos, Quaternion.identity);
        activeClouds.Add(cloud);

        cloudsLeft--;

        StartCoroutine(DestroyCloudAfterTime(cloud));

        // Calculate directional boost
        Vector3 inputDir = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));
        Vector3 boostDir = transform.TransformDirection(inputDir.normalized) * horizontalBoost;

        // Always add vertical boost
        boostDir.y = verticalBoost;

        // Apply boost safely through PlayerMovement
        if (playerMovement != null)
        {
            playerMovement.AddBoost(boostDir);
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

    // Optional: permanently increase clouds the player can spawn
    public void IncreaseMaxClouds(int amount)
    {
        maxClouds += amount;
        cloudsLeft += amount;
    }
}
