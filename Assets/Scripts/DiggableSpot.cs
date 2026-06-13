using System.Collections;
using UnityEngine;

public class DiggableSpot : MonoBehaviour
{
    [Header("Loot")]
    public string[] lootItemNames;
    public Sprite[] lootItemIcons;

    [Header("Visuals")]
    public GameObject xMarkerVisual;
    public GameObject digEffectPrefab;

    [Header("Respawn")]
    public bool respawns = false;
    public float respawnTime = 120f;

    public bool HasBeenDug { get; private set; }

    public void Dig()
    {
        if (HasBeenDug) return;
        HasBeenDug = true;

        GiveLoot();
        SpawnEffect();

        if (xMarkerVisual != null) xMarkerVisual.SetActive(false);

        // Disable collider so the raycast ignores this spot while dug
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        if (respawns)
            StartCoroutine(RespawnAfter());
    }

    void GiveLoot()
    {
        if (lootItemNames == null || lootItemNames.Length == 0 || InventoryManager.Instance == null) return;

        int i = Random.Range(0, lootItemNames.Length);
        Sprite icon = (lootItemIcons != null && i < lootItemIcons.Length) ? lootItemIcons[i] : null;
        InventoryManager.Instance.AddItem(lootItemNames[i], icon, 1);
    }

    void SpawnEffect()
    {
        if (digEffectPrefab == null) return;
        Destroy(Instantiate(digEffectPrefab, transform.position, Quaternion.identity), 3f);
    }

    IEnumerator RespawnAfter()
    {
        yield return new WaitForSeconds(respawnTime);

        HasBeenDug = false;
        if (xMarkerVisual != null) xMarkerVisual.SetActive(true);

        var col = GetComponent<Collider>();
        if (col != null) col.enabled = true;
    }
}
