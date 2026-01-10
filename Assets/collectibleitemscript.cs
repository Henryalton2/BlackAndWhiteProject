using UnityEngine;

// Attach this to collectible objects in your scene
public class CollectibleItem : MonoBehaviour
{
    [Header("Item Properties")]
    [SerializeField] private string itemName = "Coin";
    [SerializeField] private Sprite itemIcon;
    [SerializeField] private int quantity = 1;

    [Header("Collection Settings")]
    [SerializeField] private bool autoCollectOnTrigger = true;
    [SerializeField] private string playerTag = "Player";

    [Header("Effects")]
    [SerializeField] private AudioClip collectSound;
    [SerializeField] private GameObject collectEffect;
    [SerializeField] private bool destroyOnCollect = true;

    private bool collected = false;

    private void OnTriggerEnter(Collider other)
    {
        if (autoCollectOnTrigger && !collected && other.CompareTag(playerTag))
        {
            Collect();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (autoCollectOnTrigger && !collected && other.CompareTag(playerTag))
        {
            Collect();
        }
    }

    public void Collect()
    {
        if (collected) return;

        collected = true;

        // Add to inventory
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddItem(itemName, itemIcon, quantity);
        }

        // Play sound
        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position);
        }

        // Spawn effect
        if (collectEffect != null)
        {
            Instantiate(collectEffect, transform.position, Quaternion.identity);
        }

        // Destroy or hide the object
        if (destroyOnCollect)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}