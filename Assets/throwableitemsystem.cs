using UnityEngine;
using System.Collections.Generic;

public class ThrowableItemSystem : MonoBehaviour
{
    [Header("Throwable Settings")]
    [SerializeField] private List<ThrowableItemData> throwableItems = new List<ThrowableItemData>();
    private int currentItemIndex = 0;

    [Header("Throw Settings")]
    [SerializeField] private Transform throwPoint; // Where the item spawns (usually camera position)
    [SerializeField] private float throwForce = 15f;
    [SerializeField] private KeyCode throwKey = KeyCode.E;
    [SerializeField] private KeyCode nextItemKey = KeyCode.Q;
    [SerializeField] private KeyCode previousItemKey = KeyCode.LeftBracket;

    [Header("UI Feedback")]
    [SerializeField] private bool showDebugMessages = true;

    private Camera playerCamera;
    private ThrowableItemData currentThrowable;

    private void Start()
    {
        if (throwPoint == null)
        {
            // Default to camera position if not assigned
            playerCamera = Camera.main;
            throwPoint = playerCamera.transform;
        }
        else
        {
            playerCamera = Camera.main;
        }

        UpdateCurrentThrowable();
    }

    private void Update()
    {
        // Pause check
        if (PauseMenu.GameisPaused) return;

        // Don't throw when inventory is open
        SimpleInventoryUI inventoryUI = FindObjectOfType<SimpleInventoryUI>();
        if (inventoryUI != null && inventoryUI.IsInventoryOpen())
        {
            return;
        }

        // Throw item
        if (Input.GetKeyDown(throwKey))
        {
            ThrowCurrentItem();
        }

        // Switch between throwable items
        if (Input.GetKeyDown(nextItemKey))
        {
            SwitchToNextItem();
        }

        if (Input.GetKeyDown(previousItemKey))
        {
            SwitchToPreviousItem();
        }
    }

    private void ThrowCurrentItem()
    {
        if (currentThrowable == null)
        {
            if (showDebugMessages)
                Debug.Log("No throwable item selected!");
            return;
        }

        // Check if player has the item in inventory
        if (InventoryManager.Instance == null)
        {
            Debug.LogError("InventoryManager instance not found!");
            return;
        }

        if (!InventoryManager.Instance.HasItem(currentThrowable.itemName, 1))
        {
            if (showDebugMessages)
                Debug.Log($"You don't have any {currentThrowable.itemName} to throw!");
            return;
        }

        // Remove item from inventory
        InventoryManager.Instance.RemoveItem(currentThrowable.itemName, 1);

        // Spawn and throw the projectile
        SpawnAndThrowProjectile(currentThrowable);

        if (showDebugMessages)
        {
            int remaining = InventoryManager.Instance.GetItemCount(currentThrowable.itemName);
            Debug.Log($"Threw {currentThrowable.itemName}! Remaining: {remaining}");
        }
    }

    private void SpawnAndThrowProjectile(ThrowableItemData throwableData)
    {
        if (throwableData.projectilePrefab == null)
        {
            Debug.LogError($"No projectile prefab assigned for {throwableData.itemName}!");
            return;
        }

        Vector3 spawnPos = throwPoint.position + playerCamera.transform.forward * 1.8f;

        GameObject projectile = Instantiate(
            throwableData.projectilePrefab,
            spawnPos,
            throwPoint.rotation
        );


        // Get the projectile component and initialize it
        ThrowableProjectile projectileScript = projectile.GetComponent<ThrowableProjectile>();
        if (projectileScript != null)
        {
            projectileScript.Initialize(throwableData);
        }

        // Apply throw force
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 throwDirection = playerCamera.transform.forward;
            rb.AddForce(throwDirection * throwForce, ForceMode.Impulse);

            // Add slight upward arc if configured
            if (throwableData.addArc)
            {
                rb.AddForce(Vector3.up * throwableData.arcAmount, ForceMode.Impulse);
            }

            // Add spin if configured
            if (throwableData.addSpin)
            {
                rb.AddTorque(Random.insideUnitSphere * throwableData.spinAmount, ForceMode.Impulse);
            }
        }

        // Play throw sound
        if (throwableData.throwSound != null)
        {
            AudioSource.PlayClipAtPoint(throwableData.throwSound, throwPoint.position);
        }
    }

    private void SwitchToNextItem()
    {
        if (throwableItems.Count == 0) return;

        currentItemIndex++;
        if (currentItemIndex >= throwableItems.Count)
            currentItemIndex = 0;

        UpdateCurrentThrowable();
    }

    private void SwitchToPreviousItem()
    {
        if (throwableItems.Count == 0) return;

        currentItemIndex--;
        if (currentItemIndex < 0)
            currentItemIndex = throwableItems.Count - 1;

        UpdateCurrentThrowable();
    }

    private void UpdateCurrentThrowable()
    {
        if (throwableItems.Count > 0 && currentItemIndex < throwableItems.Count)
        {
            currentThrowable = throwableItems[currentItemIndex];

            if (showDebugMessages && InventoryManager.Instance != null)
            {
                int count = InventoryManager.Instance.GetItemCount(currentThrowable.itemName);
                Debug.Log($"Selected: {currentThrowable.itemName} ({count} available)");
            }
        }
    }

    // Public method to check if item is throwable
    public bool IsItemThrowable(string itemName)
    {
        return throwableItems.Exists(x => x.itemName == itemName);
    }

    // Public method to add throwable items at runtime
    public void AddThrowableItem(ThrowableItemData itemData)
    {
        if (!throwableItems.Contains(itemData))
        {
            throwableItems.Add(itemData);
        }
    }

    // Get current throwable info
    public ThrowableItemData GetCurrentThrowable()
    {
        return currentThrowable;
    }

    public int GetCurrentItemCount()
    {
        if (currentThrowable != null && InventoryManager.Instance != null)
        {
            return InventoryManager.Instance.GetItemCount(currentThrowable.itemName);
        }
        return 0;
    }
}

// Data structure for throwable items
[System.Serializable]
public class ThrowableItemData
{
    [Header("Item Reference")]
    public string itemName; // Must match the name in inventory

    [Header("Projectile")]
    public GameObject projectilePrefab; // The visible object that gets thrown

    [Header("Physics")]
    public bool addArc = true;
    [Range(0f, 10f)] public float arcAmount = 2f;
    public bool addSpin = false;
    [Range(0f, 20f)] public float spinAmount = 5f;

    [Header("Audio")]
    public AudioClip throwSound;
}