using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
// FishData — ScriptableObject defining one type of fish.
// Create assets via: Right-click in Project > Create > Fishing > Fish Data
// ─────────────────────────────────────────────────────────────────────────────
[CreateAssetMenu(fileName = "NewFish", menuName = "Fishing/Fish Data")]
public class FishData : ScriptableObject
{
    [Header("Identity")]
    public string fishName = "Bass";
    public Sprite fishIcon;
    [TextArea] public string description = "A common river fish.";

    [Header("Rarity")]
    [Range(0f, 1f)]
    public float spawnWeight = 1f;   // relative probability; higher = more common

    [Header("Minigame Difficulty Overrides")]
    [Tooltip("Leave at 0 to use FishingMinigameUI defaults")]
    public float fishZoneHeightOverride = 0f;
    public float fishZoneMaxSpeedOverride = 0f;
}


// ─────────────────────────────────────────────────────────────────────────────
// FishDatabase — ScriptableObject holding the full roster of catchable fish.
// Create via: Right-click > Create > Fishing > Fish Database
// ─────────────────────────────────────────────────────────────────────────────
[CreateAssetMenu(fileName = "FishDatabase", menuName = "Fishing/Fish Database")]
public class FishDatabase : ScriptableObject
{
    public FishData[] fish;

    // ── Static singleton-style accessor ──────────────────────────────
    // Assign the database asset to this field at startup, or load from Resources.
    private static FishDatabase _instance;
    public static FishDatabase Instance
    {
        get
        {
            if (_instance == null)
                _instance = Resources.Load<FishDatabase>("FishDatabase");
            return _instance;
        }
        set => _instance = value;
    }

    /// <summary>
    /// Returns a random FishData weighted by spawnWeight.
    /// Falls back to fish[0] if database is empty.
    /// </summary>
    public static FishData GetRandomFish()
    {
        var db = Instance;
        if (db == null || db.fish == null || db.fish.Length == 0)
        {
            Debug.LogWarning("[FishDatabase] No fish database found or database is empty.");
            return null;
        }

        float totalWeight = 0f;
        foreach (var f in db.fish)
            totalWeight += Mathf.Max(0f, f.spawnWeight);

        float roll = Random.Range(0f, totalWeight);
        float accumulated = 0f;

        foreach (var f in db.fish)
        {
            accumulated += Mathf.Max(0f, f.spawnWeight);
            if (roll <= accumulated) return f;
        }

        return db.fish[db.fish.Length - 1];
    }
}