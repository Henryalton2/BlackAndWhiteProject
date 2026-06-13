using UnityEngine;
[CreateAssetMenu(fileName = "NewStoreItem", menuName = "Store/Store Item")]
public class StoreItem : ScriptableObject
{
    [Header("What the player receives")]
    public string rewardItemName = "Rock";
    public Sprite rewardIcon;
    public int rewardQuantity = 1;

    [Header("Throwable properties of the reward")]
    public bool isThrowable = true;
    public GameObject throwPrefab;
    public float throwForce = 12f;
    public float throwDamage = 10f;
    public bool consumeOnThrow = true;

    [Header("Cost  (paid in throwable items)")]
    public string costItemName = "PineCone";   // name of the currency item
    public int costQuantity = 3;

    [Header("Stock  (-1 = unlimited)")]
    public int stock = -1;

    [Header("Display")]
    [TextArea] public string description = "A throwable rock.";
}