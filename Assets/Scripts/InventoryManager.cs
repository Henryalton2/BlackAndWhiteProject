using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class Item
{
    public string itemName;
    public Sprite icon;
    public int quantity;

 
    public bool isThrowable = false;
    public GameObject throwPrefab;
    public float throwForce = 12f;
    public float throwDamage = 10f;
    public bool consumeOnThrow = true;
 

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

    public System.Action OnInventoryChanged;

    private void Awake()
    {
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

    public bool AddItem(string itemName, Sprite icon, int quantity = 1)
    {
        Item existingItem = inventory.Find(item => item.itemName == itemName);
        if (existingItem != null)
        {
            existingItem.quantity += quantity;
        }
        else
        {
            if (inventory.Count >= maxInventorySlots)
            {
                Debug.Log("Inventory is full!");
                return false;
            }
            inventory.Add(new Item(itemName, icon, quantity));
        }

        OnInventoryChanged?.Invoke();
        Debug.Log($"Added {quantity}x {itemName} to inventory");
        return true;
    }

  
    public bool AddItem(Item newItem)
    {
        Item existingItem = inventory.Find(item => item.itemName == newItem.itemName);
        if (existingItem != null)
        {
            existingItem.quantity += newItem.quantity;
        }
        else
        {
            if (inventory.Count >= maxInventorySlots)
            {
                Debug.Log("Inventory is full!");
                return false;
            }
            inventory.Add(newItem);
        }

        OnInventoryChanged?.Invoke();
        Debug.Log($"Added {newItem.quantity}x {newItem.itemName} to inventory");
        return true;
    }
   

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

    public bool HasItem(string itemName, int quantity = 1)
    {
        Item item = inventory.Find(i => i.itemName == itemName);
        return item != null && item.quantity >= quantity;
    }

    public int GetItemCount(string itemName)
    {
        Item item = inventory.Find(i => i.itemName == itemName);
        return item != null ? item.quantity : 0;
    }

    public List<Item> GetAllItems()
    {
        return new List<Item>(inventory);
    }

    public void ClearInventory()
    {
        inventory.Clear();
        OnInventoryChanged?.Invoke();
    }
}