using UnityEngine;

public class SmokePoof : MonoBehaviour
{
    [Header("Vertical Motion")]
    public float riseSpeed = 0.4f;

    [Header("Wind")]
    public float windMultiplier = 0.1f;   // How much cloud speed affects smoke
    public float turbulenceStrength = 0.15f;

    [Header("Lifetime")]
    public float lifetime = 3f;
    public float scaleGrowSpeed = 0.2f;

    private SpriteRenderer sr;
    private CloudSystem cloudSystem;
    private float timer;
    private float noiseSeed;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        cloudSystem = FindObjectOfType<CloudSystem>();
        noiseSeed = Random.Range(0f, 1000f);
    }

    void Update()
    {
        timer += Time.deltaTime;

        // Base upward motion
        Vector3 movement = Vector3.up * riseSpeed;

        if (cloudSystem)
        {
            Vector3 windDir = cloudSystem.windDirection.normalized;

            // Wind strength tied to cloud speed
            float windStrength = cloudSystem.baseSpeed * windMultiplier;

            // Wind ramps up as smoke rises
            float windRamp = Mathf.Clamp01(timer / lifetime);

            // Turbulence
            float noise = Mathf.PerlinNoise(noiseSeed, Time.time * 0.4f) - 0.5f;
            Vector3 turbulence = windDir * noise * turbulenceStrength;

            movement += windDir * windStrength * windRamp;
            movement += turbulence;
        }

        transform.position += movement * Time.deltaTime;

        // Scale up slightly
        transform.localScale += Vector3.one * scaleGrowSpeed * Time.deltaTime;

        // Fade out
        float alpha = Mathf.Lerp(1f, 0f, timer / lifetime);
        sr.color = new Color(1f, 1f, 1f, alpha);

        if (timer >= lifetime)
            Destroy(gameObject);

        // **DON’T touch rotation** — let your billboarding script handle it
    }
}
