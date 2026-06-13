using UnityEngine;

public class WorldBendManager : MonoBehaviour
{
    public Material bendMaterial;
    public float lerpSpeed = 2f;

    float currentBend;
    float targetBend;

    private Terrain terrain;
    private Material terrainMaterial;

    void Start()
    {
        terrain = FindObjectOfType<Terrain>();
        if (terrain != null)
            terrainMaterial = terrain.materialTemplate;

        currentBend = bendMaterial != null
            ? bendMaterial.GetFloat("_BendAmount")
            : 0f;
        targetBend = currentBend;

        Debug.Log("WorldBendManager started. Initial bend = " + currentBend);
    }

    void Update()
    {
        currentBend = Mathf.Lerp(currentBend, targetBend, Time.deltaTime * lerpSpeed);
        SetBend(currentBend);

        if (Time.frameCount % 60 == 0)
            Debug.Log("Current bend: " + currentBend + " | Target: " + targetBend);
    }

    void SetBend(float value)
    {
        if (bendMaterial != null)
            bendMaterial.SetFloat("_BendAmount", value);

        if (terrainMaterial != null)
            terrainMaterial.SetFloat("_BendAmount", value);
    }

    public void SetTargetBend(float newBend)
    {
        Debug.Log("Bend target set to: " + newBend);
        targetBend = newBend;
    }
}
