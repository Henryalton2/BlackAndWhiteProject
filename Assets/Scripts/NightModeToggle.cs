using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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

    private bool nightMode = false;
    private Coroutine transitionCoroutine;
    [SerializeField] private Material materials;
    // Cached renderers
    private readonly List<SpriteRenderer> spriteRenderers = new();
    private readonly List<MeshRenderer> meshRenderers = new();
    private readonly List<Terrain> terrains = new();
    //private readonly List<Material> materials = new();

    private MaterialPropertyBlock mpb;

    void Awake()
    {
        mpb = new MaterialPropertyBlock();
    }

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        CacheInitialObjects();
        ApplyImmediate(false); // start in DAY
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.N))
        {
            nightMode = !nightMode;

            if (transitionCoroutine != null)
                StopCoroutine(transitionCoroutine);

            transitionCoroutine = StartCoroutine(Transition(nightMode));
        }
    }

    // ================= REGISTRATION =================

    void CacheInitialObjects()
    {
        GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(invertTag);

        foreach (var go in taggedObjects)
            RegisterObject(go);
    }

    public void RegisterObject(GameObject obj)
    {
        if (!obj) return;

        var sr = obj.GetComponent<SpriteRenderer>();
        if (sr && !spriteRenderers.Contains(sr))
            spriteRenderers.Add(sr);

        var mr = obj.GetComponent<MeshRenderer>();
        if (mr && !meshRenderers.Contains(mr))
            meshRenderers.Add(mr);

        var t = obj.GetComponent<Terrain>();
        if (t && !terrains.Contains(t))
            terrains.Add(t);
    }

    public void UnregisterObject(GameObject obj)
    {
        if (!obj) return;

        spriteRenderers.Remove(obj.GetComponent<SpriteRenderer>());
        meshRenderers.Remove(obj.GetComponent<MeshRenderer>());
        terrains.Remove(obj.GetComponent<Terrain>());
    }

    // ================= TRANSITION =================

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
            float t = Mathf.Clamp01(elapsed / transitionTime);

            float invertValue = Mathf.Lerp(startInvert, endInvert, t);
            ApplyInvert(invertValue);

            if (mainCamera)
                mainCamera.backgroundColor = Color.Lerp(startSky, endSky, t);

            yield return null;
        }

        ApplyImmediate(enableNight);
    }

    void ApplyInvert(float invertValue)
    {
        // Sprites
        foreach (var sr in spriteRenderers)
        {
            if (!sr) continue;

            sr.GetPropertyBlock(mpb);
            mpb.SetFloat("_Invert", invertValue);
            sr.SetPropertyBlock(mpb);
        }

        // Meshes
        foreach (var mr in meshRenderers)
        {
            if (!mr) continue;

            mr.GetPropertyBlock(mpb);
            mpb.SetFloat("_Invert", invertValue);
            mr.SetPropertyBlock(mpb);
        }

        // Terrains (safe to touch materialTemplate)
        foreach (var t in terrains)
        {
            if (!t || !t.materialTemplate) continue;
            if (t.materialTemplate.HasProperty("_Invert"))
                t.materialTemplate.SetFloat("_Invert", invertValue);
        }

        {
            if (!materials) return;

            if (materials.HasProperty("_Invert"))
                materials.SetFloat("_Invert", invertValue);
        }
    }

    void ApplyImmediate(bool enableNight)
    {
        float invertValue = enableNight ? 1f : 0f;
        ApplyInvert(invertValue);

        if (mainCamera)
            mainCamera.backgroundColor = enableNight ? nightSky : daySky;
    }
}
