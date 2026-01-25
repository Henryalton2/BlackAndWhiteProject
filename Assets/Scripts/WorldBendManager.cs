using UnityEngine;

public class WorldBendManager : MonoBehaviour
{
    public Material bendMaterial;
    public float lerpSpeed = 2f;

    float currentBend;
    float targetBend;

    void Start()
    {
        if (bendMaterial == null)
        {
            Debug.LogError("WorldBendManager: No material assigned!");
            return;
        }

        currentBend = bendMaterial.GetFloat("_BendAmount");
        targetBend = currentBend;

        Debug.Log("WorldBendManager started. Initial bend = " + currentBend);
    }

    void Update()
    {
        if (bendMaterial == null) return;

        currentBend = Mathf.Lerp(currentBend, targetBend, Time.deltaTime * lerpSpeed);
        bendMaterial.SetFloat("_BendAmount", currentBend);

        // TEMP debug every second
        if (Time.frameCount % 60 == 0)
            Debug.Log("Current bend: " + currentBend + " | Target: " + targetBend);
    }

    public void SetTargetBend(float newBend)
    {
        Debug.Log("Bend target set to: " + newBend);
        targetBend = newBend;
    }
}
