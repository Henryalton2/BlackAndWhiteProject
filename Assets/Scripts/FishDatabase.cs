using UnityEngine;

[CreateAssetMenu(fileName = "FishDatabase", menuName = "Fishing/Fish Database")]
public class FishDatabase : ScriptableObject
{
    public FishData[] fish;

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