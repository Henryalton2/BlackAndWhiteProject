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
    public Transform groundCheck;

    [Header("Cloud Limit")]
    public int maxClouds = 1;

    [Header("Bounce Settings")]
    public float verticalBoost = 5f;

    [Header("Air Time Required")]
    public float airTimeRequired = 0.33f;

    [Header("Ground Detection")]
    public LayerMask terrainMask;

    private int cloudsLeft;
    private float timeLeftGround = -10f;
    private bool wasOnGround;
    private PlayerMovement playerMovement;
    private CharacterController characterController;
    private List<GameObject> activeClouds = new List<GameObject>();

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        characterController = GetComponent<CharacterController>();
        cloudsLeft = maxClouds;
        wasOnGround = true;
    }

    bool IsOnTerrain()
    {
        RaycastHit hit;
        bool didHit = Physics.Raycast(groundCheck.position, Vector3.down, out hit, 0.5f, terrainMask);
        if (didHit)
            Debug.Log("[CloudSpawner] Raycast hit: " + hit.collider.gameObject.name + " layer: " + hit.collider.gameObject.layer);
        else
            Debug.Log("[CloudSpawner] Raycast hit nothing");
        return didHit;
    }
    void Update()
    {
        bool onGround = characterController.isGrounded;
        bool onTerrain = IsOnTerrain();

        Debug.Log($"[CloudSpawner] onGround: {onGround} | onTerrain: {onTerrain} | airTime: {(Time.time - timeLeftGround):F2} | cloudsLeft: {cloudsLeft}");

        // Record exact moment player leaves the ground
        if (wasOnGround && !onGround)
            timeLeftGround = Time.time;

        wasOnGround = onGround;

        // Only recharge clouds when standing on actual terrain, not cloud platforms
        if (onTerrain && cloudsLeft != maxClouds)
        {
            cloudsLeft = maxClouds;
            Debug.Log("[CloudSpawner] Reset cloudsLeft — touching terrain.");
        }

        float airTime = Time.time - timeLeftGround;
        bool hasBeenAirborne = airTime > airTimeRequired;

        if (!onGround && hasBeenAirborne && Input.GetButtonDown("Jump") && cloudsLeft > 0)
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