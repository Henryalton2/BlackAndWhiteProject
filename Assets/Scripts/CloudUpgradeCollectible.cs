using UnityEngine;

public class CloudUpgradeCollectible : MonoBehaviour
{
    [Header("Cloud Upgrade")]
    [Tooltip("How many extra clouds this pickup grants")]
    public int cloudIncreaseAmount = 1;

    [Tooltip("Reference to the player's CloudPlatformSpawner")]
    public CloudPlatformSpawner cloudSpawner;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (cloudSpawner != null)
        {
            cloudSpawner.IncreaseMaxClouds(cloudIncreaseAmount);
        }
        else
        {
            Debug.LogWarning("CloudUpgradeCollectible: No CloudPlatformSpawner assigned!");
        }

        Destroy(gameObject);
    }
}
