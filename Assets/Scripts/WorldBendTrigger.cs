using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WorldBendTrigger : MonoBehaviour
{
    [Header("Material using TerrainWorldBend shader")]
    public Material bendMaterial;

    [Header("Bend Control")]
    public float targetBend = 0.0015f;   // bend when inside trigger
    public float blendSpeed = 2f;

    // Global bend value for trees
    public static float CurrentBend;

    private float currentBend;
    private bool playerInside = false;

    private Terrain terrain;
    private float originalDetailDistance = -1f;

    // Track the active trigger
    private static WorldBendTrigger activeTrigger;

    void Start()
    {
        if (bendMaterial == null)
        {
            Debug.LogError($"WorldBendTrigger ({gameObject.name}): No material assigned!");
            return;
        }

        currentBend = 0f;
        CurrentBend = 0f;
        bendMaterial.SetFloat("_BendAmount", currentBend);

        terrain = FindObjectOfType<Terrain>();
        if (terrain != null)
            originalDetailDistance = terrain.detailObjectDistance;

        // Ensure collider is trigger
        Collider col = GetComponent<Collider>();
        if (!col.isTrigger)
        {
            Debug.LogWarning($"WorldBendTrigger ({gameObject.name}): Collider is not marked as Trigger! Setting it automatically.");
            col.isTrigger = true;
        }
    }

    void Update()
    {
        // If this trigger is active, lerp toward its target
        if (activeTrigger == this)
        {
            float target = playerInside ? targetBend : 0f;
            currentBend = Mathf.Lerp(currentBend, target, Time.deltaTime * blendSpeed);
            CurrentBend = currentBend;
            bendMaterial.SetFloat("_BendAmount", currentBend);

            if (terrain != null)
            {
                float targetDistance = playerInside ? 15f : originalDetailDistance;
                terrain.detailObjectDistance = Mathf.Lerp(terrain.detailObjectDistance, targetDistance, Time.deltaTime * blendSpeed);
            }
        }
        // No active trigger → reset bend to zero
        else if (activeTrigger == null && CurrentBend > 0f)
        {
            CurrentBend = Mathf.Lerp(CurrentBend, 0f, Time.deltaTime * blendSpeed);
            if (bendMaterial != null)
                bendMaterial.SetFloat("_BendAmount", CurrentBend);

            if (terrain != null)
                terrain.detailObjectDistance = Mathf.Lerp(terrain.detailObjectDistance, originalDetailDistance, Time.deltaTime * blendSpeed);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = true;
            activeTrigger = this;
            Debug.Log($"[WorldBendTrigger] Entered trigger '{gameObject.name}', targetBend = {targetBend}");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;

            // If this was the active trigger, clear it
            if (activeTrigger == this)
                activeTrigger = null;

            Debug.Log($"[WorldBendTrigger] Exited trigger '{gameObject.name}'");
        }
    }
}
