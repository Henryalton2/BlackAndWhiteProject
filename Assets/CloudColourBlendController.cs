using UnityEngine;

public class CloudColourBlendController : MonoBehaviour
{
    public Material targetMaterial; // your SpriteColourBlend material
    public Texture2D variant1;
    public Texture2D variant2;
    public Texture2D variant3;

    [Tooltip("Seconds to animate blend")]
    public float duration = 1f;

    private Texture2D[] variants;
    private bool isBlending = false;

    void Awake()
    {
        // Make material unique per cloud instance
        if (targetMaterial != null)
        {
            targetMaterial = new Material(targetMaterial);
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.material = targetMaterial;
        }

        // Build variant array
        variants = new Texture2D[3];
        variants[0] = variant1;
        variants[1] = variant2;
        variants[2] = variant3;
    }

    public void BlendToRandomVariant()
    {
        if (isBlending || targetMaterial == null) return;

        // Pick a random variant
        Texture2D chosen = variants[Random.Range(0, variants.Length)];
        if (chosen == null) return;

        // Start coroutine for smooth blend
        StartCoroutine(BlendCoroutine(chosen));
    }

    private System.Collections.IEnumerator BlendCoroutine(Texture2D newTex)
    {
        isBlending = true;

        targetMaterial.SetTexture("_ColourVariant1", newTex); // set as _ColourVariant1 (used by shader)
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float blend = Mathf.Clamp01(t / duration);
            targetMaterial.SetFloat("_ColourBlend", blend);
            yield return null;
        }

        // Optional: swap main texture and reset blend
        targetMaterial.SetTexture("_MainTex", newTex);
        targetMaterial.SetFloat("_ColourBlend", 0f);

        isBlending = false;
    }
}
