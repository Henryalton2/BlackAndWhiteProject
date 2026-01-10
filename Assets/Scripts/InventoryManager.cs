using UnityEngine;
using System.Collections.Generic;

// Item data structure
[System.Serializable]
public class Item
{
    public string itemName;
    public Sprite icon;
    public int quantity;

    public Item(string name, Sprite itemIcon, int qty = 1)
    {
        itemName = name;
        icon = itemIcon;
        quantity = qty;
    }
}


public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [Header("Inventory Settings")]
    [SerializeField] private int maxInventorySlots = 20;

    private List<Item> inventory = new List<Item>();

    // Event to notify UI when inventory changes
    public System.Action OnInventoryChanged;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Add item to inventory
    public bool AddItem(string itemName, Sprite icon, int quantity = 1)
    {
        // Check if item already exists in inventory
        Item existingItem = inventory.Find(item => item.itemName == itemName);

        if (existingItem != null)
        {
            // Stack the item
            existingItem.quantity += quantity;
        }
        else
        {
            // Check if inventory is full
            if (inventory.Count >= maxInventorySlots)
            {
                Debug.Log("Inventory is full!");
                return false;
            }

            // Add new item
            inventory.Add(new Item(itemName, icon, quantity));
        }

        OnInventoryChanged?.Invoke();
        Debug.Log($"Added {quantity}x {itemName} to inventory");
        return true;
    }

    // Remove item from inventory
    public bool RemoveItem(string itemName, int quantity = 1)
    {
        Item item = inventory.Find(i => i.itemName == itemName);

        if (item != null)
        {
            item.quantity -= quantity;

            if (item.quantity <= 0)
            {
                inventory.Remove(item);
            }

            OnInventoryChanged?.Invoke();
            return true;
        }

        return false;
    }

    // Check if player has item
    public bool HasItem(string itemName, int quantity = 1)
    {
        Item item = inventory.Find(i => i.itemName == itemName);
        return item != null && item.quantity >= quantity;
    }

    // Get item count
    public int GetItemCount(string itemName)
    {
        Item item = inventory.Find(i => i.itemName == itemName);
        return item != null ? item.quantity : 0;
    }

    // Get all items
    public List<Item> GetAllItems()
    {
        return new List<Item>(inventory);
    }

    // Clear inventory
    public void ClearInventory()
    {
        inventory.Clear();
        OnInventoryChanged?.Invoke();
    }
}