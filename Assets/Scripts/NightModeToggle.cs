using UnityEngine;
using System.Collections;
using System.Linq;

public class NightModeToggle : MonoBehaviour
{
    [Header("Tag of objects to invert")]
    public string invertTag = "Invertible";

    [Header("Transition time in seconds")]
    public float transitionTime = 5f;

    [Header("Star particle systems to invert")]
    public ParticleSystem[] starSystems;

    [Header("Camera settings")]
    public Camera mainCamera;

    private SpriteRenderer[] invertibleSprites;
    private MeshRenderer[] invertibleMeshes;
    private Terrain[] invertibleTerrains;

    private bool nightMode = false; // START IN DAY MODE
    private Coroutine transitionCoroutine;

    // Store original particle colors
    private Color[] startStarColors;

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        // Find all tagged objects
        GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(invertTag);

        invertibleSprites = taggedObjects
            .Select(o => o.GetComponent<SpriteRenderer>())
            .Where(sr => sr != null)
            .ToArray();

        invertibleMeshes = taggedObjects
            .Select(o => o.GetComponent<MeshRenderer>())
            .Where(mr => mr != null)
            .ToArray();

        invertibleTerrains = taggedObjects
            .Select(o => o.GetComponent<Terrain>())
            .Where(t => t != null)
            .ToArray();

        // Store original particle colors
        startStarColors = new Color[starSystems.Length];
        for (int i = 0; i < starSystems.Length; i++)
        {
            if (starSystems[i] != null)
            {
                var main = starSystems[i].main;
                startStarColors[i] = main.startColor.color;
            }
        }

        // ---- Set initial DAY visuals ----
        float initialInvert = 0f; // fully day

        foreach (var sr in invertibleSprites)
            if (sr != null && sr.material != null && sr.material.HasProperty("_Invert"))
                sr.material.SetFloat("_Invert", initialInvert);

        foreach (var mr in invertibleMeshes)
            if (mr != null && mr.material != null && mr.material.HasProperty("_Invert"))
                mr.material.SetFloat("_Invert", initialInvert);

        foreach (var terrain in invertibleTerrains)
            if (terrain != null && terrain.materialTemplate != null && terrain.materialTemplate.HasProperty("_Invert"))
                terrain.materialTemplate.SetFloat("_Invert", initialInvert);

        if (mainCamera != null)
            mainCamera.backgroundColor = Color.white; // day sky

        for (int i = 0; i < starSystems.Length; i++)
        {
            if (starSystems[i] != null)
            {
                var main = starSystems[i].main;
                // Stars contrast the sky: black for day
                main.startColor = new Color(1f - startStarColors[i].r, 1f - startStarColors[i].g, 1f - startStarColors[i].b, startStarColors[i].a);
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.N))
        {
            nightMode = !nightMode;

            if (transitionCoroutine != null)
                StopCoroutine(transitionCoroutine);

            transitionCoroutine = StartCoroutine(TransitionNightMode(nightMode));
        }
    }

    private IEnumerator TransitionNightMode(bool enable)
    {
        float startValue = enable ? 0f : 1f;
        float endValue = enable ? 1f : 0f;
        float elapsed = 0f;

        Color startBg = enable ? Color.white : Color.black;
        Color endBg = enable ? Color.black : Color.white;

        while (elapsed < transitionTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transitionTime);
            float current = Mathf.Lerp(startValue, endValue, t);

            // Update sprites
            foreach (var sr in invertibleSprites)
                if (sr != null && sr.material != null && sr.material.HasProperty("_Invert"))
                    sr.material.SetFloat("_Invert", current);

            // Update meshes
            foreach (var mr in invertibleMeshes)
                if (mr != null && mr.material != null && mr.material.HasProperty("_Invert"))
                    mr.material.SetFloat("_Invert", current);

            // Update terrains
            foreach (var terrain in invertibleTerrains)
                if (terrain != null && terrain.materialTemplate != null && terrain.materialTemplate.HasProperty("_Invert"))
                    terrain.materialTemplate.SetFloat("_Invert", current);

            // Update camera background
            if (mainCamera != null)
                mainCamera.backgroundColor = Color.Lerp(startBg, endBg, t);

            // Gradually invert star colors
            for (int i = 0; i < starSystems.Length; i++)
            {
                if (starSystems[i] != null)
                {
                    var main = starSystems[i].main;
                    Color original = startStarColors[i];

                    main.startColor = Color.Lerp(
                        new Color(1f - original.r, 1f - original.g, 1f - original.b, original.a), // day (inverted)
                        original, // night (original)
                        current
                    );
                }
            }

            yield return null;
        }

        // Final values
        foreach (var sr in invertibleSprites)
            if (sr != null && sr.material != null && sr.material.HasProperty("_Invert"))
                sr.material.SetFloat("_Invert", endValue);

        foreach (var mr in invertibleMeshes)
            if (mr != null && mr.material != null && mr.material.HasProperty("_Invert"))
                mr.material.SetFloat("_Invert", endValue);

        foreach (var terrain in invertibleTerrains)
            if (terrain != null && terrain.materialTemplate != null && terrain.materialTemplate.HasProperty("_Invert"))
                terrain.materialTemplate.SetFloat("_Invert", endValue);

        if (mainCamera != null)
            mainCamera.backgroundColor = endBg;

        for (int i = 0; i < starSystems.Length; i++)
        {
            if (starSystems[i] != null)
            {
                var main = starSystems[i].main;
                main.startColor = startStarColors[i]; // night
            }
        }
    }
}
