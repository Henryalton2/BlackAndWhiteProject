using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class VisualTreeReaction : MonoBehaviour
{
    [Header("Glow Settings")]
    public Color glowColor = new Color(0f, 0.5f, 1f); // Subtle blue
    public float glowIntensity = 1f;                  // Max emission intensity
    public float fadeSpeed = 3f;                      // How fast glow fades

    [Header("Audio Source")]
    public AudioManager audioManager;                 // Reference to AudioManager

    private Renderer rend;
    private Material mat;
    private float currentIntensity = 0f;

    void Start()
    {
        rend = GetComponent<Renderer>();
        if (rend != null)
        {
            // Make a unique material instance for this tree
            mat = rend.material;
            mat.EnableKeyword("_EMISSION");
        }

        // Auto-find AudioManager if not assigned
        if (audioManager == null)
        {
            audioManager = FindObjectOfType<AudioManager>();
            if (audioManager == null)
            {
                Debug.LogWarning($"No AudioManager found for {gameObject.name}. Tree will not react.");
            }
        }
    }

    void Update()
    {
        if (mat == null || audioManager == null) return;

        // Target intensity based on musicLevel
        float targetIntensity = audioManager.musicLevel * glowIntensity;

        // Smoothly interpolate for natural fade in/out
        currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, Time.deltaTime * fadeSpeed);

        // Apply emission color
        mat.SetColor("_EmissionColor", glowColor * currentIntensity);
        Debug.Log($"{gameObject.name} Glow Intensity: {currentIntensity}");
    }

    private void OnDisable()
    {
        if (mat != null)
        {
            mat.SetColor("_EmissionColor", Color.black);
        }
    }
}
