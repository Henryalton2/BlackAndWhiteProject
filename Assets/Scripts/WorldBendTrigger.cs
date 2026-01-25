using UnityEngine;

public class WorldBendTrigger : MonoBehaviour
{
    [Header("Material using TerrainWorldBend shader")]
    public Material bendMaterial;

    [Header("Bend Control")]
    public float targetBend = 0.0015f;   // bend when inside trigger
    public float blendSpeed = 2f;

    // Global bend value for trees
    public static float CurrentBend;

    float currentBend;
    bool playerInside = false;

    Terrain terrain;
    float originalDetailDistance = -1f;

    void Start()
    {
        if (bendMaterial == null)
        {
            Debug.LogError("WorldBendTrigger: No material assigned!");
            return;
        }

        // Start at no bend
        currentBend = 0f;
        CurrentBend = 0f;
        bendMaterial.SetFloat("_BendAmount", currentBend);

        // Automatically find Terrain in the scene
        terrain = FindObjectOfType<Terrain>();
        if (terrain != null)
        {
            originalDetailDistance = terrain.detailObjectDistance;
        }
        else
        {
            Debug.LogWarning("WorldBendTrigger: No Terrain found in the scene.");
        }
    }

    void Update()
    {
        // Set target bend: inside = targetBend, outside = 0
        float target = playerInside ? targetBend : 0f;

        // Smoothly blend bend
        currentBend = Mathf.Lerp(currentBend, target, Time.deltaTime * blendSpeed);
        bendMaterial.SetFloat("_BendAmount", currentBend);

        // Update global bend for trees
        CurrentBend = currentBend;

        // Adjust terrain detail distance smoothly
        if (terrain != null)
        {
            float targetDistance = playerInside ? 15f : originalDetailDistance;
            terrain.detailObjectDistance = Mathf.Lerp(terrain.detailObjectDistance, targetDistance, Time.deltaTime * blendSpeed);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInside = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInside = false;
    }
}
