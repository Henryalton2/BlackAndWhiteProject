using UnityEngine;
using System.Collections.Generic;

public class StoreManager : MonoBehaviour
{
    public static StoreManager Instance { get; private set; }

    [Header("Catalogue")]
    [SerializeField] private List<StoreItem> catalogue = new();

    // Runtime stock tracking (index matches catalogue list)
    private List<int> runtimeStock = new();

    public System.Action OnStoreChanged;   // UI listens to this

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // Initialise runtime stock from ScriptableObjects
        foreach (var item in catalogue)
            runtimeStock.Add(item.stock);
    }

    

    public List<StoreItem> GetCatalogue() => catalogue;

    public int GetStock(int index)
    {
        if (index < 0 || index >= runtimeStock.Count) return 0;
        return runtimeStock[index];   // -1 means unlimited
    }

    /// Attempt to buy one listing. Returns true on success.
  
    public bool TryPurchase(int index)
    {
        if (index < 0 || index >= catalogue.Count) return false;

        StoreItem entry = catalogue[index];

        // Stock check
        if (runtimeStock[index] == 0)
        {
            Debug.Log($"[Store] {entry.rewardItemName} is out of stock.");
            return false;
        }

        // Currency check
        if (!InventoryManager.Instance.HasItem(entry.costItemName, entry.costQuantity))
        {
            Debug.Log($"[Store] Not enough {entry.costItemName}. Need {entry.costQuantity}.");
            return false;
        }

        // Deduct cost
        InventoryManager.Instance.RemoveItem(entry.costItemName, entry.costQuantity);

        // Give reward — build a full Item with throwable data
        Item reward = new Item(entry.rewardItemName, entry.rewardIcon, entry.rewardQuantity)
        {
            isThrowable = entry.isThrowable,
            throwPrefab = entry.throwPrefab,
            throwForce = entry.throwForce,
            throwDamage = entry.throwDamage,
            consumeOnThrow = entry.consumeOnThrow
        };
        InventoryManager.Instance.AddItem(reward);

        // Reduce stock if finite
        if (runtimeStock[index] > 0)
            runtimeStock[index]--;

        Debug.Log($"[Store] Bought {entry.rewardQuantity}x {entry.rewardItemName}.");
        OnStoreChanged?.Invoke();
        return true;
    }
}