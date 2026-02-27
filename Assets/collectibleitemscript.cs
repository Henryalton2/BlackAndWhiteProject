using UnityEngine;

public class CollectibleItem : MonoBehaviour
{
    [Header("Item Properties")]
    [SerializeField] private string itemName = "Coin";
    [SerializeField] private Sprite itemIcon;
    [SerializeField] private int quantity = 1;

    
    [Header("Throwable Properties")]
    [SerializeField] private bool isThrowable = false;
    [SerializeField] private GameObject throwPrefab;
    [SerializeField] private float throwForce = 12f;
    [SerializeField] private float throwDamage = 10f;
    [SerializeField] private bool consumeOnThrow = true;
  

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
            Collect();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (autoCollectOnTrigger && !collected && other.CompareTag(playerTag))
            Collect();
    }

    public void Collect()
    {
        if (collected) return;
        collected = true;

        if (InventoryManager.Instance != null)
        {
           
            Item item = new Item(itemName, itemIcon, quantity);
            item.isThrowable = isThrowable;
            item.throwPrefab = throwPrefab;
            item.throwForce = throwForce;
            item.throwDamage = throwDamage;
            item.consumeOnThrow = consumeOnThrow;

            InventoryManager.Instance.AddItem(item);
          
        }

        if (collectSound != null)
            AudioSource.PlayClipAtPoint(collectSound, transform.position);

        if (collectEffect != null)
            Instantiate(collectEffect, transform.position, Quaternion.identity);

        if (destroyOnCollect)
            Destroy(gameObject);
        else
            gameObject.SetActive(false);
    }
}