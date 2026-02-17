using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class WorldBendTrigger : MonoBehaviour
{
    [Header("Material using TerrainWorldBend shader")]
    public Material bendMaterial;

    [Header("Bend Control")]
    public float targetBend = 0.0015f;
    public float blendSpeed = 2f;

    [Header("Objects Enabled Inside Bend")]
    public List<GameObject> bendObjects = new List<GameObject>();

    // Global bend value for trees
    public static float CurrentBend;

    private float currentBend;
    private bool playerInside = false;

    private Terrain terrain;
    private float originalDetailDistance = -1f;

    private static WorldBendTrigger activeTrigger;

    void Start()
    {
        terrain = FindObjectOfType<Terrain>();

        if (bendMaterial == null)
        {
            Debug.LogError($"WorldBendTrigger ({gameObject.name}): No material assigned!");
            return;
        }

        currentBend = 0f;
        CurrentBend = 0f;
        bendMaterial.SetFloat("_BendAmount", currentBend);

        if (terrain != null)
            originalDetailDistance = terrain.detailObjectDistance;

        Collider col = GetComponent<Collider>();
        if (!col.isTrigger)
            col.isTrigger = true;

        // Ensure objects start disabled
        SetBendObjectsActive(false);
    }

    void Update()
    {
        if (activeTrigger == this)
        {
            float target = playerInside ? targetBend : 0f;
            currentBend = Mathf.Lerp(currentBend, target, Time.deltaTime * blendSpeed);
            CurrentBend = currentBend;
            bendMaterial.SetFloat("_BendAmount", currentBend);

            if (terrain != null)
            {
                float targetDistance = playerInside ? 15f : originalDetailDistance;
                terrain.detailObjectDistance =
                    Mathf.Lerp(terrain.detailObjectDistance, targetDistance, Time.deltaTime * blendSpeed);
            }

            SetBendObjectsActive(playerInside);
        }
        else if (activeTrigger == null && CurrentBend > 0f)
        {
            CurrentBend = Mathf.Lerp(CurrentBend, 0f, Time.deltaTime * blendSpeed);

            if (bendMaterial != null)
                bendMaterial.SetFloat("_BendAmount", CurrentBend);

            if (terrain != null)
                terrain.detailObjectDistance =
                    Mathf.Lerp(terrain.detailObjectDistance, originalDetailDistance, Time.deltaTime * blendSpeed);

            SetBendObjectsActive(false);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = true;
            activeTrigger = this;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;

            if (activeTrigger == this)
                activeTrigger = null;
        }
    }

    private void SetBendObjectsActive(bool state)
    {
        foreach (GameObject obj in bendObjects)
        {
            if (obj != null)
                obj.SetActive(state);
        }
    }
}
