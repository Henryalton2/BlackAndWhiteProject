using UnityEngine;

public class Harvestable : MonoBehaviour
{
    [Header("Item to Give")]
    [SerializeField] private string itemName = "Item";
    [SerializeField] private Sprite itemIcon;
    [SerializeField] private int harvestQuantity = 1;

    [Header("Interaction")]
    [SerializeField] private KeyCode harvestKey = KeyCode.E;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string promptText = "Press E to Harvest";

    [Header("Sprite Swap (Optional)")]
    [SerializeField] private Sprite harvestedSprite;

    [Header("After Harvest (Optional)")]
    [SerializeField] private bool disableAfterHarvest = false;
    [SerializeField] private bool destroyAfterHarvest = false;
    [SerializeField] private float respawnTime = 0f;

    [Header("Effects (Optional)")]
    [SerializeField] private AudioClip harvestSound;
    [SerializeField] private GameObject harvestEffect;

    private bool harvested = false;
    private bool playerInRange = false;
    private SpriteRenderer spriteRenderer;
    private Sprite originalSprite;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            originalSprite = spriteRenderer.sprite;
    }

    private void Update()
    {
        if (playerInRange && !harvested && Input.GetKeyDown(harvestKey))
            Harvest();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
            playerInRange = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
            playerInRange = false;
    }

    private void Harvest()
    {
        harvested = true;

        // Add to inventory
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.AddItem(itemName, itemIcon, harvestQuantity);

        // Swap sprite
        if (spriteRenderer != null && harvestedSprite != null)
            spriteRenderer.sprite = harvestedSprite;

        // Sound
        if (harvestSound != null)
            AudioSource.PlayClipAtPoint(harvestSound, transform.position);

        // Effect
        if (harvestEffect != null)
            Instantiate(harvestEffect, transform.position, Quaternion.identity);

        // Post-harvest behaviour
        if (destroyAfterHarvest)
        {
            Destroy(gameObject);
            return;
        }

        if (disableAfterHarvest)
        {
            gameObject.SetActive(false);
            return;
        }

        // Respawn
        if (respawnTime > 0f)
            Invoke(nameof(Respawn), respawnTime);
    }

    private void Respawn()
    {
        harvested = false;

        if (spriteRenderer != null && originalSprite != null)
            spriteRenderer.sprite = originalSprite;
    }

    private void OnGUI()
    {
        if (playerInRange && !harvested)
            GUI.Label(new Rect(Screen.width / 2 - 80, Screen.height - 100, 160, 30), promptText);
    }
}