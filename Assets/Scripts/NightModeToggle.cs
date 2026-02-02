using UnityEngine;
using System.Collections;
using System.Linq;

public class NightModeToggle : MonoBehaviour
{
    [Header("Tag of objects to invert")]
    public string invertTag = "Invertible";

    [Header("Transition time (seconds)")]
    public float transitionTime = 5f;

    [Header("Camera")]
    public Camera mainCamera;
    public Color daySky = Color.white;
    public Color nightSky = Color.black;

    [Header("Star particle systems")]
    public ParticleSystem[] starSystems;

    private SpriteRenderer[] invertibleSprites;
    private MeshRenderer[] invertibleMeshes;
    private Terrain[] invertibleTerrains;

    private bool nightMode = false;
    private Coroutine transitionCoroutine;

    // Original star colors (WHITE stars)
    private Color[] startStarColors;

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        CacheInvertibles();

        // Cache original (WHITE) star colors
        startStarColors = new Color[starSystems.Length];
        for (int i = 0; i < starSystems.Length; i++)
        {
            if (starSystems[i] != null)
            {
                var main = starSystems[i].main;
                startStarColors[i] = main.startColor.color;
            }
        }

        ApplyImmediate(false); // start in DAY
    }

    void Update()
    {
        // Refresh invertible objects in case new clouds spawned
        CacheInvertibles();

        if (Input.GetKeyDown(KeyCode.N))
        {
            nightMode = !nightMode;

            if (transitionCoroutine != null)
                StopCoroutine(transitionCoroutine);

            transitionCoroutine = StartCoroutine(Transition(nightMode));
        }
    }

    void CacheInvertibles()
    {
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
    }

    IEnumerator Transition(bool enableNight)
    {
        float elapsed = 0f;
        float startInvert = enableNight ? 0f : 1f;
        float endInvert = enableNight ? 1f : 0f;

        Color startSky = enableNight ? daySky : nightSky;
        Color endSky = enableNight ? nightSky : daySky;

        while (elapsed < transitionTime)
        {
            elapsed += Time.deltaTime;
            float tVal = Mathf.Clamp01(elapsed / transitionTime); // renamed from 't'

            float invertValue = Mathf.Lerp(startInvert, endInvert, tVal);

            foreach (var sr in invertibleSprites)
                if (sr != null && sr.material.HasProperty("_Invert"))
                    sr.material.SetFloat("_Invert", invertValue);

            foreach (var mr in invertibleMeshes)
                if (mr != null && mr.material.HasProperty("_Invert"))
                    mr.material.SetFloat("_Invert", invertValue);

            foreach (var terrain in invertibleTerrains)
                if (terrain != null && terrain.materialTemplate != null &&
                    terrain.materialTemplate.HasProperty("_Invert"))
                    terrain.materialTemplate.SetFloat("_Invert", invertValue);

            if (mainCamera != null)
                mainCamera.backgroundColor = Color.Lerp(startSky, endSky, tVal);

            // ⭐ FIXED STAR LOGIC ⭐
            for (int s = 0; s < starSystems.Length; s++)
            {
                if (starSystems[s] == null) continue;

                var main = starSystems[s].main;

                Color white = startStarColors[s];
                Color black = new Color(0f, 0f, 0f, white.a);

                // Day = BLACK stars
                // Night = WHITE stars
                Color from = enableNight ? black : white;
                Color to = enableNight ? white : black;

                main.startColor = Color.Lerp(from, to, tVal);
            }

            yield return null;
        }

        ApplyImmediate(enableNight);
    }

    void ApplyImmediate(bool enableNight)
    {
        float invertValue = enableNight ? 1f : 0f;

        foreach (var sr in invertibleSprites)
            if (sr != null && sr.material.HasProperty("_Invert"))
                sr.material.SetFloat("_Invert", invertValue);

        foreach (var mr in invertibleMeshes)
            if (mr != null && mr.material.HasProperty("_Invert"))
                mr.material.SetFloat("_Invert", invertValue);

        foreach (var terrain in invertibleTerrains)
            if (terrain != null && terrain.materialTemplate != null &&
                terrain.materialTemplate.HasProperty("_Invert"))
                terrain.materialTemplate.SetFloat("_Invert", invertValue);

        if (mainCamera != null)
            mainCamera.backgroundColor = enableNight ? nightSky : daySky;

        // ⭐ LOCK FINAL STAR STATE ⭐
        for (int s = 0; s < starSystems.Length; s++)
        {
            if (starSystems[s] == null) continue;

            var main = starSystems[s].main;
            Color white = startStarColors[s];
            Color black = new Color(0f, 0f, 0f, white.a);

            main.startColor = enableNight ? white : black;
        }
    }
}
